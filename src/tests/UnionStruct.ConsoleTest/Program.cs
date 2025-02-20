// See https://aka.ms/new-console-template for more information

Console.WriteLine("dfawdaw");
// var option1 = Optional<int>.Some(123);
// Console.WriteLine($"{option1.State}, {option1.IsSome(out _)}"); // Some, True
//
// var option2 = Optional<int>.None;
// Console.WriteLine($"{option2.State}, {option2.IsNone()}"); // None, True
//
// OperationResult<int>.Ok(123).WhenOk(Console.WriteLine); // 123
// OperationResult<int>.Fail(new Exception("test exception")).WhenFail(Console.WriteLine); // Prints exception
//
// Either<int, DateTimeOffset>.Left(123).MapLeft(x => x.ToString("00000")).WhenLeft(Console.WriteLine); // 00123
//
// var someDate = new DateTimeOffset(2025, 12, 12, 12, 12, 12, TimeSpan.Zero);
// Either<int, DateTimeOffset>.Right(someDate).MapRight(x => x.ToString("s")).WhenRight(Console.WriteLine); // 2025-12-12T12:12:12