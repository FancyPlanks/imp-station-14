using Robust.Shared.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Systems;

public abstract class SharedGeneSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entitySystem = default!;
    public GeneFactory _geneFactory = default!;

    /// <summary>
    /// Kick the Gene factory into gear so it can go through and auto-register all of the Genes we need
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _geneFactory = new GeneFactory
            (
            IoCManager.Resolve<IDynamicTypeFactory>(),
            IoCManager.Resolve<IReflectionManager>(),
            IoCManager.Resolve<ILogManager>()
            );

        _geneFactory.DoAutoRegistration();

        foreach(var gene in _geneFactory.GetRegisteredGenes())
        {
            gene.OnGeneInitialise(_entitySystem, this);
        }
    }

    /// <summary>
    /// The Genes use this to subscribe their necessary functions
    /// Only EntitySystems can do this hence why this exists. I get the sixth sense that this is a
    /// god awful idea that would have me sent to the gallows but, it works!
    /// </summary>
    /// <typeparam name="TComp"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="method"></param>
    public void SubscribeMeToEvent<TComp, TEvent>(EntityEventRefHandler<TComp, TEvent> method)
        where TComp : IComponent
        where TEvent : notnull
    {
        SubscribeLocalEvent(method);
    }
}
