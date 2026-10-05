#region Purpose
// Tests for TWA0029/TWA0030: [ActionOffer] records must name an explicit [CatalogAction] Name and match its constructor parameters.
#endregion

#region Design
// SPA-ness comes from a global analyzer config (build_property.UsingMicrosoftNETSdkBlazorWebAssembly),
// like the TWA0022 tests. Offers are in source for most cases (TWA0029 then anchors on the record);
// one case puts them in a referenced project — the real shape (web-contracts → web-spa) — where
// TWA0029 has no source location and reports at Location.None.
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
      .WithArguments("App.DeleteEverythingOffer", "Credentials.DeleteEverything"));
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
      .WithArguments("App.RevokeCredentialOffer", "Credentials.RevokeCredential"));
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
      .WithArguments("Contracts.GoneOffer", "Credentials.Gone"));
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

  private static DiagnosticResult Mismatch(int line, string offer, string catalogName, string problem) =>
    new DiagnosticResult("TWA0030", DiagnosticSeverity.Warning)
      .WithSpan("Actions.cs", line, 8, line, 55)
      .WithArguments(offer, catalogName, problem);
}
