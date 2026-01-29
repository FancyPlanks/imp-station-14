using Content.Server._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Systems;
using Content.Shared.Magic.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Impstation.Genetics.Systems;

public sealed class GeneSystem : SharedGeneSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public EntityQuery<MetaDataComponent> MetaQuery;

    public Dictionary<BaseGene, EntityUid> _freshGenes = new Dictionary<BaseGene, EntityUid>();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (KeyValuePair<BaseGene, EntityUid> gene in _freshGenes)
        {
            gene.Key.OnGeneAdded(_entityManager, gene.Value);
            _freshGenes.Remove(gene.Key);
        }
    }

    public bool AddGeneToEntity<T>(EntityUid entity, T gene, MetaDataComponent? metadata = null) where T : BaseGene
    {
        //if (!MetaQuery.Resolve(entity, ref metadata, false))
        //    throw new ArgumentException($"Entity {entity} is not valid.", nameof(entity));

        if(gene == null)
            throw new ArgumentNullException(nameof(gene));

        if (!_entityManager.TryGetComponent<GeneticsHostComponent>(entity, out var geneticsComp))
            return false;

        var newGene = _geneFactory.GetRegistration(gene);

        geneticsComp.Genes.Add(newGene.Name, gene);
        _freshGenes.Add(gene, entity);

        return true;
    }
}
