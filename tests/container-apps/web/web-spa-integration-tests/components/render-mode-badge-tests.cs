#region Purpose
// The footer render-mode map: one icon and sentence per known RendererInfo.Name, text for any other.
#endregion

#region Design
// RenderModeBadge is pure data, so these cases call For directly. The static render of the shell
// (TimeWarpPage_Should_) covers the markup for the one name HtmlRenderer reports, Static; Server
// and WebAssembly only exist in a running app, so their icons and words are pinned here.
#endregion

namespace RenderModeBadge_;

using TimeWarp.Architecture.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

[TestTag("Unit")]
public class RenderModeBadge_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RenderModeBadge_Should_>();

  public static Task Map_Each_Known_Mode_To_Its_Icon_And_Sentence()
  {
    Expect("Static", typeof(Icons.Regular.Size16.Document), "Pre-rendered page, not yet interactive.");
    Expect("Server", typeof(Icons.Regular.Size16.Server), "Running on the server over a live connection.");
    Expect("WebAssembly", typeof(Icons.Regular.Size16.DeveloperBoard), "Running in your browser.");
    return Task.CompletedTask;
  }

  public static Task Fall_Back_To_The_Name_For_An_Unknown_Mode()
  {
    RenderModeBadge badge = RenderModeBadge.For("Hybrid");

    badge.Icon.ShouldBeNull();
    badge.Description.ShouldBeNull();
    badge.Label.ShouldBe("Hybrid");
    return Task.CompletedTask;
  }

  private static void Expect(string name, Type iconType, string sentence)
  {
    RenderModeBadge badge = RenderModeBadge.For(name);

    badge.Name.ShouldBe(name);
    badge.Icon.ShouldNotBeNull().ShouldBeOfType(iconType);
    badge.Description.ShouldBe(sentence);
    badge.Heading.ShouldBe($"Rendering: {name}");
    badge.Label.ShouldBe($"Rendering: {name}. {sentence}");
  }
}
