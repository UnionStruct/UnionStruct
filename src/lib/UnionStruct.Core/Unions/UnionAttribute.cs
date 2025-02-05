using System;

namespace UnionStruct.Unions;

[AttributeUsage(AttributeTargets.Struct)]
public class UnionAttribute : Attribute
{
    private readonly string[] _states;

    public UnionAttribute(params string[] states)
    {
        _states = states;
    }
}