using System;

namespace UnionStruct.Unions;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct)]
public class UnionPartAttribute : Attribute
{
    public string? State { get; set; }
    public bool AddMap { get; set; }
}