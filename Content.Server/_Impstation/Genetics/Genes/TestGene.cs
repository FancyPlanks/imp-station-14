using Content.Server._Impstation.Genetics.Components;
using Content.Server._Impstation.Genetics.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Impstation.Genetics.Attributes;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Movement.Events;
using Content.Shared.Radiation.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Impstation.Genetics.Genes;

[RegisterGene]
public sealed partial class TestGene : SharedTestGene
{
    public override void OnGeneInitialise(IEntityManager entManager, SharedGeneSystem system)
    {
        base.OnGeneInitialise(entManager, system);
        //var geneSystem = _entManager.System<GeneSystem>();
        system.SubscribeMeToEvent<GeneticsHostComponent, OnIrradiatedEvent>(TriggerExplosion);
    }

    public override void OnGeneAdded(IEntityManager entManager, EntityUid host)
    {
        base.OnGeneAdded(entManager, host);
    }

    public void TriggerExplosion(Entity<GeneticsHostComponent> entity, ref OnIrradiatedEvent args)
    {
        if (!DoesEntityHaveGene("Test", entity.Comp))
            return;

        var explosionSystem = _entManager.System<SharedExplosionSystem>();

        //if (entity.Owner != _host)
        //    return;

        explosionSystem.QueueExplosion(
            entity.Comp.Genes["Test"]._host,
            "Default",
            20f,
            10f,
            10f
        );
    }
}
