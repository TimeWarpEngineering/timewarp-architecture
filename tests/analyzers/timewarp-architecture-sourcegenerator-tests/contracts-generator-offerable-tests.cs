#region Purpose
// Tests for the contracts generator's [Offerable] output (task 281): Offer record + OfferName shape, auth-field exclusion, TWE012/TWE013, and incremental caching.
#endregion

#region Design
// The attribute stubs live in Stub.Attributes (not TimeWarp.Architecture.Attributes) to prove the
// generator resolves [ActionOffer] from the [Offerable] attribute's own namespace instead of spelling
// the sourceName-rewritten Attributes namespace. IAuthApiRequest is stubbed in
// TimeWarp.Foundation.Features like the real foundation contract.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using TimeWarp.Foundation.Contracts.Generators;

public class ContractsGeneratorOfferable_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContractsGeneratorOfferable_Tests>();

  private const string Stubs = """
    namespace TimeWarp.Foundation.Features
    {
        public enum HttpVerb { Get, Post, Put, Delete, Patch, Head, Options }
        public interface IAuthApiRequest { System.Guid UserId { get; set; } }
    }
    namespace Stub.Attributes
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public sealed class OfferableAttribute : System.Attribute
        {
            public string[] UserInput { get; set; } = new string[0];
        }

        [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
        public sealed class ActionOfferAttribute : System.Attribute
        {
            public ActionOfferAttribute(string catalogName) { CatalogName = catalogName; }
            public string CatalogName { get; }
            public string[] UserInput { get; set; } = new string[0];
        }
    }
    """;

  private const string RenameSource = """
    using Stub.Attributes;
    using TimeWarp.Foundation.Features;

    namespace Test.Features.Identity;

    public interface ICredentialActionOffer { System.Guid CredentialId { get; } }

    [Offerable(UserInput = [nameof(Command.Nickname)])]
    public static partial class RenameCredential
    {
        partial record Offer : ICredentialActionOffer;

        [ApiRoute("api/identity/credentials/{CredentialId:guid}/rename", HttpVerb.Post)]
        public sealed partial class Command : IAuthApiRequest
        {
            public System.Guid UserId { get; set; }
            public string Nickname { get; set; } = null!;
            public int? Priority { get; set; }
            public string Computed => Nickname;
            public static string Ignored { get; set; } = "";
        }
    }
    """;

  private static CSharpCompilation CreateCompilation(string source) =>
    CSharpCompilation.Create(
      "Test.Contracts",
      [CSharpSyntaxTree.ParseText(Stubs), CSharpSyntaxTree.ParseText(source)],
      [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

  private static GeneratorDriver CreateDriver(bool trackSteps = false) =>
    CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new ContractsGenerator().AsSourceGenerator()),
      driverOptions: trackSteps
        ? new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        : default);

  private static GeneratorDriverRunResult RunResult(string source) =>
    CreateDriver().RunGenerators(CreateCompilation(source)).GetRunResult();

  private static string? OfferSource(GeneratorDriverRunResult result) =>
    result.Results
      .SelectMany(static r => r.GeneratedSources)
      .Where(static s => s.HintName.EndsWith(".Offer.g.cs", StringComparison.Ordinal))
      .Select(static s => s.SourceText.ToString())
      .SingleOrDefault();

  public static Task Should_Emit_Offer_Record_And_OfferName()
  {
    GeneratorDriverRunResult result = RunResult(RenameSource);
    string generated = OfferSource(result).ShouldNotBeNull();

    result.Diagnostics.ShouldBeEmpty();
    result.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.HintName)
      .ShouldContain("Test.Features.Identity.RenameCredential.Offer.g.cs");
    generated.ShouldContain("namespace Test.Features.Identity;");
    generated.ShouldContain("partial class RenameCredential");
    generated.ShouldContain("public const string OfferName = \"Identity.RenameCredential\";");
    generated.ShouldContain("[global::Stub.Attributes.ActionOfferAttribute(OfferName, UserInput = [\"nickname\"])]");
    // Route parameter first (generated in the same pass), then non-user Command properties.
    generated.ShouldContain("public sealed partial record Offer(global::System.Guid CredentialId, int? Priority);");
    return Task.CompletedTask;
  }

  public static Task Should_Exclude_Auth_Filled_UserId()
  {
    string generated = OfferSource(RunResult(RenameSource)).ShouldNotBeNull();
    generated.ShouldNotContain("UserId");

    // [AuthApiRequest] (which generates UserId + IAuthApiRequest) counts too, even when UserId is not declared.
    const string attributeAuth = """
      using Stub.Attributes;
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Identity;

      [Offerable]
      public static partial class RevokeCredential
      {
          [AuthApiRequest]
          [ApiRoute("api/identity/credentials/{CredentialId:guid}/revoke", HttpVerb.Post)]
          public sealed partial class Command { }
      }
      """;
    string revoke = OfferSource(RunResult(attributeAuth)).ShouldNotBeNull();
    revoke.ShouldContain("public sealed partial record Offer(global::System.Guid CredentialId);");
    revoke.ShouldContain("[global::Stub.Attributes.ActionOfferAttribute(OfferName)]");

    // Without IAuthApiRequest a UserId property is an ordinary bound argument.
    const string noAuth = """
      using Stub.Attributes;

      namespace Test.Features.Admin;

      [Offerable]
      public static partial class AssignOwner
      {
          public sealed partial class Command
          {
              public System.Guid UserId { get; set; }
          }
      }
      """;
    string assign = OfferSource(RunResult(noAuth)).ShouldNotBeNull();
    assign.ShouldContain("public sealed partial record Offer(global::System.Guid UserId);");
    assign.ShouldContain("public const string OfferName = \"Admin.AssignOwner\";");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE012_For_UserInput_That_Names_No_Command_Property()
  {
    const string source = """
      using Stub.Attributes;

      namespace Test.Features.Identity;

      [Offerable(UserInput = ["Nickname", "Colour"])]
      public static partial class RenameCredential
      {
          public sealed partial class Command
          {
              public string Nickname { get; set; } = "";
          }
      }
      """;

    GeneratorDriverRunResult result = RunResult(source);
    Diagnostic diagnostic = result.Diagnostics.ShouldHaveSingleItem();
    diagnostic.Id.ShouldBe("TWE012");
    diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
    diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("'Colour'");
    diagnostic.Location.GetLineSpan().StartLinePosition.Line.ShouldBe(4);
    OfferSource(result).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE013_For_Contract_Without_Command()
  {
    const string source = """
      using Stub.Attributes;

      namespace Test.Features.Identity;

      [Offerable]
      public static partial class GetCredentials
      {
          public sealed partial class Query { }
      }
      """;

    GeneratorDriverRunResult result = RunResult(source);
    Diagnostic diagnostic = result.Diagnostics.ShouldHaveSingleItem();
    diagnostic.Id.ShouldBe("TWE013");
    diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
    OfferSource(result).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Should_Compile_Generated_Offer_With_Partial_Interface()
  {
    _ = CreateDriver().RunGeneratorsAndUpdateCompilation(CreateCompilation(RenameSource), out Compilation updated, out _);
    ImmutableArray<Diagnostic> errors = [.. updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];
    errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));

    INamedTypeSymbol offer = updated.GetTypeByMetadataName("Test.Features.Identity.RenameCredential+Offer").ShouldNotBeNull();
    offer.AllInterfaces.Select(static i => i.Name).ShouldContain("ICredentialActionOffer");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Modify_Offer_Output_When_Only_Trivia_Changes()
  {
    CSharpCompilation compilation = CreateCompilation(RenameSource);
    GeneratorDriver driver = CreateDriver(trackSteps: true);
    driver = driver.RunGenerators(compilation);

    SyntaxTree sourceTree = compilation.SyntaxTrees.Single(static tree => tree.ToString().Contains("RenameCredential", StringComparison.Ordinal));
    string trivia = sourceTree.ToString().Replace(
      "public static partial class RenameCredential",
      "/* trivia */ public static partial class RenameCredential",
      StringComparison.Ordinal);
    compilation = compilation.ReplaceSyntaxTree(sourceTree, CSharpSyntaxTree.ParseText(trivia));
    driver = driver.RunGenerators(compilation);

    ImmutableArray<IncrementalStepRunReason> reasons =
    [
      .. driver.GetRunResult().Results
        .SelectMany(static r => r.TrackedOutputSteps)
        .SelectMany(static pair => pair.Value)
        .SelectMany(static step => step.Outputs)
        .Select(static output => output.Reason)
    ];
    reasons.ShouldNotBeEmpty();
    reasons.ShouldNotContain(IncrementalStepRunReason.Modified);
    reasons.ShouldNotContain(IncrementalStepRunReason.New);
    return Task.CompletedTask;
  }
}
