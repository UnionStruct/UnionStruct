using System;

namespace UnionStruct.Unions;

[AttributeUsage(AttributeTargets.Field)]
public sealed class UnionType : Attribute 
{
    private readonly Enum _type;

    public UnionType(Enum type)
    {
        _type = type;
    }
}