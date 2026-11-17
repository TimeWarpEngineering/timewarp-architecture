#region Purpose
// Shared private helpers and the ShellHost double for the TimeWarpPage_Should_ partial class.
#endregion

namespace TimeWarpPageRender_;

using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Configuration;

[TestTag("Unit")]
public partial class TimeWarpPage_Should_
{

  private static void SetProperty(object target, string name, object? value)
  {
    PropertyInfo? property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    property.ShouldNotBeNull(name);
    property.SetValue(target, value);
  }

  private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

  private static RenderFragment Mark(string dataQa, string text) => builder =>
  {
    builder.OpenElement(0, "span");
    builder.AddAttribute(1, "data-qa", dataQa);
    builder.AddContent(2, text);
    builder.CloseElement();
  };

  private static async Task<string> RenderAsync
  (
    Dictionary<string, object> parameters,
    BrandOptions? brand = null,
    ClaimsPrincipal? user = null
  )
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices(brand ?? new BrandOptions(), user).BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
    {
      Dictionary<string, object?> hostParameters = new()
      {
        ["PageParameters"] = parameters
      };
      return (await renderer.RenderComponentAsync<ShellHost>(ParameterView.FromDictionary(hostParameters))).ToHtmlString();
    });
    return WebUtility.HtmlDecode(html);
  }

  private static async Task<string> RenderFocusedAsync(BrandOptions brand)
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices(brand).BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<TimeWarpFocusedPage>(ParameterView.Empty)).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string UserCardTag(string html)
  {
    int classAt = html.IndexOf("twe-user-card", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "shell rendered no user card");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private sealed class ShellHost : ComponentBase
  {
    [Parameter] public Dictionary<string, object> PageParameters { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<TimeWarpPage>(0);
        child.AddMultipleAttributes(1, PageParameters);
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }
}
