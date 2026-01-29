using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Systems;
using Robust.Shared.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

[Reflect(false)]
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseGene
{
    public static IEntityManager _entManager = default!;

    public EntityUid _host;

    public bool _geneActive = false;

    public int _geneStabilityValue = 0;

    public virtual void OnGeneInitialise(IEntityManager entManager, SharedGeneSystem system)
    {

    }

    public virtual void OnGeneAdded(IEntityManager entManager, EntityUid host)
    {
        if (entManager == null) throw new Exception("Entity Manager not handed to Gene");
        _host = host;
        _entManager = entManager;
    }

    protected bool DoesEntityHaveGene(string gene, SharedGeneticsHostComponent genes)
    {
        return genes.Genes.TryGetValue(gene, out _);
    }

    public virtual void OnGeneRemoved() { }

    public virtual void ApplyPositiveEffect() { }
    public virtual void ApplyNegativeEffect() { }
    public virtual void ActivateEffect() { }
}

public enum GeneData
{
    AB,
    BA
}
