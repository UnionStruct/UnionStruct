using System.Collections.Generic;
using System.Collections.Immutable;

namespace UnionStruct.Unions;

public readonly struct UnionDescriptor
{
    public UnionDescriptor(
        string? ns,
        string structName,
        ImmutableArray<string> genericParameters,
        ImmutableDictionary<string, string> unvaluedStates,
        ImmutableArray<UnionTypeDescriptor> fields
    )
    {
        Namespace = ns;
        StructName = structName;
        GenericParameters = genericParameters;
        UnvaluedStates = unvaluedStates;
        Fields = fields;
    }

    public string? Namespace { get; }
    public string StructName { get; }
    public ImmutableArray<string> GenericParameters { get; }
    public ImmutableDictionary<string, string> UnvaluedStates { get; }
    public ImmutableArray<UnionTypeDescriptor> Fields { get; }
}

public readonly struct UnionTypeDescriptor
{
    public UnionTypeDescriptor(string name, string type, ImmutableDictionary<string, string> unionArguments)
    {
        Name = name;
        Type = type;
        UnionArguments = unionArguments;
    }

    public string Name { get; }
    public string Type { get; }
    public ImmutableDictionary<string, string> UnionArguments { get; }
}