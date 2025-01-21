// See https://aka.ms/new-console-template for more information

using UnionStruct.Unions;

Console.WriteLine("Hello, World!");

[Union<UnionTypes>]
public readonly partial struct TestUnion
{
    [UnionType(UnionTypes.Ok)] private readonly int _value;
    private readonly Exception _exception;
}

enum UnionTypes
{
    Ok,
    Fail
}