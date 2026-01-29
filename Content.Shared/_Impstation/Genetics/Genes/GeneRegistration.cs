using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

public sealed class GeneRegistration
{
    public string Name { get; }
    public Type Type { get; }

    internal GeneRegistration(string name, Type type)
    {
        Name = name;
        Type = type;
    }
    public override string ToString()
    {
        return $"GeneRegistration({Name}: {Type})";
    }
}
