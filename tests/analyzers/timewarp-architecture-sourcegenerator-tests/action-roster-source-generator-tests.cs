#region Purpose
// Verifies the ActionRoster generator (task 301): every nested *ActionSet.Action that implements
// TimeWarp.Mediator.IAction is listed with no opt-in attribute; [CommandPaletteOptOut] reasons are
// carried verbatim; only actions without a required constructor parameter get an Execute delegate.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;

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

        public static class HiddenActionSet
        {
          private sealed class Action : TimeWarp.Mediator.IAction;
        }

        public static class GenericActionSet<T>
        {
          public sealed class Action : TimeWarp.Mediator.IAction;
        }

        public static class GenericActionActionSet
        {
          public sealed class Action<T> : TimeWarp.Mediator.IAction;
        }

        public static class ConstReasonActionSet
        {
          [CommandPaletteOptOut(OptOutReasons.StartupLoad)]
          public sealed class Action : TimeWarp.Mediator.IAction;
        }

        public static partial class PartialReasonActionSet
        {
          public sealed partial class Action : TimeWarp.Mediator.IAction;
        }
      }

      internal static class OptOutReasons
      {
        public const string StartupLoad = "Startup load (const).";
      }
    }
    """;

  // The second partial declaration of PartialReasonActionSet.Action, in its own file.
  private const string PartialSource = """
    namespace Test.Features.Applications
    {
      public partial class AgentSurfaceState
      {
        public static partial class PartialReasonActionSet
        {
          [CommandPaletteOptOut("Opted out on another partial.")]
          public sealed partial class Action;
        }
      }
    }
    """;

  private static string RunAndConcat(params string[] sources)
  {
    var compilation = CSharpCompilation.Create(
      "Test.WebSpa",
      sources.Select(static source => CSharpSyntaxTree.ParseText(source)),
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

  public static Task Should_Skip_A_Private_Action()
  {
    string generated = RunAndConcat(Source);

    generated.ShouldNotContain("AgentSurface.Hidden");
    return Task.CompletedTask;
  }

  public static Task Should_Skip_Generic_ActionSets_And_Actions()
  {
    string generated = RunAndConcat(Source);

    generated.ShouldNotContain("AgentSurface.Generic\"");
    generated.ShouldNotContain("GenericActionSet");
    generated.ShouldNotContain("AgentSurface.GenericAction");
    return Task.CompletedTask;
  }

  public static Task Should_Honor_A_Const_Opt_Out_Reason()
  {
    string generated = RunAndConcat(Source);

    generated.ShouldContain("ConstReasonActionSet.Action), 0, \"Startup load (const).\", static");
    return Task.CompletedTask;
  }

  public static Task Should_Honor_An_Opt_Out_On_Another_Partial_Declaration()
  {
    string generated = RunAndConcat(Source, PartialSource);

    generated.ShouldContain("PartialReasonActionSet.Action), 0, \"Opted out on another partial.\", static");
    return Task.CompletedTask;
  }
}
