#region Purpose
// Tests for the contracts generator's [Offerable] output (task 281): Offer record + OfferName shape (nested contracts, inherited Command properties, route-parameter UserInput), auth-field exclusion, TWE012/TWE013/TWE014, and incremental caching.
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
using Microsoft.CodeAnalysis.Text;
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

  public static Task Should_Report_TWE012_For_Duplicate_And_Auth_Filled_UserInput()
  {
    const string duplicate = """
      using Stub.Attributes;

      namespace Test.Features.Identity;

      [Offerable(UserInput = ["Nickname", "Nickname"])]
      public static partial class RenameCredential
      {
          public sealed partial class Command
          {
              public string Nickname { get; set; } = "";
          }
      }
      """;
    GeneratorDriverRunResult duplicateResult = RunResult(duplicate);
    Diagnostic duplicateDiagnostic = duplicateResult.Diagnostics.ShouldHaveSingleItem();
    duplicateDiagnostic.Id.ShouldBe("TWE012");
    duplicateDiagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("'Nickname', which is listed more than once");
    OfferSource(duplicateResult).ShouldBeNull();

    const string userId = """
      using Stub.Attributes;
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Identity;

      [Offerable(UserInput = ["UserId"])]
      public static partial class RenameCredential
      {
          public sealed partial class Command : IAuthApiRequest
          {
              public System.Guid UserId { get; set; }
              public string Nickname { get; set; } = "";
          }
      }
      """;
    GeneratorDriverRunResult userIdResult = RunResult(userId);
    Diagnostic userIdDiagnostic = userIdResult.Diagnostics.ShouldHaveSingleItem();
    userIdDiagnostic.Id.ShouldBe("TWE012");
    string message = userIdDiagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
    message.ShouldContain("'UserId', which is the auth-filled UserId");
    message.ShouldNotContain("names no property");
    OfferSource(userIdResult).ShouldBeNull();

    // An entry that names nothing says so (the third message argument is the reason).
    const string unknown = """
      using Stub.Attributes;

      namespace Test.Features.Identity;

      [Offerable(UserInput = ["Colour"])]
      public static partial class RenameCredential
      {
          public sealed partial class Command { }
      }
      """;
    RunResult(unknown).Diagnostics.ShouldHaveSingleItem().GetMessage(System.Globalization.CultureInfo.InvariantCulture)
      .ShouldContain("'Colour', which names no property of Test.Features.Identity.RenameCredential.Command");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE013_On_The_Offerable_Attribute()
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

    Diagnostic diagnostic = RunResult(source).Diagnostics.ShouldHaveSingleItem();
    diagnostic.Id.ShouldBe("TWE013");
    LinePositionSpan span = diagnostic.Location.GetLineSpan().Span;
    span.Start.ShouldBe(new LinePosition(4, 1));
    span.End.ShouldBe(new LinePosition(4, "[Offerable".Length));
    return Task.CompletedTask;
  }

  [Input("record")]
  [Input("non-partial")]
  [Input("global-namespace")]
  [Input("non-partial-container")]
  [Input("generic")]
  public static Task Should_Report_TWE014_For_A_Type_That_Cannot_Carry_The_Offer(string shape)
  {
    string source = shape switch
    {
      "record" => """
        using Stub.Attributes;
        namespace Test.Features.Identity;
        [Offerable]
        public sealed partial record RenameCredential
        {
            public sealed partial class Command { public string Nickname { get; set; } = ""; }
        }
        """,
      "non-partial" => """
        using Stub.Attributes;
        namespace Test.Features.Identity;
        [Offerable]
        public static class RenameCredential
        {
            public sealed class Command { public string Nickname { get; set; } = ""; }
        }
        """,
      "global-namespace" => """
        using Stub.Attributes;
        [Offerable]
        public static partial class RenameCredential
        {
            public sealed partial class Command { public string Nickname { get; set; } = ""; }
        }
        """,
      "generic" => """
        using Stub.Attributes;
        namespace Test.Features.Identity;
        [Offerable]
        public static partial class RenameCredential<T>
        {
            public sealed partial class Command { public string Nickname { get; set; } = ""; }
        }
        """,
      _ => """
        using Stub.Attributes;
        namespace Test.Features.Identity;
        public static class CredentialOperations
        {
            [Offerable]
            public static partial class RenameCredential
            {
                public sealed partial class Command { public string Nickname { get; set; } = ""; }
            }
        }
        """
    };

    GeneratorDriverRunResult result = RunResult(source);
    Diagnostic diagnostic = result.Diagnostics.ShouldHaveSingleItem();
    diagnostic.Id.ShouldBe("TWE014");
    diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
    diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("RenameCredential");
    OfferSource(result).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Offer_For_Contract_Nested_In_A_Container()
  {
    const string source = """
      using Stub.Attributes;
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Identity;

      public static partial class CredentialOperations
      {
          [Offerable]
          public static partial class RevokeCredential
          {
              [ApiRoute("api/identity/credentials/{CredentialId:guid}/revoke", HttpVerb.Post)]
              public sealed partial class Command { }
          }
      }
      """;

    GeneratorDriverRunResult result = RunResult(source);
    result.Diagnostics.ShouldBeEmpty();
    result.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.HintName)
      .ShouldContain("Test.Features.Identity.CredentialOperations.RevokeCredential.Offer.g.cs");
    string generated = OfferSource(result).ShouldNotBeNull();
    generated.ShouldContain("partial class CredentialOperations");
    generated.ShouldContain("public const string OfferName = \"Identity.RevokeCredential\";");
    generated.ShouldContain("public sealed partial record Offer(global::System.Guid CredentialId);");

    _ = CreateDriver().RunGeneratorsAndUpdateCompilation(CreateCompilation(source), out Compilation updated, out _);
    ShouldHaveNoErrors(updated);
    updated.GetTypeByMetadataName("Test.Features.Identity.CredentialOperations+RevokeCredential+Offer").ShouldNotBeNull();
    return Task.CompletedTask;
  }

  public static Task Should_Bind_Inherited_Command_Properties_Once()
  {
    const string source = """
      using Stub.Attributes;

      namespace Test.Features.Admin;

      public abstract class TallyCommandBase
      {
          public string Name { get; set; } = "";
          public int Count { get; set; }
          public string ReadOnly => Name;
      }

      [Offerable]
      public static partial class Tally
      {
          public sealed partial class Command : TallyCommandBase
          {
              public new int Count { get; set; }
              public bool Flag { get; set; }
          }
      }
      """;

    GeneratorDriverRunResult result = RunResult(source);
    result.Diagnostics.ShouldBeEmpty();
    // Most-derived first; the hiding Count wins over the base one (bound once); getter-only skipped.
    OfferSource(result).ShouldNotBeNull().ShouldContain("public sealed partial record Offer(int Count, bool Flag, string Name);");

    _ = CreateDriver().RunGeneratorsAndUpdateCompilation(CreateCompilation(source), out Compilation updated, out _);
    ShouldHaveNoErrors(updated);
    return Task.CompletedTask;
  }

  public static Task Should_Accept_UserInput_Naming_A_Route_Parameter()
  {
    const string source = """
      using Stub.Attributes;
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Identity;

      [Offerable(UserInput = ["CredentialId"])]
      public static partial class RenameCredential
      {
          [ApiRoute("api/identity/credentials/{CredentialId:guid}/rename", HttpVerb.Post)]
          public sealed partial class Command
          {
              public string Nickname { get; set; } = "";
          }
      }
      """;

    GeneratorDriverRunResult result = RunResult(source);
    result.Diagnostics.ShouldBeEmpty();
    string generated = OfferSource(result).ShouldNotBeNull();
    generated.ShouldContain("[global::Stub.Attributes.ActionOfferAttribute(OfferName, UserInput = [\"credentialId\"])]");
    generated.ShouldContain("public sealed partial record Offer(string Nickname);");
    return Task.CompletedTask;
  }

  private static void ShouldHaveNoErrors(Compilation compilation)
  {
    ImmutableArray<Diagnostic> errors = [.. compilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];
    errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));
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
