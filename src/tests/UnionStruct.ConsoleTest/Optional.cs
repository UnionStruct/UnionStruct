using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union("None")]
public readonly partial struct Optional<T>
{
    [UnionPart(AddMap = true)] private readonly T? _some;
}