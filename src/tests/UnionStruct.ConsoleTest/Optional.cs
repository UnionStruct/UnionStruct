using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union("None")]
public readonly partial struct Optional<T>
{
    [UnionPart] private readonly T? _some;
}