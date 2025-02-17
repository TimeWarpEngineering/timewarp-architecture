namespace TimeWarp.Architecture.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ApiEndpointAttribute : Attribute
{
    public Type? EndpointType { get; set; }
}
