using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union]
public readonly partial struct TestUnion<T>
{
    [UnionPart] private readonly T? _value;
    [UnionPart(State = "Fail")] private readonly Exception? _exception;
}