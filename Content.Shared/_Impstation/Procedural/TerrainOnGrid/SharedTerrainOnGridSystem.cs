using Content.Shared.Sprite;
using Robust.Shared.Debugging;
using Robust.Shared.Physics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Procedural.TerrainOnGrid;

public abstract partial class SharedTerrainOnGridSystem : EntitySystem
{

    private List<LineData> _lines = new List<LineData>();

    public override void Initialize()
    {
        base.Initialize();
    }

    protected void DrawLine(LineData data)
    {

    }

    protected void DrawAngle(LineData data)
    {

    }
    protected struct LineData
    {
        public Vector2i Start;
        public Vector2i End;
        public Vector2i Angle;
    }
}
