using Content.Shared._Impstation.Genetics.Attributes;
using Content.Shared._Impstation.Genetics.Genes;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Content.Shared._Impstation.Genetics;

[Virtual]
public class GeneFactory(
    IDynamicTypeFactory _typeFactory,
    IReflectionManager _reflectionManager,
    ILogManager _logManager)
{
    private readonly ISawmill _sawmill = _logManager.GetSawmill("ent.geneFactory");

    private FrozenDictionary<string, GeneRegistration> _names = FrozenDictionary<string, GeneRegistration>.Empty;
    private FrozenDictionary<string, string> _lowerCaseNames = FrozenDictionary<string, string>.Empty;

    private FrozenDictionary<Type, GeneRegistration> _types = FrozenDictionary<Type, GeneRegistration>.Empty;

    public event Action<GeneRegistration[]>? _genesAdded;

    private static string CalculateGeneName(Type type)
    {
        const string gene = "Gene";
        var typeName = type.Name;
        if(!typeName.EndsWith(gene))
        {
            throw new InvalidGeneNameException($"Gene {type} must end with the word Gene");
        }

        string name = typeName[..^gene.Length];
        const string client = "Client";
        const string server = "Server";
        const string shared = "Shared";
        if (typeName.StartsWith(client, StringComparison.Ordinal))
        {
            name = typeName[client.Length..^gene.Length];
        }
        else if (typeName.StartsWith(server, StringComparison.Ordinal))
        {
            name = typeName[server.Length..^gene.Length];
        }
        else if (typeName.StartsWith(shared, StringComparison.Ordinal))
        {
            name = typeName[shared.Length..^gene.Length];
        }
        DebugTools.Assert(name != String.Empty, $"Gene {type} has invalid name {type.Name}");
        return name;
    }

    public void DoAutoRegistration()
    {
        var types = _reflectionManager.FindTypesWithAttribute<RegisterGeneAttribute>().ToArray();
        RegisterTypesInternal(types, false);
    }

    private void RegisterTypesInternal(Type[] types, bool overwrite)
    {
        var added = new GeneRegistration[types.Length];

        var names = _names.ToDictionary();
        var lowerCaseNames = _lowerCaseNames.ToDictionary();
        var typesDict = _types.ToDictionary();

        var st = RStopwatch.StartNew();
        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            added[i] = Register(type, names, lowerCaseNames, typesDict);
        }
        _sawmill.Verbose($"Registering genes took {st.Elapsed.TotalMilliseconds:f2}ms");

        var st2 = RStopwatch.StartNew();
        _names = names.ToFrozenDictionary();
        _types = typesDict.ToFrozenDictionary();
        _sawmill.Verbose($"Freezing gene dictionaries took {st2.Elapsed.TotalMilliseconds:f2}ms");
        _genesAdded?.Invoke(added);
    }

    private GeneRegistration Register(
        Type type,
        Dictionary<string, GeneRegistration> names,
        Dictionary<string, string> lowerCaseNames,
        Dictionary<Type, GeneRegistration> types
        )
    {
        var name = CalculateGeneName(type);

        if (!type.IsSubclassOf(typeof(BaseGene)))
            throw new InvalidOperationException($"Type is not derived from gene: {type}");

        if (_names.TryGetValue(name, out var registered))
            throw new InvalidOperationException($"Attempted to register duplicate gene name: {name}");

        var registration = new GeneRegistration(name, type);

        names[name] = registration;
        types[type] = registration;

        return registration;
    }

    #region GETGENE

    public List<BaseGene> GetRegisteredGenes()
    {
        var genes = new List<BaseGene>();

        foreach (KeyValuePair<Type, GeneRegistration> gene in _types)
        {
            genes.Add(_typeFactory.CreateInstance<BaseGene>(gene.Value.Type));
        }

        return genes;
    }

    public BaseGene GetGene(Type geneType)
    {
        if (!_types.TryGetValue(geneType, out var value))
            throw new InvalidOperationException($"{geneType} is not a registered Gene");

        return _typeFactory.CreateInstance<BaseGene>(value.Type);
    }

    public BaseGene GetGene(string geneName, bool ignoreCase = false)
    {
        return _typeFactory.CreateInstance<BaseGene>(GetRegistration(geneName, ignoreCase).Type);
    }
    #endregion

    public GeneRegistration GetRegistration(BaseGene gene)
    {
        return GetRegistration(gene.GetType());
    }

    public GeneRegistration GetRegistration(Type gene)
    {
        try
        {
            return _types[gene];
        }
        catch(KeyNotFoundException)
        {
            throw new UknownGeneException($"Unkown type: {gene}");
        }
    }

    public GeneRegistration GetRegistration(string geneName, bool ignoreCase = false)
    {
        if (ignoreCase && _lowerCaseNames.TryGetValue(geneName, out var lcName))
            geneName = lcName;

        try
        {
            return _names[geneName];
        }
        catch(KeyNotFoundException)
        {
            throw new UknownGeneException($"Uknown name: {geneName}");
        }
    }

    public bool TryGetRegistration(string geneName, [NotNullWhen(true)] out GeneRegistration? registration, bool ignoreCase = false)
    {
        if (ignoreCase && _lowerCaseNames.TryGetValue(geneName, out var lcName))
            geneName = lcName;

        if(_names.TryGetValue(geneName, out var tempReg))
        {
            registration = tempReg;
            return true;
        }

        registration = null;
        return false;
    }
}

public sealed class InvalidGeneNameException : Exception
{
    public InvalidGeneNameException(string message) : base(message) { }
}

public sealed class UknownGeneException : Exception
{
    public UknownGeneException(string message) : base(message) { }
}
