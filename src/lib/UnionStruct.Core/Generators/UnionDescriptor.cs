using System.Collections.Immutable;

namespace UnionStruct.Generators;

public record struct UnionDescriptor(string Name, ImmutableArray<string> Types);