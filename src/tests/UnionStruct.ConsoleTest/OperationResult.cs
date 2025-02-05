using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union]
public readonly partial struct OperationResult<T>
{
    [UnionPart(AddMap = true)] private readonly T? _ok;
    [UnionPart(State = "Fail")] private readonly Exception? _exception;
}