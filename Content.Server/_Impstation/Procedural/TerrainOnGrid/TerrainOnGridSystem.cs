using Content.Server.Decals;
using Content.Server.GameTicking.Events;
using Content.Server.Procedural;
using Content.Server.Procedural.DungeonJob;
using Content.Server.Station.Events;
using Content.Shared._Impstation.Procedural.TerrainOnGrid;
using Content.Shared.CCVar;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using NetCord;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server._Impstation.Procedural.TerrainOnGrid;

public sealed partial class TerrainOnGridSystem : SharedTerrainOnGridSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly RayCastSystem _rayCastSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private EntityUid _stationGrid;
    private MapGridComponent _stationMapGrid = default!;
    private Vector2i _location;
    private Entity<StationDataComponent> _mapEntity;
    private Vector2i _stationCenter;
    private bool _gotInitialBounds = false;

    private readonly List<(Vector2i, Tile)> _tiles = new();

    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private TimeSpan _startDelay = TimeSpan.FromSeconds(4);
    private TimeSpan _timeToStart = TimeSpan.Zero;
    private bool _notGenerated = true;

    private const double DungeonJobTime = 0.005;

    public const int CollisionMask = (int)CollisionGroup.Impassable;
    public const int CollisionLayer = (int)CollisionGroup.Impassable;

    private readonly JobQueue _dungeonJobQueue = new(DungeonJobTime);
    private readonly Dictionary<DungeonJob, CancellationTokenSource> _dungeonJobs = new();

    private readonly ProtoId<WeightedRandomPrototype> _asteroidOreWeights = "AsteroidOre";
    private readonly MinMax _asteroidOreCount = new(7, 20);

    private List<string> _templates = new List<string>
    {
        "BlobOmniAsteroid",
        "BlobOmniFloralAsteroid"
    };

    public override void Initialize()
    {
        base.Initialize();

        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(PrototypeReload);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeLocalEvent<StationDataComponent, StationPostInitEvent>(FetchStationData);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if(_time.CurTime > _timeToStart && _notGenerated)
        {
            _notGenerated = false;
            GetGrid();

            //if (!_prototype.TryIndex<DungeonConfigPrototype>("ExteriorMixed", out var dungeon))
            //{
            //    return;
            //}
            //GenerateTerrain(dungeon, 555, GetLocationOnStation(true));
            
            for (int i = 0; i < 10; i++)
            {
                var result = _templates[_random.Next(0, _templates.Count)];
                if (!_prototype.TryIndex<DungeonConfigPrototype>(result, out var dungeon))
                {
                    return;
                }
                GenerateTerrain(dungeon, 555, GetLocationOnStation());
            }
        }
        _dungeonJobQueue.Process();
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        foreach (var token in _dungeonJobs.Values)
        {
            token.Cancel();
        }

        _dungeonJobs.Clear();
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }

        if (!_configManager.GetCVar(CCVars.ProcgenPreload))
            return;

        // Force all templates to be setup.
        foreach (var room in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            GetOrCreateTemplate(room);
        }
    }

    private void PrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.TryGetValue(typeof(DungeonRoomPrototype), out var rooms))
        {
            return;
        }

        foreach (var proto in rooms.Modified.Values)
        {
            var roomProto = (DungeonRoomPrototype)proto;
            var query = AllEntityQuery<DungeonAtlasTemplateComponent>();

            while (query.MoveNext(out var uid, out var comp))
            {
                if (!roomProto.AtlasPath.Equals(comp.Path))
                    continue;

                QueueDel(uid);
                break;
            }
        }

        if (!_configManager.GetCVar(CCVars.ProcgenPreload))
            return;

        foreach (var proto in rooms.Modified.Values)
        {
            var roomProto = (DungeonRoomPrototype)proto;
            var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
            var found = false;

            while (query.MoveNext(out var comp))
            {
                if (!roomProto.AtlasPath.Equals(comp.Path))
                    continue;

                found = true;
                break;
            }

            if (!found)
            {
                GetOrCreateTemplate(roomProto);
            }
        }
    }

    public MapId GetOrCreateTemplate(DungeonRoomPrototype proto)
    {
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
        DungeonAtlasTemplateComponent? comp;

        while (query.MoveNext(out var uid, out comp))
        {
            // Exists
            if (comp.Path.Equals(proto.AtlasPath))
                return Transform(uid).MapID;
        }

        var opts = new MapLoadOptions
        {
            DeserializationOptions = DeserializationOptions.Default with { PauseMaps = true },
            ExpectedCategory = FileCategory.Map
        };

        if (!_loader.TryLoadGeneric(proto.AtlasPath, out var res, opts) || !res.Maps.TryFirstOrNull(out var map))
            throw new Exception($"Failed to load dungeon template.");

        comp = AddComp<DungeonAtlasTemplateComponent>(map.Value.Owner);
        comp.Path = proto.AtlasPath;
        return map.Value.Comp.MapId;
    }

    public void FetchStationData(Entity<StationDataComponent> entity, ref StationPostInitEvent args)
    {
        _mapEntity = entity;
        _notGenerated = true;
        _timeToStart = _time.CurTime + _startDelay;
        _location = (Vector2i)_transform.GetMapCoordinates(entity).Position;
        _gotInitialBounds = false;
    }

    public void GetGrid()
    {
        var grid = _station.GetLargestGrid((_mapEntity.Owner, (StationDataComponent?)_mapEntity.Comp));
        if (grid == null)
            return;

        _stationGrid = (EntityUid)grid;
        if (!_entity.TryGetComponent<MapGridComponent>(_stationGrid, out var map))
            return;

        _stationMapGrid = map;

        if (!_prototype.TryIndex<DungeonConfigPrototype>("BlobOmniAsteroid", out var dungeon))
        {
            return;
        }
        //GenerateTerrain(dungeon, 555, GetLocationOnStation());
    }

    /// <summary>
    /// Will pick a random point from around the station, aim towards the center and then fire a ray until it
    /// hits a flooring surface to place the asteroid
    /// </summary>
    /// <returns></returns>
    public Vector2i GetLocationOnStation(bool skipWallFind = false)
    {
        var location = new Vector2i(0,0);
        var stationPos = (Vector2i)_transform.GetMapCoordinates(_stationGrid).Position;
        var stationBounds = _stationMapGrid.LocalAABB;

        // Calculate the center of the station grid
        // We store this so that it's not ever shifting as more asteroids spawn
        if (!_gotInitialBounds)
        {
            var rot = _transform.GetWorldRotation(_stationGrid);
            _gotInitialBounds = true;

            _stationCenter = new Vector2i((int)(stationBounds.Width + stationBounds.Left) / 2, (int)(stationBounds.Height + stationBounds.Bottom) / 2);
            _stationCenter = stationPos - _stationCenter;
            _stationCenter = stationPos - (Vector2i)Vector2.Transform(_stationCenter - stationPos, Matrix3x2.CreateRotation((float)rot));
        }

        var randomPoint = new Vector2i();
        var wall = _random.Next(0, 4);

        // Pick a random point along the bounds of the station grid to start the spawn
        switch(wall)
        {
            case 0: // Left Wall
                randomPoint = new Vector2i((int)stationBounds.Left, _random.Next((int)stationBounds.Bottom, (int)stationBounds.Height));
                Log.Info($"Picked Wall Left: {randomPoint}");
                break;
            case 1: // Right Wall
                randomPoint = new Vector2i((int)stationBounds.Width, _random.Next((int)stationBounds.Bottom, (int)stationBounds.Height));
                Log.Info($"Picked Wall Right: {randomPoint}");
                break;
            case 2: // Top Wall
                randomPoint = new Vector2i(_random.Next((int)stationBounds.Left, (int)stationBounds.Width), (int)stationBounds.Height);
                Log.Info($"Picked Wall Top: {randomPoint}");
                break;
            case 3: // Bottom Wall
                randomPoint = new Vector2i(_random.Next((int)stationBounds.Left, (int)stationBounds.Width), (int)stationBounds.Bottom);
                Log.Info($"Picked Wall Bottom: {randomPoint}");
                break;
        }

        if(skipWallFind)
        {
            Log.Info($"Generating Terrain from point {randomPoint} on boundary wall {wall}");
            return randomPoint;
        }

        // Direction from the initial asteroid spawn to the station center
        var entCoords = new EntityCoordinates(_stationGrid, randomPoint);
        var mapCoords = new EntityCoordinates(_stationGrid, _stationCenter);
        var ent = _transform.ToMapCoordinates(entCoords);
        var map = _transform.ToMapCoordinates(mapCoords);

        Log.Info($"Working with coords: Center: {_stationCenter} - Entity: {ent.Position} - Randompoint: {randomPoint}");

        var dir = new Vector2(
            _stationCenter.X - ent.Position.X,
            _stationCenter.Y - ent.Position.Y).Normalized();

        var maxLoops = 10;
        while (maxLoops >= 0)
        {
            --maxLoops;

            // Fire a ray from the initial asteroid spawn towards the station center
            var ray = new CollisionRay(ent.Position, dir, (int)CollisionGroup.AllMask);
            var rayCastResults = _physics.IntersectRay(map.MapId, ray, 10);
            var result = rayCastResults.FirstOrNull();

            if (result?.HitEntity != null)
            {
                //var hitCoord = new MapCoordinates(result.Value.HitEntity.ToCoordinates(), map.MapId);
                var returner = (Vector2i)_entity.GetComponent<TransformComponent>(result.Value.HitEntity).LocalPosition;//(Vector2i)result.Value.HitEntity.ToCoordinates().Position;//(Vector2i)_transform.ToCoordinates(hitCoord).Position;
                var forme = _transform.ToMapCoordinates(new EntityCoordinates(_stationGrid, returner));
                Log.Info($"Found tile {_entity.ToPrettyString(result.Value.HitEntity)} at pos: Converted {forme} / Local {returner} / Hitpos {result.Value.HitPos}");
                return returner;
            }

            ent = new MapCoordinates(ent.Position + (dir * 10), map.MapId);
            Log.Info($"No suitable spot found. Shifting {ent.Position} by {dir * 10} units");
        }
        Log.Error($"No suitable location for asteroid found on station. Resorting to bounds: {randomPoint}");
        return randomPoint;
    }

    public void GenerateTerrain(
        DungeonConfig gen,
        int seed,
        Vector2i pos,
        EntityCoordinates? coordinates = null)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new DungeonJob(
            Log,
            DungeonJobTime,
            EntityManager,
            _prototype,
            _tileDefManager,
            _anchorable,
            _decals,
            _dungeon,
            _lookup,
            _tile,
            _turf,
            _transform,
            gen,
            _stationMapGrid,
            _stationGrid,
            seed,
            pos,
            coordinates,
            cancelToken.Token,
            _random);

        var oreCount = _random.Next(_asteroidOreCount.Min, _asteroidOreCount.Max);
        var layers = new Dictionary<string, int>();
        var weightedProto = _prototype.Index(_asteroidOreWeights);
        var rand = new System.Random(seed);
        for (var i = 0; i < oreCount; i++)
        {
            var ore = weightedProto.Pick(rand);
            gen.Layers.Add(_prototype.Index<OreDunGenPrototype>(ore));

            var layerCount = layers.GetOrNew(ore);
            layerCount++;
            layers[ore] = layerCount;
        }

        _dungeonJobs.Add(job, cancelToken);
        _dungeonJobQueue.EnqueueJob(job);
    }

    public async Task<List<Dungeon>> GenerateTerrainAsync(
    DungeonConfig gen,
    int seed,
    Vector2i pos,
    EntityCoordinates? coordinates = null)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new DungeonJob(
            Log,
            DungeonJobTime,
            EntityManager,
            _prototype,
            _tileDefManager,
            _anchorable,
            _decals,
            _dungeon,
            _lookup,
            _tile,
            _turf,
            _transform,
            gen,
            _stationMapGrid,
            _stationGrid,
            seed,
            pos,
            coordinates,
            cancelToken.Token,
            _random);

        _dungeonJobs.Add(job, cancelToken);
        _dungeonJobQueue.EnqueueJob(job);
        await job.AsTask;

        if(job.Exception != null)
            throw job.Exception;

        return job.Result!;
    }
}
