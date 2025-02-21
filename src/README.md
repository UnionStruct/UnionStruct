# Util lib to generate discriminated unions
### Supports:
* Mapping functionality on demand
* Async function and extensions to work with `Task<Union>` instances to avoid redundant awaits
* You can add additional logic to your unions

### Limitation
* You have to define a namespace for unions. e.g. You cannot create unions in top-level Program.cs

### Samples
```C#
    using UnionStruct.Unions;

    namespace YourNamespace;

    // Will generate a union of two possible values: Optional.None and Optional.Some(T value)
    [Union("None")]
    public readonly partial struct Optional<T>
    {
        // Will add mapping functionality: option.MapSome(lambda), option.MapSomeAsync(lambda)
        [UnionPart(AddMap = true)] private readonly T? _some;
    }
```
```C#
    using UnionStruct.Unions;

    namespace YourNamespace;

    [Union]
    public readonly partial struct Result<T>
    {
        [UnionPart(AddMap = true)] private readonly T? _ok;
        [UnionPart(State = "Fail")] private readonly Exception? _exception
    }
```
