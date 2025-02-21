// See https://aka.ms/new-console-template for more information

using UnionStruct.ConsoleTest;

Console.WriteLine("dfawdaw");

var option1 = Optional<int>.Some(123);
Console.WriteLine($"{option1.State}, {option1.IsSome(out _)}"); // Some, True

var option2 = Optional<int>.None;
Console.WriteLine($"{option2.State}, {option2.IsNone()}"); // None, True

OperationResult<int>.Ok(123).WhenOk(Console.WriteLine); // 123
OperationResult<int>.Fail(new Exception("test exception")).WhenFail(Console.WriteLine); // Prints exception

Either<int, DateTimeOffset>.Left(123).MapLeft(x => x.ToString("00000")).WhenLeft(Console.WriteLine); // 00123

var someDate = new DateTimeOffset(2025, 12, 12, 12, 12, 12, TimeSpan.Zero);
Either<int, DateTimeOffset>.Right(someDate).MapRight(x => x.ToString("s")).WhenRight(Console.WriteLine); // 2025-12-12T12:12:12

var optionTask = await Create()
    .MapSome(x => x * 2)
    .MapSomeAsync(async x =>
    {
        await Task.Delay(100);
        return x * 3;
    });

_ = NumericOption<int>.Ok(123)
    .MapOk(x => x + 123)
    .MapOk(_ => NumericOption<int>.CalcFail);

_ = DisposableOption<HttpClient>
    .Some(new HttpClient())
    .WhenSome(x => x.Dispose());

Task<Optional<int>> Create() => Task.FromResult(Optional<int>.Some(123));