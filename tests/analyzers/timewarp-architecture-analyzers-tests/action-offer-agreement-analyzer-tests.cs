#region Purpose
// Tests for TWA0029/TWA0030: [ActionOffer] records must name an explicit [CatalogAction] Name and match its constructor parameters.
#endregion

#region Design
// SPA-ness comes from a global analyzer config (build_property.UsingMicrosoftNETSdkBlazorWebAssembly),
// like the TWA0022 tests. Offers are in source for most cases (TWA0029 then anchors on the record);
// one case puts them in a referenced project — the real shape (web-contracts → web-spa) — where
// TWA0029 has no source location and reports at Location.None; another adds the production split
// where ActionOfferAttribute lives in a third assembly the contracts reference. Actions declared
// per-test mark their [CatalogAction] with markup so TWA0030 anchors without line arithmetic.
#endregion

// ReSharper disable InconsistentNaming
namespace ActionOfferAgreementAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Check_Offer_Agreement
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Check_Offer_Agreement>();

  private const string SpaGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  private const string AttributeStubs =
    """
    namespace TimeWarp.State
    {
      public sealed class CatalogActionAttribute : System.Attribute
      {
        public string Description { get; set; } = "";
        public string? Name { get; set; }
      }
    }
    namespace TimeWarp.Architecture.Attributes
    {
      public sealed class ActionOfferAttribute : System.Attribute
      {
        public ActionOfferAttribute(string catalogName) { CatalogName = catalogName; }
        public string CatalogName { get; }
        public string[] UserInput { get; set; } = new string[0];
      }
    }
    """;

  private const string Actions =
    """
    namespace App
    {
      using TimeWarp.State;

      public static class OfferedActionNames
      {
        public const string Revoke = "Credentials.RevokeCredential";
        public const string Rename = "Credentials.RenameCredential";
        public const string Link = "Credentials.LinkMicrosoft365";
      }

      public partial class CredentialsState
      {
        public static class RevokeCredentialActionSet
        {
          [CatalogAction(Name = OfferedActionNames.Revoke)]
          public sealed class Action
          {
            public Action(System.Guid credentialId) { }
          }
        }

        public static class RenameCredentialActionSet
        {
          [CatalogAction(Name = OfferedActionNames.Rename)]
          public sealed class Action
          {
            public Action(System.Guid credentialId, string nickname, bool notify = false) { }
          }
        }

        public static class LinkMicrosoft365ActionSet
        {
          [CatalogAction(Name = OfferedActionNames.Link)]
          public sealed class Action;
        }
      }
    }
    """;

  private static CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> Test(string offers, string actions = Actions, string? globalConfig = SpaGlobalConfig)
  {
    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = new()
    {
      ReferenceAssemblies = ReferenceAssemblies.Net.Net80
    };
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", AttributeStubs));
    test.TestState.Sources.Add(("Actions.cs", actions));
    test.TestState.Sources.Add(("Offers.cs", offers));
    return test;
  }

  private const string MatchingOffers =
    """
    namespace App
    {
      using TimeWarp.Architecture.Attributes;

      [ActionOffer(OfferedActionNames.Revoke)]
      public sealed record RevokeCredentialOffer(System.Guid CredentialId);

      [ActionOffer(OfferedActionNames.Rename, UserInput = new[] { "nickname" })]
      public sealed record RenameCredentialOffer(System.Guid CredentialId)
      {
        public const string Ignored = "consts are not arguments";
      }

      [ActionOffer(OfferedActionNames.Link)]
      public sealed record LinkMicrosoft365Offer;
    }
    """;

  public static async Task Given_Matching_Offers_Reports_Nothing() =>
    await Test(MatchingOffers).RunAsync();

  public static async Task Given_Non_Spa_Compilation_Reports_Nothing()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.Nothing")]
        public sealed record NothingOffer;
      }
      """;

    await Test(offers, globalConfig: null).RunAsync();
  }

  public static async Task Given_Unknown_Name_Reports_TWA0029_On_The_Record()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.DeleteEverything")]
        public sealed record {|#0:DeleteEverythingOffer|};
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(new DiagnosticResult("TWA0029", DiagnosticSeverity.Warning)
      .WithLocation(0)
      .WithArguments("App.DeleteEverythingOffer", "Credentials.DeleteEverything", NoAction("Credentials.DeleteEverything")));
    await test.RunAsync();
  }

  public static async Task Given_Action_Without_Explicit_Name_Reports_TWA0029()
  {
    // The derived default name (Credentials.RevokeCredential) does not count — renaming the action
    // set would silently change it.
    const string actions =
      """
      namespace App
      {
        public partial class CredentialsState
        {
          public static class RevokeCredentialActionSet
          {
            [TimeWarp.State.CatalogAction(Description = "Revoke.")]
            public sealed class Action
            {
              public Action(System.Guid credentialId) { }
            }
          }
        }
      }
      """;
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record {|#0:RevokeCredentialOffer|}(System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(new DiagnosticResult("TWA0029", DiagnosticSeverity.Warning)
      .WithLocation(0)
      .WithArguments("App.RevokeCredentialOffer", "Credentials.RevokeCredential", NoAction("Credentials.RevokeCredential")));
    await test.RunAsync();
  }

  public static async Task Given_Property_With_No_Parameter_Reports_TWA0030()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer(System.Guid CredentialKey);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(Mismatch(16, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "property 'CredentialKey' binds 'credentialKey', which is not a constructor parameter of App.CredentialsState.RevokeCredentialActionSet.Action"));
    test.ExpectedDiagnostics.Add(Mismatch(16, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "required parameter 'credentialId' has no property and is not listed in UserInput"));
    await test.RunAsync();
  }

  public static async Task Given_Property_Of_Wrong_Type_Reports_TWA0030()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer(string CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(Mismatch(16, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "property 'CredentialId' is string but parameter 'credentialId' is System.Guid"));
    await test.RunAsync();
  }

  public static async Task Given_Required_Parameter_Unbound_And_Not_UserInput_Reports_TWA0030()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Rename)]
        public sealed record RenameCredentialOffer(System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(Mismatch(25, "App.RenameCredentialOffer", "Credentials.RenameCredential",
      "required parameter 'nickname' has no property and is not listed in UserInput"));
    await test.RunAsync();
  }

  public static async Task Given_Bad_UserInput_Reports_TWA0030()
  {
    // 'notify' is optional, 'missing' does not exist, 'credentialId' is already bound.
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Rename, UserInput = new[] { "nickname", "notify", "missing", "credentialId" })]
        public sealed record RenameCredentialOffer(System.Guid CredentialId);
      }
      """;

    const string action = "App.CredentialsState.RenameCredentialActionSet.Action";
    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(Mismatch(25, "App.RenameCredentialOffer", "Credentials.RenameCredential",
      $"UserInput 'notify' is not a required constructor parameter of {action}"));
    test.ExpectedDiagnostics.Add(Mismatch(25, "App.RenameCredentialOffer", "Credentials.RenameCredential",
      $"UserInput 'missing' is not a required constructor parameter of {action}"));
    test.ExpectedDiagnostics.Add(Mismatch(25, "App.RenameCredentialOffer", "Credentials.RenameCredential",
      "UserInput 'credentialId' is also bound by a property; the user cannot replace an offered argument"));
    await test.RunAsync();
  }

  public static async Task Given_JsonPropertyName_Binds_By_Wire_Name()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer([property: System.Text.Json.Serialization.JsonPropertyName("credentialId")] System.Guid Id);
      }
      """;

    await Test(offers).RunAsync();
  }

  public static async Task Given_Gate_False_Reports_Nothing()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.Nothing")]
        public sealed record NothingOffer;
      }
      """;

    await Test(offers, globalConfig: "is_global = true\nbuild_property.UsingMicrosoftNETSdkBlazorWebAssembly = false").RunAsync();
  }

  public static async Task Given_No_Catalog_Actions_Reports_TWA0029()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record {|#0:RevokeCredentialOffer|}(System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions: "namespace App { }");
    test.ExpectedDiagnostics.Add(new DiagnosticResult("TWA0029", DiagnosticSeverity.Warning)
      .WithLocation(0)
      .WithArguments("App.RevokeCredentialOffer", "Credentials.RevokeCredential", NoAction("Credentials.RevokeCredential")));
    await test.RunAsync();
  }

  public static async Task Given_Two_Actions_With_One_Name_Reports_TWA0029()
  {
    const string actions =
      """
      namespace App
      {
        public static class ZetaActionSet
        {
          [TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")]
          public sealed class Action
          {
            public Action(System.Guid credentialId) { }
          }
        }

        public static class AlphaActionSet
        {
          [TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")]
          public sealed class Action
          {
            public Action(string other) { }
          }
        }
      }
      """;
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record {|#0:RevokeCredentialOffer|}(System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(new DiagnosticResult("TWA0029", DiagnosticSeverity.Warning)
      .WithLocation(0)
      .WithArguments("App.RevokeCredentialOffer", "Credentials.RevokeCredential",
        "more than one [CatalogAction] sets that Name (App.AlphaActionSet.Action, App.ZetaActionSet.Action); catalog names must be unique"));
    await test.RunAsync();
  }

  public static async Task Given_Multiple_Constructors_Uses_The_First_Declared()
  {
    // The offer matches the SECOND constructor; TimeWarp.State catalogs the first, so it is refused.
    const string actions =
      """
      namespace App
      {
        public static class RevokeCredentialActionSet
        {
          [{|#1:TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")|}]
          public sealed class Action
          {
            public Action(string nickname) { }
            public Action(System.Guid credentialId) { }
          }
        }
      }
      """;
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "property 'CredentialId' binds 'credentialId', which is not a constructor parameter of App.RevokeCredentialActionSet.Action"));
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "required parameter 'nickname' has no property and is not listed in UserInput"));
    await test.RunAsync();
  }

  public static async Task Given_Constructor_In_Another_Partial_Declaration_Treats_Action_As_Parameterless()
  {
    // TimeWarp.State parses only the declaration carrying [CatalogAction]; a constructor in another
    // partial declaration is not in the catalog.
    const string actions =
      """
      namespace App
      {
        public static partial class RevokeCredentialActionSet
        {
          [{|#1:TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")|}]
          public sealed partial class Action;

          public sealed partial class Action
          {
            public Action(System.Guid credentialId) { }
          }
        }
      }
      """;
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "property 'CredentialId' binds 'credentialId', which is not a constructor parameter of App.RevokeCredentialActionSet.Action"));
    await test.RunAsync();
  }

  public static async Task Given_Bound_Parameter_After_Omitted_Optional_Reports_TWA0030()
  {
    const string actions =
      """
      namespace App
      {
        public static class RevokeCredentialActionSet
        {
          [{|#1:TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")|}]
          public sealed class Action
          {
            public Action(System.Guid credentialId, bool notify = false, string tag = "") { }
          }
        }
      }
      """;
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId, string Tag);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "parameter 'tag' is bound after the omitted optional parameter 'notify'; the client binder cannot leave a positional hole"));
    await test.RunAsync();
  }

  public static async Task Given_Nullable_Reference_Property_For_Non_Nullable_Parameter_Reports_TWA0030()
  {
    const string actions =
      """
      #nullable enable
      namespace App
      {
        public static class RenameCredentialActionSet
        {
          [{|#1:TimeWarp.State.CatalogAction(Name = "Credentials.RenameCredential")|}]
          public sealed class Action
          {
            public Action(System.Guid credentialId, string nickname) { }
          }
        }
      }
      """;
    const string offers =
      """
      #nullable enable
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RenameCredential")]
        public sealed record RenameCredentialOffer(System.Guid CredentialId, string? Nickname);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RenameCredentialOffer", "Credentials.RenameCredential",
      "property 'Nickname' is nullable but parameter 'nickname' is required; the client binder treats null as missing"));
    await test.RunAsync();
  }

  public static async Task Given_Nullable_Value_Property_For_Non_Nullable_Parameter_Reports_TWA0030()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer(System.Guid? CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(Mismatch(16, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "property 'CredentialId' is System.Guid? but parameter 'credentialId' is System.Guid"));
    await test.RunAsync();
  }

  public static async Task Given_JsonIgnore_Property_Skips_It()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId, [property: System.Text.Json.Serialization.JsonIgnore] string Note);
      }
      """;

    await Test(offers).RunAsync();
  }

  public static async Task Given_Inherited_Property_Binds()
  {
    const string offers =
      """
      namespace App
      {
        public abstract record CredentialOfferBase
        {
          public System.Guid CredentialId { get; init; }
        }

        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer : CredentialOfferBase;
      }
      """;

    await Test(offers).RunAsync();
  }

  public static async Task Given_JsonPropertyName_Naming_No_Parameter_Reports_TWA0030()
  {
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer([property: System.Text.Json.Serialization.JsonPropertyName("credential")] System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(Mismatch(16, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "property 'CredentialId' binds 'credential', which is not a constructor parameter of App.CredentialsState.RevokeCredentialActionSet.Action"));
    test.ExpectedDiagnostics.Add(Mismatch(16, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "required parameter 'credentialId' has no property and is not listed in UserInput"));
    await test.RunAsync();
  }

  public static async Task Given_Catalog_Action_On_A_Record_Reports_TWA0029()
  {
    // TimeWarp.State catalogs class declarations only; the enclosing class's Handler constructor must
    // not stand in for the record's parameters.
    const string actions =
      """
      namespace App
      {
        public static class RevokeCredentialActionSet
        {
          [TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")]
          public sealed record Action(System.Guid CredentialId);

          public sealed class Handler
          {
            public Handler(System.Guid credentialId) { }
          }
        }
      }
      """;
    const string offers =
      """
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record {|#0:RevokeCredentialOffer|}(System.Guid CredentialId);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(new DiagnosticResult("TWA0029", DiagnosticSeverity.Warning)
      .WithLocation(0)
      .WithArguments("App.RevokeCredentialOffer", "Credentials.RevokeCredential", NoAction("Credentials.RevokeCredential")));
    await test.RunAsync();
  }

  public static async Task Given_Nullable_Property_For_Nullable_Required_Parameter_Reports_TWA0030()
  {
    // The binder treats null as missing for any required parameter, whatever its annotation.
    const string actions =
      """
      #nullable enable
      namespace App
      {
        public static class RenameCredentialActionSet
        {
          [{|#1:TimeWarp.State.CatalogAction(Name = "Credentials.RenameCredential")|}]
          public sealed class Action
          {
            public Action(System.Guid? credentialId, string? nickname) { }
          }
        }
      }
      """;
    const string offers =
      """
      #nullable enable
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RenameCredential")]
        public sealed record RenameCredentialOffer(System.Guid? CredentialId, string? Nickname);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RenameCredentialOffer", "Credentials.RenameCredential",
      "property 'CredentialId' is nullable but parameter 'credentialId' is required; the client binder treats null as missing"));
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RenameCredentialOffer", "Credentials.RenameCredential",
      "property 'Nickname' is nullable but parameter 'nickname' is required; the client binder treats null as missing"));
    await test.RunAsync();
  }

  public static async Task Given_Bound_Parameter_After_Nullable_Optional_Reports_TWA0030()
  {
    // A null Tag is omitted on the client, which would leave a hole before notify.
    const string actions =
      """
      #nullable enable
      namespace App
      {
        public static class RevokeCredentialActionSet
        {
          [{|#1:TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")|}]
          public sealed class Action
          {
            public Action(System.Guid credentialId, string? tag = null, bool notify = false) { }
          }
        }
      }
      """;
    const string offers =
      """
      #nullable enable
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId, string? Tag, bool Notify);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers, actions);
    test.ExpectedDiagnostics.Add(MismatchAt(1, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "parameter 'notify' is bound after the optional parameter 'tag', which a null (nullable property) omits; the client binder cannot leave a positional hole"));
    await test.RunAsync();
  }

  public static async Task Given_Trailing_Nullable_Optional_Reports_Nothing()
  {
    const string actions =
      """
      #nullable enable
      namespace App
      {
        public static class RevokeCredentialActionSet
        {
          [TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")]
          public sealed class Action
          {
            public Action(System.Guid credentialId, string? tag = null) { }
          }
        }
      }
      """;
    const string offers =
      """
      #nullable enable
      namespace App
      {
        [TimeWarp.Architecture.Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId, string? Tag);
      }
      """;

    await Test(offers, actions).RunAsync();
  }

  public static async Task Given_Conditional_JsonIgnore_Still_Checks_The_Property()
  {
    // Condition = WhenWritingNull still writes a non-null value; Condition = Always never does.
    const string offers =
      """
      namespace App
      {
        using System.Text.Json.Serialization;

        [TimeWarp.Architecture.Attributes.ActionOffer(OfferedActionNames.Revoke)]
        public sealed record RevokeCredentialOffer(
          System.Guid CredentialId,
          [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Note,
          [property: JsonIgnore(Condition = JsonIgnoreCondition.Always)] string Hidden);
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = Test(offers);
    test.ExpectedDiagnostics.Add(Mismatch(16, "App.RevokeCredentialOffer", "Credentials.RevokeCredential",
      "property 'Note' binds 'note', which is not a constructor parameter of App.CredentialsState.RevokeCredentialActionSet.Action"));
    await test.RunAsync();
  }

  public static async Task Given_Offers_In_A_Referenced_Assembly_Checks_Them()
  {
    // The real shape: records live in contracts, the SPA references them.
    const string contracts =
      """
      namespace Contracts
      {
        public sealed class ActionOfferAttribute : System.Attribute
        {
          public ActionOfferAttribute(string catalogName) { }
          public string[] UserInput { get; set; } = new string[0];
        }

        [ActionOffer("Credentials.RevokeCredential")]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId);

        [ActionOffer("Credentials.Gone")]
        public sealed record GoneOffer;
      }
      """;
    const string spa =
      """
      namespace TimeWarp.State
      {
        public sealed class CatalogActionAttribute : System.Attribute
        {
          public string? Name { get; set; }
        }
      }
      namespace App
      {
        public static class RevokeCredentialActionSet
        {
          [TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")]
          public sealed class Action
          {
            public Action(System.Guid credentialId) { }
          }
        }
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = new()
    {
      ReferenceAssemblies = ReferenceAssemblies.Net.Net80
    };
    test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", SpaGlobalConfig));
    test.TestState.Sources.Add(("Spa.cs", spa));
    test.TestState.AdditionalProjects["Contracts"].Sources.Add(("Contracts.cs", contracts));
    test.TestState.AdditionalProjects["Contracts"].ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
    test.TestState.AdditionalProjectReferences.Add("Contracts");
    test.ExpectedDiagnostics.Add(new DiagnosticResult("TWA0029", DiagnosticSeverity.Warning)
      .WithNoLocation()
      .WithArguments("Contracts.GoneOffer", "Credentials.Gone", NoAction("Credentials.Gone")));
    await test.RunAsync();
  }

  public static async Task Given_Attribute_In_A_Third_Assembly_Still_Checks_Offers()
  {
    // Production split: ActionOfferAttribute in the attributes package, records in contracts (which
    // references it), the SPA referencing both (MSBuild flows project references transitively) — the
    // OfferAssemblies branch that admits contracts because it references the defining assembly.
    const string attributes =
      """
      namespace Attributes
      {
        public sealed class ActionOfferAttribute : System.Attribute
        {
          public ActionOfferAttribute(string catalogName) { }
          public string[] UserInput { get; set; } = new string[0];
        }
      }
      """;
    const string contracts =
      """
      namespace Contracts
      {
        [Attributes.ActionOffer("Credentials.RevokeCredential")]
        public sealed record RevokeCredentialOffer(System.Guid CredentialId);

        [Attributes.ActionOffer("Credentials.Gone")]
        public sealed record GoneOffer;
      }
      """;
    const string spa =
      """
      namespace TimeWarp.State
      {
        public sealed class CatalogActionAttribute : System.Attribute
        {
          public string? Name { get; set; }
        }
      }
      namespace App
      {
        public static class RevokeCredentialActionSet
        {
          [TimeWarp.State.CatalogAction(Name = "Credentials.RevokeCredential")]
          public sealed class Action
          {
            public Action(System.Guid credentialId) { }
          }
        }
      }
      """;

    CSharpAnalyzerTest<ActionOfferAgreementAnalyzer, RoslynTestVerifier> test = new()
    {
      ReferenceAssemblies = ReferenceAssemblies.Net.Net80
    };
    test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", SpaGlobalConfig));
    test.TestState.Sources.Add(("Spa.cs", spa));
    test.TestState.AdditionalProjects["Attributes"].Sources.Add(("Attributes.cs", attributes));
    test.TestState.AdditionalProjects["Attributes"].ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
    test.TestState.AdditionalProjects["Contracts"].Sources.Add(("Contracts.cs", contracts));
    test.TestState.AdditionalProjects["Contracts"].ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
    test.TestState.AdditionalProjects["Contracts"].AdditionalProjectReferences.Add("Attributes");
    test.TestState.AdditionalProjectReferences.Add("Contracts");
    test.TestState.AdditionalProjectReferences.Add("Attributes");
    test.ExpectedDiagnostics.Add(new DiagnosticResult("TWA0029", DiagnosticSeverity.Warning)
      .WithNoLocation()
      .WithArguments("Contracts.GoneOffer", "Credentials.Gone", NoAction("Credentials.Gone")));
    await test.RunAsync();
  }

  public static Task CamelCase_Matches_System_Text_Json()
  {
    foreach (string name in new[] { "CredentialId", "ID", "URLValue", "X", "already", "IOStream", "A B" })
    {
      ActionOfferAgreementAnalyzer.CamelCase(name).ShouldBe(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(name), name);
    }

    return Task.CompletedTask;
  }

  private static string NoAction(string catalogName) =>
    $"no [CatalogAction] in this compilation sets Name = \"{catalogName}\"; set [CatalogAction(Name = <shared constant>)] on the offered action";

  private static DiagnosticResult MismatchAt(int markup, string offer, string catalogName, string problem) =>
    new DiagnosticResult("TWA0030", DiagnosticSeverity.Warning)
      .WithLocation(markup)
      .WithArguments(offer, catalogName, problem);

  private static DiagnosticResult Mismatch(int line, string offer, string catalogName, string problem) =>
    new DiagnosticResult("TWA0030", DiagnosticSeverity.Warning)
      .WithSpan("Actions.cs", line, 8, line, 55)
      .WithArguments(offer, catalogName, problem);
}
