#region Purpose
// TWA0036: dates and times render as ISO 8601 — culture shortcuts, format-less ToString,
// non-ISO formats, custom formats without the invariant culture, interpolation holes (both
// lowerings), concatenation, and razor content are flagged.
#endregion

#region Design
// The razor case adds a RenderTreeBuilder stub and names the tree *_razor.g.cs, because that is
// where component markup compiles; a non-razor generated tree with the same code is exempt.
// IsApprovedFormat is pinned directly so the approved-shape table reads in one place.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

public class Should_Require_Iso_Date_Rendering
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Require_Iso_Date_Rendering>();

  private const string RenderTreeBuilderStub =
    """
    #region Purpose
    // Minimal RenderTreeBuilder stub for TWA0036 tests.
    #endregion
    namespace Microsoft.AspNetCore.Components.Rendering
    {
      public sealed class RenderTreeBuilder
      {
        public void AddContent(int sequence, string? textContent) { }
        public void AddContent(int sequence, object? textContent) { }
      }
    }
    """;

  private static CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "Feature.cs"
  )
  {
    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test = new()
    {
      ReferenceAssemblies = ReferenceAssemblies.Net.Net80
    };
    test.TestState.Sources.Add(("Stubs.cs", RenderTreeBuilderStub));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  private static DiagnosticResult Flag(int line, int startColumn, int endColumn, string what, string path = "Feature.cs") =>
    new DiagnosticResult(id: "TWA0036", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn)
      .WithArguments(what);

  public static async Task Given_Culture_Shortcuts_Flags()
  {
    const string source =
      """
      #region Purpose
      // Culture shortcut renderings.
      #endregion
      using System;

      public static class Render
      {
        public static string[] All(DateTime at, DateOnly day, TimeOnly time, DateTime? maybe) =>
        [
          at.ToShortDateString(),
          at.ToLongTimeString(),
          day.ToLongDateString(),
          time.ToShortTimeString(),
          maybe?.ToShortDateString() ?? "",
        ];
      }
      """;

    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(10, 5, 27, "ToShortDateString()"));
    test.ExpectedDiagnostics.Add(Flag(11, 5, 26, "ToLongTimeString()"));
    test.ExpectedDiagnostics.Add(Flag(12, 5, 27, "ToLongDateString()"));
    test.ExpectedDiagnostics.Add(Flag(13, 5, 29, "ToShortTimeString()"));
    test.ExpectedDiagnostics.Add(Flag(14, 11, 31, "ToShortDateString()"));
    await test.RunAsync();
  }

  public static async Task Given_ToString_Without_Format_Flags()
  {
    const string source =
      """
      #region Purpose
      // Format-less ToString renderings.
      #endregion
      using System;
      using System.Globalization;

      public static class Render
      {
        public static string[] All(DateTimeOffset at, DateTime? maybe) =>
        [
          at.ToString(),
          at.ToString(CultureInfo.InvariantCulture),
          maybe.ToString(),
        ];
      }
      """;

    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(11, 5, 18, "ToString() without a format"));
    test.ExpectedDiagnostics.Add(Flag(12, 5, 46, "ToString() without a format"));
    test.ExpectedDiagnostics.Add(Flag(13, 5, 21, "ToString() without a format"));
    await test.RunAsync();
  }

  public static async Task Given_Non_Iso_Formats_Flags()
  {
    const string source =
      """
      #region Purpose
      // Non-ISO format strings.
      #endregion
      using System;
      using System.Globalization;

      public static class Render
      {
        public static string[] All(DateTimeOffset at, DateTime day) =>
        [
          at.ToString("u"),
          day.ToString("MM/dd/yyyy"),
          at.ToString("t", CultureInfo.CurrentCulture),
          day.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        ];
      }
      """;

    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(11, 5, 21, "ToString(\"u\")"));
    test.ExpectedDiagnostics.Add(Flag(12, 5, 31, "ToString(\"MM/dd/yyyy\")"));
    test.ExpectedDiagnostics.Add(Flag(13, 5, 49, "ToString(\"t\")"));
    test.ExpectedDiagnostics.Add(Flag(14, 5, 70, "ToString(\"yyyy-MM-dd HH:mm:ss\")"));
    await test.RunAsync();
  }

  public static async Task Given_Iso_Formats_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // ISO renderings, plus a format the analyzer cannot see.
      #endregion
      using System;
      using System.Globalization;

      public static class Render
      {
        private const string DateFormat = "yyyy-MM-dd";

        public static string[] All(DateTimeOffset at, DateTime day, TimeOnly time, string runtimeFormat) =>
        [
          at.ToString("O", CultureInfo.InvariantCulture),
          day.ToString("s", CultureInfo.InvariantCulture),
          day.ToString(DateFormat, CultureInfo.InvariantCulture),
          at.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
          at.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
          time.ToString("HH:mm", CultureInfo.InvariantCulture),
          at.ToString("O"),
          day.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo),
          string.Create(CultureInfo.InvariantCulture, $"screenshot-{at.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}.png"),
          FormattableString.Invariant($"on {day:yyyy-MM-dd}"),
          $"at {at:O}",
          at.ToString(runtimeFormat, CultureInfo.InvariantCulture),
          "on " + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ];
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Iso_Custom_Format_Without_Invariant_Culture_Flags()
  {
    const string source =
      """
      #region Purpose
      // ISO-shaped custom formats that still follow the current culture.
      #endregion
      using System;
      using System.Globalization;

      public static class Render
      {
        public static string[] All(DateTimeOffset at, DateOnly day, IFormatProvider provider) =>
        [
          at.ToString("yyyy-MM-dd"),
          at.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture),
          day.ToString("yyyy-MM-dd", provider),
          $"Set {at.UtcDateTime:yyyy-MM-dd'T'HH:mm:ss'Z'}.",
          string.Create(CultureInfo.CurrentCulture, $"Set {day:yyyy-MM-dd}."),
          at.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ];
      }
      """;

    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(11, 5, 30, "ToString(\"yyyy-MM-dd\") without CultureInfo.InvariantCulture"));
    test.ExpectedDiagnostics.Add(Flag(12, 5, 58, "ToString(\"yyyy-MM-dd\") without CultureInfo.InvariantCulture"));
    test.ExpectedDiagnostics.Add(Flag(13, 5, 41, "ToString(\"yyyy-MM-dd\") without CultureInfo.InvariantCulture"));
    test.ExpectedDiagnostics.Add(Flag(14, 11, 52, "Interpolation format \":yyyy-MM-dd'T'HH:mm:ss'Z'\" without CultureInfo.InvariantCulture"));
    test.ExpectedDiagnostics.Add(Flag(15, 53, 69, "Interpolation format \":yyyy-MM-dd\" without CultureInfo.InvariantCulture"));
    await test.RunAsync();
  }

  public static async Task Given_Interpolated_String_Handler_Flags_Each_Non_Iso_Hole_Once()
  {
    // StringBuilder.Append($"…") binds a handler conversion: holes are AppendFormatted calls.
    const string source =
      """
      #region Purpose
      // Interpolated-string handler holes (AppendFormatted).
      #endregion
      using System;
      using System.Globalization;
      using System.Text;

      public static class Render
      {
        public static string All(DateTimeOffset at)
        {
          StringBuilder builder = new();
          builder.Append($"{at}");
          builder.Append($"{at:u}");
          builder.Append($"{at:yyyy-MM-dd}");
          builder.Append($"{at,10}");
          builder.Append($"{at:O}");
          builder.Append(CultureInfo.InvariantCulture, $"{at:yyyy-MM-dd}");
          return builder.ToString();
        }
      }
      """;

    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(13, 22, 26, "An interpolation hole without a format"));
    test.ExpectedDiagnostics.Add(Flag(14, 22, 28, "Interpolation format \":u\""));
    test.ExpectedDiagnostics.Add(Flag(15, 22, 37, "Interpolation format \":yyyy-MM-dd\" without CultureInfo.InvariantCulture"));
    test.ExpectedDiagnostics.Add(Flag(16, 22, 29, "An interpolation hole without a format"));
    await test.RunAsync();
  }

  public static async Task Given_Interpolation_And_Concatenation_Flags()
  {
    const string source =
      """
      #region Purpose
      // Implicit ToString through interpolation, concatenation, and +=.
      #endregion
      using System;

      public static class Render
      {
        public static object[] All(DateTimeOffset at, DateOnly day) =>
        [
          $"Set {at}.",
          $"Set {at:u}.",
          (FormattableString)$"Set {day}.",
          "on " + day,
        ];

        public static string Append(string text, DateOnly day)
        {
          text += day;
          return text;
        }
      }
      """;

    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(10, 11, 15, "An interpolation hole without a format"));
    test.ExpectedDiagnostics.Add(Flag(11, 11, 17, "Interpolation format \":u\""));
    test.ExpectedDiagnostics.Add(Flag(12, 30, 35, "An interpolation hole without a format"));
    test.ExpectedDiagnostics.Add(Flag(13, 13, 16, "String concatenation of a date"));
    test.ExpectedDiagnostics.Add(Flag(18, 13, 16, "String concatenation of a date"));
    await test.RunAsync();
  }

  public static async Task Given_Razor_Content_Of_A_Date_Flags()
  {
    // `<td>@forecast.Date</td>` compiles to AddContent(int, object) with the date boxed.
    const string source =
      """
      #region Purpose
      // Razor-generated tree rendering a date as content.
      #endregion
      using System;
      using Microsoft.AspNetCore.Components.Rendering;

      public class ForecastPage
      {
        public DateTime Date { get; } = DateTime.UnixEpoch;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.AddContent(0, Date);
          builder.AddContent(1, "plain text");
        }
      }
      """;

    CSharpAnalyzerTest<IsoDateRenderingAnalyzer, RoslynTestVerifier> test =
      Test(source, sourcePath: "Pages_ForecastPage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag(13, 5, 32, "Razor content of a date value", "Pages_ForecastPage_razor.g.cs"));
    await test.RunAsync();
  }

  public static async Task Given_Non_Razor_Generated_Tree_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Generated non-razor tree (EF migration designer shape).
      #endregion
      using System;

      public static class Snapshot
      {
        public static string Stamp(DateTime at) => at.ToString();
      }
      """;

    await Test(source, sourcePath: "20261028000000_Initial.Designer.cs").RunAsync();
  }

  public static async Task Given_Format_Table_Approves_Only_Iso_Shapes()
  {
    string[] approved =
    [
      "O", "o", "s",
      "yyyy", "yyyy-MM", "yyyy-MM-dd",
      "yyyy-MM-ddTHH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:sszzz",
      "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.fffK", "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
      "HH:mm", "HH:mm:ss", "HH:mm:ss.fff",
      "yyyyMMdd", "yyyyMMdd'T'HHmmss'Z'", "yyyyMMdd'T'HHmmss.fff'Z'",
    ];
    string[] rejected =
    [
      "", "u", "U", "R", "r", "d", "D", "t", "T", "g", "G", "f", "F", "M", "Y",
      "MM/dd/yyyy", "M/d/yyyy, h:mm:ss tt", "yyyy-MM-dd HH:mm:ss", "yyyyMMdd-HHmmss",
      "yyyyMMddHHmmssfff", "dd-MM-yyyy", "yyyy-M-d", "hh:mm", "HH:mm tt",
    ];

    foreach (string format in approved)
    {
      IsoDateRenderingAnalyzer.IsApprovedFormat(format).ShouldBeTrue(format);
    }

    foreach (string format in rejected)
    {
      IsoDateRenderingAnalyzer.IsApprovedFormat(format).ShouldBeFalse(format);
    }

    await Task.CompletedTask;
  }
}
