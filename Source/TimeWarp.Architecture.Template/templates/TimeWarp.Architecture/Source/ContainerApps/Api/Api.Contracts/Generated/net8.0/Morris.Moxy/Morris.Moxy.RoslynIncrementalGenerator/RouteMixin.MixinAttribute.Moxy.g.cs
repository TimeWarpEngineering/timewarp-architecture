// Generated from ..\..\..\Common\Common.Contracts\Mixins\RouteMixin.mixin at 2024-04-28 18:25:48 UTC
namespace TimeWarp.Architecture
{
    
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true)]
    internal class RouteMixinAttribute : System.Attribute
    {
        public string RouteTemplate { get; set; }
        public TimeWarp.Architecture.Features.HttpVerb HttpVerb { get; set; }
        
        public RouteMixinAttribute(
            string RouteTemplate,
            TimeWarp.Architecture.Features.HttpVerb HttpVerb)
        {
            this.RouteTemplate = RouteTemplate;
            this.HttpVerb = HttpVerb;
        }
    }
}

