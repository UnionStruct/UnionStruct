using System;

namespace UnionStruct.Unions;

[AttributeUsage(AttributeTargets.Struct)]
public class UnionAttribute<T> : Attribute where T : Enum
{
}