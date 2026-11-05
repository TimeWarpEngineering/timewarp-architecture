#region Purpose
// Tests for the task 475 contract-agreement rules on the FeedbackItem shape: mirrored wire enums,
// child-class projection (activity entries), and a projection wrapped in one response member.
#endregion

#region Design
// Same harness and Edit discipline as Should_Check_Contract_Agreement (Roles and Profile): each
// drift edits a copy of ContractAgreementFeedbackShape and asserts the replaced text is present.
// Mirrored types print alike in short form, so their messages use the full type names.
#endregion

// ReSharper disable InconsistentNaming
namespace ContractAgreementAnalyzer_;

using TimeWarp.Architecture.Analyzers;
using static TimeWarp.Architecture.Analyzers.Tests.ContractAgreementFeedbackShape;
using static TimeWarp.Architecture.Analyzers.Tests.ContractAgreementTestFixture;

public class Should_Check_Contract_Agreement_For_Feedback_Shape
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Check_Contract_Agreement_For_Feedback_Shape>();

  private const string Root = "FeedbackItem";
  private const string WireKind = "App.Features.Feedback.FeedbackKind";
  private const string DomainKind = "App.Features.Feedback.Domain.FeedbackKind";
  private const string WireStatus = "App.Features.Feedback.FeedbackStatus";
  private const string DomainStatus = "App.Features.Feedback.Domain.FeedbackStatus";

  private static string Edit(string source, string oldText, string newText)
  {
    source.ShouldContain(oldText);
    return source.Replace(oldText, newText, StringComparison.Ordinal);
  }

  private static string Missing(string root, string member) =>
    $"'{root}' has no public member '{member}'; names match exactly, so rename the side that drifted";

  private static string Mismatch(string root, string member, string rootType, string contractType, string suffix = "") =>
    $"'{root}.{member}' is {rootType} but the contract member is {contractType}{suffix}";

  public static async Task Given_Feedback_Shape_IsClean()
  {
    await Test(Contracts, Domain).RunAsync();
  }

  public static async Task Given_Feedback_With_Other_Shapes_IsClean()
  {
    await Test([Contracts, RoleContracts, ProfileContracts], [Domain, RoleDomain, ProfileDomain]).RunAsync();
  }

  public static async Task Given_Wire_Enum_Member_Drifts_Flags_Details_And_Projection()
  {
    string contracts = Edit(Contracts, "public enum FeedbackKind { Bug = 0, Idea = 1 }", "public enum FeedbackKind { Bug = 0, Suggestion = 1 }");
    string reason = Mismatch(Root, "Kind", DomainKind, WireKind, "; the enum member names differ (contract only: Suggestion; root only: Idea)");

    await Test
    (
      contracts,
      Domain,
      Flag(ContractAgreementAnalyzer.DetailsMemberId, Root, "IFeedbackItemDetails", "Kind", Group, reason),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.FeedbackAdminView", "Kind", Root, Group, reason)
    ).RunAsync();
  }

  public static async Task Given_Nullable_Wire_Enum_Drifts_Flags_The_Child_Entry()
  {
    string domain = Edit(Domain, "public enum FeedbackStatus { New = 10, Planned = 11, Closed = 12 }", "public enum FeedbackStatus { New = 10, Planned = 11 }");
    const string suffix = "; the enum member names differ (contract only: Closed; root only: none)";

    await Test
    (
      Contracts,
      domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.FeedbackActivityEntry", "ToStatus", "FeedbackActivity", Group,
        Mismatch("FeedbackActivity", "ToStatus", DomainStatus + "?", WireStatus + "?", suffix)),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.FeedbackAdminView", "Status", Root, Group,
        Mismatch(Root, "Status", DomainStatus, WireStatus, suffix)),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.ListMyFeedback.Item", "Status", Root, Group,
        Mismatch(Root, "Status", DomainStatus, WireStatus, suffix))
    ).RunAsync();
  }

  public static async Task Given_Child_Entry_Member_Disagrees_Flags_TWA0041_On_The_Child()
  {
    string contracts = Edit(Contracts, "[Computed]\n    public Guid? AuthorPrincipalId", "public Guid? AuthorPrincipalId");

    await Test
    (
      contracts,
      Domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.FeedbackActivityEntry", "AuthorPrincipalId", "FeedbackActivity", Group,
        Mismatch("FeedbackActivity", "AuthorPrincipalId", "Guid", "Guid?"))
    ).RunAsync();
  }

  public static async Task Given_Child_Entry_Member_Missing_Flags_TWA0041_On_The_Child()
  {
    string contracts = Edit(Contracts, "public DateTimeOffset At { get; set; }", "public DateTimeOffset When { get; set; }");

    await Test
    (
      contracts,
      Domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.FeedbackActivityEntry", "When", "FeedbackActivity", Group,
        Missing("FeedbackActivity", "When"))
    ).RunAsync();
  }

  public static async Task Given_Wrapped_View_Member_Not_On_Root_Flags_TWA0041()
  {
    string contracts = Edit(Contracts, "[Computed]\n    public bool FilerHasEmail", "public bool FilerHasEmail");

    await Test
    (
      contracts,
      Domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.FeedbackAdminView", "FilerHasEmail", Root, Group, Missing(Root, "FilerHasEmail"))
    ).RunAsync();
  }

  public static async Task Given_Wrapped_List_Item_Drifts_Flags_TWA0041()
  {
    string contracts = Edit(Contracts, "public string Title { get; set; } = \"\";\n      public FeedbackStatus Status", "public string Name { get; set; } = \"\";\n      public FeedbackStatus Status");

    await Test
    (
      contracts,
      Domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.ListMyFeedback.Item", "Name", Root, Group, Missing(Root, "Name"))
    ).RunAsync();
  }

  public static async Task Given_Collection_Inside_Wrapped_View_Is_Not_A_Second_Wrapper_Flags_TWA0041()
  {
    // A wrapper is one level only: Attachments inside the wrapped admin view is not walked as a
    // projection of the root, so without [Computed] it is a missing root member.
    string contracts = Edit(Contracts, "[Computed]\n    public IReadOnlyList<Attachment> Attachments", "public IReadOnlyList<Attachment> Attachments");

    await Test
    (
      contracts,
      Domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.FeedbackAdminView", "Attachments", Root, Group, Missing(Root, "Attachments"))
    ).RunAsync();
  }

  public static async Task Given_Computed_Wrapped_Class_IsClean()
  {
    string contracts = Edit(Contracts, "[Computed]\n    public bool FilerHasEmail", "public bool FilerHasEmail");
    contracts = Edit(contracts, "public sealed class FeedbackAdminView", "[Computed]\n  public sealed class FeedbackAdminView");

    await Test(contracts, Domain).RunAsync();
  }

  public static async Task Given_Root_Enum_Against_Non_Enum_Flags_TWA0041()
  {
    // Only two enums compare by names; an enum against its underlying int is drift.
    string contracts = Edit(Contracts, "public FeedbackStatus Status { get; set; }\n    }\n    public sealed class Response : BaseResponse\n    {\n      public IReadOnlyList<Item>", "public int Status { get; set; }\n    }\n    public sealed class Response : BaseResponse\n    {\n      public IReadOnlyList<Item>");

    await Test
    (
      contracts,
      Domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Feedback.ListMyFeedback.Item", "Status", Root, Group, Mismatch(Root, "Status", "FeedbackStatus", "int"))
    ).RunAsync();
  }
}
