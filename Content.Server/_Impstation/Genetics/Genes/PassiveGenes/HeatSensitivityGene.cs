using Content.Server._Impstation.Genetics.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Damage.Components;
using Content.Shared._Impstation.Genetics.Attributes;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Radiation.Events;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Impstation.Genetics.Genes.PassiveGenes;

[RegisterGene]
public sealed partial class HeatSensitivityGene : BaseGene
{
    private static FlammableSystem _flammableSystem = default!;

    public override void OnGeneInitialise(IEntityManager entManager, SharedGeneSystem system)
    {
        base.OnGeneInitialise(entManager, system);
        system.SubscribeMeToEvent<GeneticsHostComponent, DamageChangedEvent>(LightEmUp);
        _flammableSystem = entManager.System<FlammableSystem>();
    }

    public override void OnGeneAdded(IEntityManager entManager, EntityUid host)
    {
        base.OnGeneAdded(entManager, host);
    }

    /// <summary>
    /// Lights someone on fire if they take heat damage
    /// I still have yet to find a way to check if ONLY heat damage has increased
    /// this means if you have any heat damage and get hurt by something else, you still catch fire
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    public void LightEmUp(Entity<GeneticsHostComponent> entity, ref DamageChangedEvent args)
    {
        if (!DoesEntityHaveGene("HeatSensitivity", entity.Comp))
            return;

        if (args.DamageDelta == null)
            return;

        if (!args.DamageIncreased)
            return;

        if (args.Damageable.Damage.DamageDict["Heat"].Value > 0)
        {
            _flammableSystem.AdjustFireStacks(entity, 100, null, true);
        }
    }
}
