namespace UnionStruct.CodeGeneration;

public static class PropertyGenerators
{
    public static string CreatePublicReadonlyProperty(string type, string name)
        => $"public {type} {name} {{ get; }}";
}