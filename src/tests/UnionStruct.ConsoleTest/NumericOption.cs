using System.Numerics;
using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union("CalcFail")]
public partial struct NumericOption<T> where T : struct, INumber<T>
{
    [UnionPart(AddMap = true, State = "Ok")]
    private readonly T? _number;
}