using Content.Server._Impstation.Genetics.Components;
using Content.Server._Impstation.Genetics.Systems;
using Content.Server.Administration;
using Content.Shared._Impstation.Genetics;
using Content.Shared.Administration;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server._Impstation.Administration.Commands;

[AdminCommand(AdminFlags.Admin), UsedImplicitly]
public sealed class AddGeneCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GeneSystem _geneSystem = default!; 
    private GeneFactory _geneFactory = default!; 

    public override string Command => "addgene";

    public override string Description => "cmd-addgene-desc" + ("requiredComponent", nameof(GeneticsHostComponent));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _geneFactory ??= _geneSystem._geneFactory;

        if(args.Length < 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var entInt))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var nEnt))
        {
            shell.WriteLine($"{args[0]} is not a valid entity.");
            return;
        }

        if (!EntityManager.TryGetEntity(nEnt, out var target))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!EntityManager.HasComponent<GeneticsHostComponent>(target))
        {
            shell.WriteLine(Loc.GetString("shell-target-entity-does-not-have-message", ("missing", "Genetics Host Component")));
            return;
        }

        if (!_geneFactory.TryGetRegistration(args[1], out var registration))
        {
            shell.WriteLine($"No component found with name {args[1]}.");
            return;
        }

        var gene = _geneFactory.GetGene(registration.Type);

        _geneSystem.AddGeneToEntity((EntityUid)target, gene);
    }
}
