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

    /// <summary>
    /// Called once all Gene types are registered in the Factory
    /// </summary>
    /// <param name="entManager"></param>
    /// <param name="system"></param>
    public virtual void OnGeneInitialise(IEntityManager entManager, SharedGeneSystem system)
    {

    }

    /// <summary>
    /// Called when a gene is added to an Entity
    /// </summary>
    /// <param name="entManager"></param>
    /// <param name="host"></param>
    /// <exception cref="Exception"></exception>
    public virtual void OnGeneAdded(IEntityManager entManager, EntityUid host)
    {
        if (entManager == null) throw new Exception("Entity Manager not handed to Gene");
        _host = host;
        _entManager = entManager;
    }

    /// <summary>
    /// Checks if an Entity has a specified Gene
    /// </summary>
    /// <param name="gene"></param>
    /// <param name="genes"></param>
    /// <returns></returns>
    protected bool DoesEntityHaveGene(string gene, SharedGeneticsHostComponent genes)
    {
        return genes.Genes.TryGetValue(gene, out _);
    }

    /// <summary>
    /// Called when a Gene is removed from an entity
    /// </summary>
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
