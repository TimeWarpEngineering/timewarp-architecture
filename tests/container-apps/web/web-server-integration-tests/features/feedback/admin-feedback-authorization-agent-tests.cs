#region Purpose
// End-to-end proof of the admin feedback endpoints under agent bearer tokens: per-scope grants, the
// four-scope workflow, and role-assigned agents not inheriting human grants (task 378).
#endregion

#region Design
// Real HTTP, isolated clients, roles via IPrincipalRoleStore, as in the
// principals-authorization-*-tests files. Human sessions are in admin-feedback-authorization-human-tests.
// Task 378: a real agent bearer token is the only grant. feedback:read lists and is refused
// the writes. Each write scope is refused the other writes and the list. Reply stays on the
// human schemes, so an agent token is 401 even when it holds every feedback scope. A
// principal who also holds Administrator is still evaluated as the agent token: a
// feedback:note token cannot download, and a feedback:read token does not receive the
// owner display name.
// One token that holds all four feedback scopes lists Status = New, reads the item, sets
// severity, adds a note, and moves status off New. The next Status = New list omits it.
#endregion

namespace AdminFeedbackAuthorization_;

using System.Net;
using System.Text.Json;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Identity;
using static AdminFeedbackAuthorization_.AdminFeedbackAuthorizationFixture;

public class Agent_Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  public static async Task SetupOnce() => Graph = await AdminFeedbackAuthorizationFixture.CreateHostAsync();

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Agent_Returns_>();

  public static async Task Scoped_Given_Agent_Bearer_Tokens()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie(Web);
    await SetRolesAsync(Web, filerId, RoleIds.Member);
    Guid itemId;
    using (HttpClient filer = Client(Web, filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Agent triage target");
    }

    string readToken = await IssueTokenAsync(Web, AgentScopes.FeedbackRead);
    string noteToken = await IssueTokenAsync(Web, AgentScopes.FeedbackNote);
    string severityToken = await IssueTokenAsync(Web, AgentScopes.FeedbackSeverity);
    string statusToken = await IssueTokenAsync(Web, AgentScopes.FeedbackStatus);
    string everyToken = await IssueTokenAsync(Web,
      AgentScopes.FeedbackRead,
      AgentScopes.FeedbackNote,
      AgentScopes.FeedbackSeverity,
      AgentScopes.FeedbackStatus);

    using (HttpClient reader = Bearer(Web, readToken))
    {
      HttpResponseMessage list = await reader.GetAsync(new ListFeedback.Query().GetRouteWithQueryString());
      list.StatusCode.ShouldBe(HttpStatusCode.OK);
      (await list.Content.ReadAsStringAsync()).ShouldContain(itemId.ToString());
      (await PostNoteAsync(reader, itemId, "from read")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutSeverityAsync(reader, itemId, FeedbackSeverity.Low)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutStatusAsync(reader, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(reader, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    using (HttpClient note = Bearer(Web, noteToken))
    {
      (await note.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode
        .ShouldBe(HttpStatusCode.Forbidden);
      (await PutSeverityAsync(note, itemId, FeedbackSeverity.Low)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutStatusAsync(note, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(note, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      (await PostNoteAsync(note, itemId, "internal")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    using (HttpClient severity = Bearer(Web, severityToken))
    {
      (await severity.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode
        .ShouldBe(HttpStatusCode.Forbidden);
      (await PostNoteAsync(severity, itemId, "nope")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutStatusAsync(severity, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(severity, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      (await PutSeverityAsync(severity, itemId, FeedbackSeverity.Low)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    using (HttpClient status = Bearer(Web, statusToken))
    {
      (await status.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode
        .ShouldBe(HttpStatusCode.Forbidden);
      (await PostNoteAsync(status, itemId, "nope")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutSeverityAsync(status, itemId, FeedbackSeverity.High)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(status, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      (await PutStatusAsync(status, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    using HttpClient every = Bearer(Web, everyToken);
    (await PostReplyAsync(every, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Ok_Given_Four_Scope_Token_Processes_A_New_Item()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie(Web);
    await SetRolesAsync(Web, filerId, RoleIds.Member);
    Guid itemId;
    using (HttpClient filer = Client(Web, filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Reviewer loop target");
    }

    string token = await IssueTokenAsync(Web,
      AgentScopes.FeedbackRead,
      AgentScopes.FeedbackNote,
      AgentScopes.FeedbackSeverity,
      AgentScopes.FeedbackStatus);

    using HttpClient reviewer = Bearer(Web, token);
    ListFeedback.Query listNew = new()
    {
      Status = FeedbackStatus.New,
      PageSize = ListFeedback.MaxPageSize,
    };
    string listRoute = listNew.GetRouteWithQueryString();
    ListFeedback.Response before = await ReadAsync<ListFeedback.Response>(await reviewer.GetAsync(listRoute));
    before.Items.ShouldContain(item => item.FeedbackItemId == itemId);

    GetFeedbackForAdmin.Response detail = await ReadAsync<GetFeedbackForAdmin.Response>(
      await reviewer.GetAsync($"api/admin/feedback/{itemId}"));
    detail.Item.FeedbackItemId.ShouldBe(itemId);

    (await PutSeverityAsync(reviewer, itemId, FeedbackSeverity.High)).StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PostNoteAsync(reviewer, itemId, "Reviewed by the agent")).StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PutStatusAsync(reviewer, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.OK);

    ListFeedback.Response after = await ReadAsync<ListFeedback.Response>(await reviewer.GetAsync(listRoute));
    after.Items.ShouldNotContain(item => item.FeedbackItemId == itemId);
  }

  public static async Task Role_Assigned_Agent_Should_Not_Inherit_Human_Grants()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie(Web);
    await SetRolesAsync(Web, filerId, RoleIds.Member);
    string displayName = await EnsureProfileAsync(Web, filerId);
    Guid itemId;
    Guid attachmentId;
    using (HttpClient filer = Client(Web, filerCookie))
    {
      attachmentId = await UploadAttachmentAsync(filer, "notes.txt", "attached by the filer"u8.ToArray());
      itemId = await FileFeedbackAsync(filer, "Role assigned agent", attachmentId);
    }

    (PrincipalId notePrincipal, string noteToken) = await IssueTokenForPrincipalAsync(Web, AgentScopes.FeedbackNote);
    await SetRolesAsync(Web, notePrincipal, RoleIds.Administrator);
    using (HttpClient note = Bearer(Web, noteToken))
    {
      (await note.GetAsync(DownloadFeedbackAttachment.DownloadPath(attachmentId))).StatusCode
        .ShouldBe(HttpStatusCode.NotFound);
    }

    (PrincipalId readPrincipal, string readToken) = await IssueTokenForPrincipalAsync(Web, AgentScopes.FeedbackRead);
    await SetRolesAsync(Web, readPrincipal, RoleIds.Administrator);
    using HttpClient reader = Bearer(Web, readToken);
    HttpResponseMessage download = await reader.GetAsync(DownloadFeedbackAttachment.DownloadPath(attachmentId));
    download.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await download.Content.ReadAsStringAsync()).ShouldBe("attached by the filer");

    HttpResponseMessage listResponse = await reader.GetAsync(
      new ListFeedback.Query { PageSize = ListFeedback.MaxPageSize }.GetRouteWithQueryString());
    listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    string listJson = await listResponse.Content.ReadAsStringAsync();
    listJson.ShouldNotContain(displayName);
    ListFeedback.Response list = JsonSerializer
      .Deserialize<ListFeedback.Response>(listJson, ContractSerializationDefaults.Options)
      .ShouldNotBeNull();
    ListFeedback.Item row = list.Items.Single(item => item.FeedbackItemId == itemId);
    row.OwnerPrincipalId.ShouldBe(filerId.Value);
    row.OwnerDisplayName.ShouldBeNull();

    HttpResponseMessage detailResponse = await reader.GetAsync($"api/admin/feedback/{itemId}");
    detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    string detailJson = await detailResponse.Content.ReadAsStringAsync();
    detailJson.ShouldNotContain(displayName);
    JsonSerializer.Deserialize<GetFeedbackForAdmin.Response>(detailJson, ContractSerializationDefaults.Options)
      .ShouldNotBeNull().Item.OwnerDisplayName.ShouldBeNull();
  }
}
