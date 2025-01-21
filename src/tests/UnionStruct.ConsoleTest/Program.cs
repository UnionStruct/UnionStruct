// See https://aka.ms/new-console-template for more information

using UnionStruct.Unions;

Console.WriteLine("Hello, World!");

[Union<UnionType>]
public readonly partial struct TestUnion;

enum UnionType
{
    Ok,
    Fail
}