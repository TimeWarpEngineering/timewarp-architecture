#region Purpose
// Blocking head script that restores the shell nav width before the body paints.
#endregion

#region Design
// The choice is localStorage twe.shellNav ("full" or "rail"), a per-browser preference, not a
// server setting. Prerender cannot read it. This script runs in <head> and sets data-twe-nav on
// <html> before the parser reaches the nav, so the rail rules (keyed on --twe-nav-rail) apply on
// first paint. The value is a raw token, not Blazored JSON, because the script runs before the
// runtime. ToggleMenu and LoadShellNav call the same window functions. An absent key leaves the
// attribute unset and the breakpoint default stands. A storage failure leaves the breakpoint too.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Head script and browser calls for the shell nav width preference.</summary>
public static class ShellNavScript
{
  /// <summary>localStorage key. Raw <c>full</c> or <c>rail</c>, not a JSON string.</summary>
  public const string StorageKey = "twe.shellNav";

  /// <summary>Stored and attribute value for the 240px column.</summary>
  public const string Full = "full";

  /// <summary>Stored and attribute value for the icon rail.</summary>
  public const string Rail = "rail";

  /// <summary>Attribute on <c>html</c> that tokens.css turns into <c>--twe-nav-rail</c>.</summary>
  public const string AttributeName = "data-twe-nav";

  /// <summary>window function that writes the key and the attribute.</summary>
  public const string ApplyName = "tweShellNavApply";

  /// <summary>window function that returns <c>full</c>, <c>rail</c>, or null.</summary>
  public const string ReadName = "tweShellNavRead";

  /// <summary>window function that is true at 1280px and wider.</summary>
  public const string WideName = "tweShellNavWide";

  /// <summary>Script body with no surrounding script element.</summary>
  public static string Render() =>
    "(function () {\n"
    + "  var key = \"" + StorageKey + "\";\n"
    + "  var full = \"" + Full + "\";\n"
    + "  var rail = \"" + Rail + "\";\n"
    + "  function read() {\n"
    + "    try {\n"
    + "      var value = localStorage.getItem(key);\n"
    + "      return value === full || value === rail ? value : null;\n"
    + "    } catch (ignored) { return null; }\n"
    + "  }\n"
    + "  function apply(choice) {\n"
    + "    try {\n"
    + "      if (choice === full || choice === rail) {\n"
    + "        localStorage.setItem(key, choice);\n"
    + "        document.documentElement.setAttribute(\"" + AttributeName + "\", choice);\n"
    + "      } else {\n"
    + "        localStorage.removeItem(key);\n"
    + "        document.documentElement.removeAttribute(\"" + AttributeName + "\");\n"
    + "      }\n"
    + "    } catch (ignored) {}\n"
    + "  }\n"
    + "  window." + ApplyName + " = apply;\n"
    + "  window." + ReadName + " = read;\n"
    + "  window." + WideName + " = function () { return window.matchMedia(\"(min-width: 1280px)\").matches; };\n"
    + "  apply(read());\n"
    + "})();\n";

  /// <summary>Reads the stored choice. Null when it is absent or the runtime cannot call the page.</summary>
  public static async Task<string?> ReadAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    try
    {
      return await jsRuntime.InvokeAsync<string?>(ReadName, cancellationToken);
    }
    catch (Exception exception) when (IsUnavailable(exception))
    {
      return null;
    }
  }

  /// <summary>True at 1280px and wider. True when the page cannot answer, matching the wide default.</summary>
  public static async Task<bool> IsWideAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    try
    {
      return await jsRuntime.InvokeAsync<bool>(WideName, cancellationToken);
    }
    catch (Exception exception) when (IsUnavailable(exception))
    {
      return true;
    }
  }

  /// <summary>Persists <paramref name="choice"/> and sets <c>data-twe-nav</c>. A storage failure keeps the in-memory state.</summary>
  public static async Task ApplyAsync(IJSRuntime jsRuntime, string choice, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    ArgumentException.ThrowIfNullOrEmpty(choice);
    try
    {
      await jsRuntime.InvokeVoidAsync(ApplyName, cancellationToken, choice);
    }
    catch (Exception exception) when (IsUnavailable(exception))
    {
    }
  }

  private static bool IsUnavailable(Exception exception) =>
    exception is JSException or JSDisconnectedException or InvalidOperationException;
}
