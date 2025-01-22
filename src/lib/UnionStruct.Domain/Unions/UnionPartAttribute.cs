using System;

namespace UnionStruct.Unions;

[AttributeUsage(AttributeTargets.Field)]
public class UnionPartAttribute : Attribute
{
    public string? State { get; set; }
}