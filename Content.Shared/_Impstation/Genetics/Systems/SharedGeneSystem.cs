using Robust.Shared.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Systems;

public abstract class SharedGeneSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entitySystem = default!;
    public GeneFactory _geneFactory = default!;

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

    public void SubscribeMeToEvent<TComp, TEvent>(EntityEventRefHandler<TComp, TEvent> method)
        where TComp : IComponent
        where TEvent : notnull
    {
        SubscribeLocalEvent(method);
    }
}
