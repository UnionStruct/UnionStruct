using System;
using System.Collections.Immutable;
using System.Linq;
using UnionStruct.Unions;

namespace UnionStruct.CodeGeneration;

public static class FieldNameToStateMap
{
    public static ImmutableArray<string> CreateUnvaluedEnums(UnionDescriptor descriptor)
    {
        return descriptor.UnvaluedStates.Select(SanitizeState).ToImmutableArray();
    }
    
    public static ImmutableDictionary<string, string> Create(UnionDescriptor descriptor)
    {
        const string stateConfig = nameof(UnionPartAttribute.State);

        return descriptor.Fields
            .ToImmutableDictionary(
                x => x.Name,
                x => x.UnionArguments.TryGetValue(stateConfig, out var stateName)
                    ? SanitizeState(stateName)
                    : SanitizeField(x.Name)
            );
    }
    
    private static string SanitizeState(string state)
    {
        var span = state.AsSpan();
        var hasQuotes = span.StartsWith("\"") && span.EndsWith("\"");

        if (hasQuotes)
        {
            span = span[1..^1];
        }

        return span.ToString();
    }

    private static string SanitizeField(string field)
    {
        var span = field.AsSpan();
        var hasDash = span.StartsWith("_");

        if (hasDash)
        {
            span = span[1..];
        }

        var isLowerStart = char.IsLower(span[0]);

        if (isLowerStart)
        {
            Span<char> copySpan = stackalloc char[span.Length];
            span.CopyTo(copySpan);
            copySpan[0] = char.ToUpper(copySpan[0]);

            span = new ReadOnlySpan<char>(copySpan.ToArray());
        }

        return span.ToString();
    }
}