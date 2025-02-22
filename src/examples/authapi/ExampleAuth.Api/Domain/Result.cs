using System.Collections.Immutable;
using UnionStruct.Unions;

namespace ExampleAuth.Api.Domain;

[Union]
public readonly partial struct Result<T>
{
    [UnionPart(AddMap = true)] private readonly T? _ok;
    [UnionPart] private readonly UseCaseError? _error;
    [UnionPart] private readonly ImmutableDictionary<string, ImmutableArray<string>> _validationErrors;
}

public readonly record struct UseCaseError(string Code, string Message);