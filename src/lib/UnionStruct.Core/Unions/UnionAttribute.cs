using System;

namespace UnionStruct.Unions;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class UnionAttribute<T> : Attribute
    where T : Enum
{
    
}