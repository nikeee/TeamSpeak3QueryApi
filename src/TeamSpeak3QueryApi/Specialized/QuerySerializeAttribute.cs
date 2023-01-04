using System;

namespace TeamSpeak3QueryApi.Net.Specialized;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
class QuerySerializeAttribute : Attribute
{
    public string Name { get; }
    public QuerySerializeAttribute(string name) => Name = string.IsNullOrWhiteSpace(name) ? null : name;
}
