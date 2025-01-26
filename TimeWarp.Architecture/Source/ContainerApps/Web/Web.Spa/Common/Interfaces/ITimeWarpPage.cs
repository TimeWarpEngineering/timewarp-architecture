namespace TimeWarp.Architecture.Common.Interfaces;

public interface ITimeWarpPage
{
    static abstract string GetPageUrl();
    static abstract string Title { get; }
    static abstract RenderFragment NavigationIconContent { get; }
}
