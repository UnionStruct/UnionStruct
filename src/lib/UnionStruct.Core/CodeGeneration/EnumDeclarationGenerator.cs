using System.Linq;

namespace UnionStruct.CodeGeneration;

public static class EnumDeclarationGenerator
{
    public static GeneratedEnum Generate(UnionContext context)
    {
        var enumValues = string.Join(',', context.FieldNameToEnumMap.Values.Concat(context.UnvaluedEnums));
        var enumName = $"{context.Descriptor.StructName}State";
        return new GeneratedEnum(
            enumName,
            $"public enum {enumName} {{ {enumValues} }}",
            PropertyGenerators.CreatePublicReadonlyProperty(enumName, "State")
        );
    }
}

public readonly struct GeneratedEnum
{
    public GeneratedEnum(string name, string body, string enumPropertyDeclaration)
    {
        Name = name;
        Body = body;
        EnumPropertyDeclaration = enumPropertyDeclaration;
    }

    public string Name { get; }

    public string Body { get; }
    public string EnumPropertyDeclaration { get; }
}