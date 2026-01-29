using Content.Shared._Impstation.Genetics.Genes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Components;

/// <summary>
/// The main component for keeping track of a Mobs active Genes and their Gene Value
/// </summary>
public abstract partial class SharedGeneticsHostComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, BaseGene> Genes = new Dictionary<string, BaseGene>();

    [ViewVariables(VVAccess.ReadWrite)]
    public int GeneScaleValue = 0;
}
