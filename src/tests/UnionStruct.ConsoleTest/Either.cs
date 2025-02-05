using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union]
public readonly partial struct Either<T1, T2>
{
    [UnionPart(AddMap = true)] private readonly T1? _left;
    [UnionPart(AddMap = true)] private readonly T2? _right;
}