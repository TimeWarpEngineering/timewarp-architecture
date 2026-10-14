namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;

// Verifies the ActionRoster generator (task 301): every nested *ActionSet.Action that implements
// TimeWarp.Mediator.IAction is listed with no opt-in attribute; [CommandPaletteOptOut] reasons are
// carried verbatim; only actions without a required constructor parameter get an Execute delegate.
public class ActionRosterSourceGenerator_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ActionRosterSourceGenerator_Tests>();

  private const string Source = """
    namespace TimeWarp.Mediator { public interface IAction { } }

    namespace Test.Features.Applications
    {
      public partial class AgentSurfaceState
      {
        public static class OpenAskPanelActionSet
        {
          public sealed class Action : TimeWarp.Mediator.IAction;
        }

        public static class LoadChatConfigurationActionSet
        {
          [CommandPaletteOptOut("Startup load.")]
          public sealed class Action : TimeWarp.Mediator.IAction;
        }

        public static class SetEditModeActionSet
        {
          public sealed class Action : TimeWarp.Mediator.IAction
          {
            public Action(int mode, bool remember = false) { }
          }
        }

        public static class NotAnActionActionSet
        {
          public sealed class Action { }
        }
      }
    }
    """;

  private static string RunAndConcat(string source)
  {
    var compilation = CSharpCompilation.Create(
      "Test.WebSpa",
      new[] { CSharpSyntaxTree.ParseText(source) },
      new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var options = new Dictionary<string, string> { ["build_property.RootNamespace"] = "TimeWarp.Architecture" };

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new ActionRosterSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    return string.Join(
      Environment.NewLine,
      result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
  }

  public static Task Should_Emit_OptOut_Attribute_In_RootNamespace()
  {
    string generated = RunAndConcat(Source);

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("internal sealed class CommandPaletteOptOutAttribute : System.Attribute");
    return Task.CompletedTask;
  }

  public static Task Should_List_Every_Action_Without_An_Opt_In()
  {
    string generated = RunAndConcat(Source);

    generated.ShouldContain("public static class ActionRoster");
    generated.ShouldContain(
      "new(\"AgentSurface.OpenAskPanel\", typeof(global::Test.Features.Applications.AgentSurfaceState), "
      + "typeof(global::Test.Features.Applications.AgentSurfaceState.OpenAskPanelActionSet.Action), 0, null, "
      + "static (store, cancellationToken) => store.GetState<global::Test.Features.Applications.AgentSurfaceState>()"
      + ".OpenAskPanel(externalCancellationToken: cancellationToken)),");
    generated.ShouldNotContain("AgentSurface.NotAnAction");
    return Task.CompletedTask;
  }

  public static Task Should_Carry_The_Opt_Out_Reason()
  {
    string generated = RunAndConcat(Source);

    generated.ShouldContain("LoadChatConfigurationActionSet.Action), 0, \"Startup load.\", static");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Execute_Actions_With_Required_Parameters()
  {
    string generated = RunAndConcat(Source);

    generated.ShouldContain("SetEditModeActionSet.Action), 1, null, null),");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_No_Roster_Without_Actions()
  {
    string generated = RunAndConcat("namespace Empty { public sealed class Nothing { } }");

    generated.ShouldNotContain("class ActionRoster");
    return Task.CompletedTask;
  }

  public static Task Should_Sort_Entries_By_Name()
  {
    string generated = RunAndConcat(Source);

    generated.IndexOf("AgentSurface.LoadChatConfiguration", StringComparison.Ordinal)
      .ShouldBeLessThan(generated.IndexOf("AgentSurface.OpenAskPanel", StringComparison.Ordinal));
    generated.IndexOf("AgentSurface.OpenAskPanel", StringComparison.Ordinal)
      .ShouldBeLessThan(generated.IndexOf("AgentSurface.SetEditMode", StringComparison.Ordinal));
    return Task.CompletedTask;
  }
}
