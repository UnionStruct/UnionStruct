using UnionStruct.Unions;

namespace UnionStruct.ConsoleTest;

[Union]
public readonly partial struct Result<T>
{
    [UnionPart(State = "Success", AddMap = true)]
    private readonly T? _value;

    [UnionPart(State = "Fail")] private readonly Exception? _exception;
}

[Union, UnionPart(State = "None")]
public readonly partial struct Option<T>
{
    [UnionPart(State = "Some", AddMap = true)]
    private readonly T? _value;
}

[Union]
public readonly partial struct Either<TLeft, TRight>
{
    [UnionPart(State = "Left", AddMap = true)]
    private readonly TLeft? _left;

    [UnionPart(State = "Right", AddMap = true)]
    private readonly TRight? _right;
}