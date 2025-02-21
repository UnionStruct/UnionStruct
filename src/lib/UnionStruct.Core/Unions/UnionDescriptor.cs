using System.Collections.Immutable;

namespace UnionStruct.Unions;

public readonly struct UnionDescriptor
{
    public UnionDescriptor(
        string? ns,
        string structName,
        ImmutableArray<string> genericParameters,
        ImmutableArray<string> unvaluedStates,
        ImmutableArray<string> usings,
        ImmutableArray<UnionTypeDescriptor> fields,
        ImmutableDictionary<string, string> genericConstraints
    )
    {
        Namespace = ns;
        StructName = structName;
        GenericParameters = genericParameters;
        UnvaluedStates = unvaluedStates;
        Usings = usings;
        Fields = fields;
        GenericConstraints = genericConstraints;
    }

    public string? Namespace { get; }
    public string StructName { get; }
    public ImmutableArray<string> GenericParameters { get; }
    public ImmutableArray<string> UnvaluedStates { get; }
    public ImmutableArray<string> Usings { get; }
    public ImmutableArray<UnionTypeDescriptor> Fields { get; }
    public ImmutableDictionary<string, string> GenericConstraints { get; }
}

public readonly struct UnionTypeDescriptor
{
    public UnionTypeDescriptor(string name, string type, ImmutableDictionary<string, string> unionArguments, bool isNullable)
    {
        Name = name;
        Type = type;
        UnionArguments = unionArguments;
        IsNullable = isNullable;
    }

    public string Name { get; }
    public string Type { get; }
    public ImmutableDictionary<string, string> UnionArguments { get; }
    public bool IsNullable { get; }
}