#region Purpose
// Verifies the [ContractState] generator (task 483): the emitted ActionSet, handler and dispatch method compile with
// the hooks a person writes, and a missing required hook is CS8795 (the 464 §4f data-loss case cannot compile).
#endregion

#region Design
// The contracts compile into their own assembly, as web-contracts does for web-spa, so the generator reads them from
// metadata. The host holds small stand-ins for the web-spa and library types the generated code names
// (DefaultApiHandler, IBaseAction, IStore, State<T>.Sender, ...); ContractStateHostSources has them. References are
// the runtime's own assemblies plus TimeWarp.Contracts (for [Destructive]), so no stand-in collides with a real type.
// Each test runs the generator, then compiles the result, and checks the compiler's diagnostics.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;

public class ContractStateSourceGenerator_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContractStateSourceGenerator_Tests>();

  private const string AllHooks = """
    namespace App.Features.Things
    {
      using App.Contracts;

      [App.Features.ContractState(typeof(GetThings), typeof(SaveThing), typeof(RemoveThing), DraftRequests = [typeof(SaveThing)])]
      public sealed partial class ThingState : TimeWarp.State.State<ThingState>
      {
        public int Loaded { get; private set; }

        private partial void OnGetThingsLoaded(GetThings.Response response) => Loaded = response.Count;

        private partial void BuildSaveThingRequest(SaveThing.Command request) => request.Names = ["draft"];

        private partial System.Threading.Tasks.Task<bool> ConfirmRemoveThingAsync(
          RemoveThing.Command request,
          App.Features.IConfirmationService confirmation,
          System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.FromResult(true);
      }
    }
    """;

  public static Task Should_Compile_When_Every_Required_Hook_Is_Written()
  {
    (Compilation output, ImmutableArray<Diagnostic> generatorDiagnostics, string generated) = Run(AllHooks);

    generatorDiagnostics.ShouldBeEmpty();
    Errors(output).ShouldBeEmpty();
    generated.ShouldContain("public static class GetThingsActionSet");
    generated.ShouldContain("internal sealed class Handler : global::App.Features.DefaultApiHandler<Action, global::App.Contracts.GetThings.Query, global::App.Contracts.GetThings.Response>");
    generated.ShouldContain("[global::TimeWarp.Features.ActionTracking.TrackActionAttribute]");
    generated.ShouldContain("public async global::System.Threading.Tasks.Task GetThings(global::App.Contracts.GetThings.Query? request = null,");
    generated.ShouldContain("public async global::System.Threading.Tasks.Task SaveThing(global::App.Contracts.SaveThing.Command request,");
    return Task.CompletedTask;
  }

  public static Task Should_Report_CS8795_When_The_Fetch_Seed_Hook_Is_Missing()
  {
    // 464 §4f: a fetch that does not seed its draft must not compile.
    string source = AllHooks.Replace(
      "private partial void OnGetThingsLoaded(GetThings.Response response) => Loaded = response.Count;", "", StringComparison.Ordinal);

    (Compilation output, _, _) = Run(source);

    Diagnostic error = Errors(output).ShouldHaveSingleItem();
    error.Id.ShouldBe("CS8795");
    error.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("OnGetThingsLoaded");
    return Task.CompletedTask;
  }

  public static Task Should_Report_CS8795_When_The_Draft_Build_Hook_Is_Missing()
  {
    string source = AllHooks.Replace(
      "private partial void BuildSaveThingRequest(SaveThing.Command request) => request.Names = [\"draft\"];", "", StringComparison.Ordinal);

    (Compilation output, _, _) = Run(source);

    Diagnostic error = Errors(output).ShouldHaveSingleItem();
    error.Id.ShouldBe("CS8795");
    error.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("BuildSaveThingRequest");
    return Task.CompletedTask;
  }

  public static Task Should_Report_CS8795_When_A_Destructive_Confirm_Hook_Is_Missing()
  {
    int start = AllHooks.IndexOf("private partial System.Threading.Tasks.Task<bool> ConfirmRemoveThingAsync(", StringComparison.Ordinal);
    int end = AllHooks.IndexOf("Task.FromResult(true);", start, StringComparison.Ordinal) + "Task.FromResult(true);".Length;
    string source = AllHooks.Remove(start, end - start);

    (Compilation output, _, string generated) = Run(source);

    generated.ShouldContain("protected override global::System.Threading.Tasks.Task<bool> ConfirmAsync(Action action,");
    Diagnostic error = Errors(output).ShouldHaveSingleItem();
    error.Id.ShouldBe("CS8795");
    error.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("ConfirmRemoveThingAsync");
    return Task.CompletedTask;
  }

  public static Task Should_Declare_Optional_Hooks_As_Partial_Void()
  {
    (Compilation output, _, string generated) = Run(AllHooks);

    generated.ShouldContain("partial void OnSaveThingSucceeded(global::App.Contracts.SaveThing.Response response);");
    generated.ShouldContain("partial void OnRemoveThingSucceeded(global::App.Contracts.RemoveThing.Response response);");
    generated.ShouldContain("partial void OnBuildingGetThingsRequest(global::App.Contracts.GetThings.Query request);");
    generated.ShouldNotContain("partial void OnBuildingSaveThingRequest");
    generated.ShouldNotContain("private partial void OnSaveThingSucceeded");
    Errors(output).ShouldBeEmpty("Optional hooks may stay unwritten.");
    return Task.CompletedTask;
  }

  public static Task Should_Copy_Settable_Members_And_Stamp_Nothing()
  {
    (_, _, string generated) = Run(AllHooks);

    generated.ShouldContain("Names = action.Request.Names,");
    generated.ShouldNotContain("UserId");
    generated.ShouldNotContain("Computed = action.Request.Computed");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE015_For_A_Contract_Without_A_Response()
  {
    string source = AllHooks.Replace("typeof(RemoveThing), DraftRequests", "typeof(RemoveThing), typeof(Broken), DraftRequests", StringComparison.Ordinal);

    (Compilation output, ImmutableArray<Diagnostic> generatorDiagnostics, string generated) = Run(source);

    Diagnostic diagnostic = generatorDiagnostics.ShouldHaveSingleItem();
    diagnostic.Id.ShouldBe("TWE015");
    diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("Broken needs exactly one nested Query or Command and a nested Response");
    generated.ShouldContain("GetThingsActionSet", customMessage: "One bad contract does not drop the others.");
    Errors(output).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE015_For_A_Draft_Request_That_Is_Not_A_Contract()
  {
    string source = AllHooks.Replace("DraftRequests = [typeof(SaveThing)]", "DraftRequests = [typeof(SaveThing), typeof(Broken)]", StringComparison.Ordinal);

    (_, ImmutableArray<Diagnostic> generatorDiagnostics, _) = Run(source);

    generatorDiagnostics.ShouldHaveSingleItem().GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("DraftRequests names Broken");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE015_When_The_State_Already_Has_The_Action_Set()
  {
    string source = AllHooks.Replace(
      "public int Loaded { get; private set; }",
      "public int Loaded { get; private set; }\n    public static class GetThingsActionSet { }",
      StringComparison.Ordinal);

    (_, ImmutableArray<Diagnostic> generatorDiagnostics, string generated) = Run(source);

    generatorDiagnostics.ShouldHaveSingleItem().GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("already declares GetThingsActionSet");
    generated.ShouldNotContain("public static class GetThingsActionSet");
    return Task.CompletedTask;
  }

  public static Task Should_List_Contract_Actions_In_The_ActionRoster()
  {
    (_, _, string generated) = Run(AllHooks, new ActionRosterSourceGenerator());

    generated.ShouldContain("new(\"Thing.GetThings\", typeof(global::App.Features.Things.ThingState), typeof(global::App.Features.Things.ThingState.GetThingsActionSet.Action), 0, \"Contract fetch generated by [ContractState]; the owning page loads it.\", static (store, cancellationToken) => store.GetState<global::App.Features.Things.ThingState>().GetThings(externalCancellationToken: cancellationToken)),");
    generated.ShouldContain("new(\"Thing.SaveThing\", typeof(global::App.Features.Things.ThingState), typeof(global::App.Features.Things.ThingState.SaveThingActionSet.Action), 1, null, null),");
    return Task.CompletedTask;
  }

  private static ImmutableArray<Diagnostic> Errors(Compilation output) =>
    [.. output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];

  private static (Compilation Output, ImmutableArray<Diagnostic> GeneratorDiagnostics, string Generated) Run(
    string stateSource,
    IIncrementalGenerator? extra = null)
  {
    CSharpParseOptions parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
    CSharpCompilation contracts = CSharpCompilation.Create(
      "App.Contracts",
      [CSharpSyntaxTree.ParseText(ContractStateHostSources.Contracts, parse)],
      References,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    using var stream = new MemoryStream();
    Microsoft.CodeAnalysis.Emit.EmitResult emitted = contracts.Emit(stream);
    emitted.Success.ShouldBeTrue(string.Join("\n", emitted.Diagnostics));

    CSharpCompilation host = CSharpCompilation.Create(
      "App.Host",
      [CSharpSyntaxTree.ParseText(ContractStateHostSources.Host, parse), CSharpSyntaxTree.ParseText(stateSource, parse)],
      [.. References, MetadataReference.CreateFromImage(stream.ToArray())],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    List<ISourceGenerator> generators = [new ContractStateSourceGenerator().AsSourceGenerator()];
    if (extra is not null) generators.Add(extra.AsSourceGenerator());
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, parseOptions: parse);
    driver = driver.RunGeneratorsAndUpdateCompilation(host, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);

    string generated = string.Join(
      Environment.NewLine,
      driver.GetRunResult().Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.SourceText.ToString()));
    return (output, diagnostics, generated);
  }

  private static readonly MetadataReference[] References =
  [
    .. Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll")
      .Where(static path => !Path.GetFileName(path).StartsWith("Microsoft.VisualBasic", StringComparison.Ordinal))
      .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)),
    MetadataReference.CreateFromFile(typeof(TimeWarp.Contracts.DestructiveAttribute).Assembly.Location),
  ];
}
