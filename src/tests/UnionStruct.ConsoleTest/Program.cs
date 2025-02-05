// See https://aka.ms/new-console-template for more information

using UnionStruct.ConsoleTest;

var option = await Option<DateTimeOffset>.Some(DateTimeOffset.UtcNow)
    .MapSome(async x =>
    {
        await Task.Delay(x.Year);
        return x.Year;
    });

_ = option.Fold(
    x =>
    {
        Console.WriteLine(x);
        return 1;
    },
    () =>
    {
        Console.WriteLine("Sosat");
        return 2;
    }
);