using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union("None")]
public partial struct DisposableOption<T> where T : IDisposable
{
    [UnionPart(AddMap = true)] private readonly T? _some;
}