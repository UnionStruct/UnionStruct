using System.Collections.Immutable;
using UnionStruct.Unions;

namespace UnionStruct.CodeGeneration;

public readonly struct UnionContext
{
    public UnionContext(
        UnionDescriptor descriptor,
        ImmutableDictionary<string, string> fieldNameToEnumMap,
        ImmutableArray<string> unvaluedEnums,
        string fullUnionDeclaration, string genericDeclaration)
    {
        Descriptor = descriptor;
        FieldNameToEnumMap = fieldNameToEnumMap;
        UnvaluedEnums = unvaluedEnums;
        FullUnionDeclaration = fullUnionDeclaration;
        GenericDeclaration = genericDeclaration;
    }

    public UnionDescriptor Descriptor { get; }
    public ImmutableDictionary<string, string> FieldNameToEnumMap { get; }
    public ImmutableArray<string> UnvaluedEnums { get; }
    public string FullUnionDeclaration { get; }
    public string GenericDeclaration { get; }

    public static UnionContext Create(UnionDescriptor descriptor)
    {
        var fieldNameToEnumMap = FieldNameToStateMap.Create(descriptor);
        var unvaluedEnums = FieldNameToStateMap.CreateUnvaluedEnums(descriptor);

        var genericParameters = string.Join(',', descriptor.GenericParameters);
        var genericDeclaration = genericParameters == string.Empty ? string.Empty : $"<{genericParameters}>";

        return new UnionContext(
            descriptor,
            fieldNameToEnumMap,
            unvaluedEnums,
            $"{descriptor.StructName}{genericDeclaration}",
            genericDeclaration
        );
    }
}