#region Purpose
// End-to-end proof of the admin feedback endpoints under agent bearer tokens: per-scope grants, the
// four-scope workflow, and role-assigned agents not inheriting human grants (task 378).
#endregion

#region Design
// Mirrors principals-authorization-tests: real HTTP, isolated clients, passkey cookie mint,
// roles via IPrincipalRoleStore. Anonymous is 401. A Member-only session is 403 on the admin
// list and on a status change. An Administrator lists, changes status, and the filer then sees
// the new status and the reply on their own item (the filer view never shows an internal
// note). Read-without-manage shrinks the Administrator bundle to admin.feedback.read: list 200,
// status change 403. Attachment download shrinks the
// bundle to admin.access + admin.feedback.read with no feedback.file.self: the admin reads the
// filer's attachment (200) while another Member gets 404, so the route never gates on the filer
// permission. The original bundle is restored in finally.
// Task 336: an Administrator sees the filer's display name on the list row and the detail; an
// Operator granted admin.access + admin.feedback.read without admin.profiles.read (the
// reachable no-PII shape, as in principals-authorization-tests) sees ids only.
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
  public static Task SetupOnce() => AdminFeedbackAuthorizationFixture.AcquireAsync();

  public static Task CleanUpOnce() => AdminFeedbackAuthorizationFixture.ReleaseAsync();

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Agent_Returns_>();

  public static async Task Scoped_Given_Agent_Bearer_Tokens()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    Guid itemId;
    using (HttpClient filer = Client(filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Agent triage target");
    }

    string readToken = await IssueTokenAsync(AgentScopes.FeedbackRead);
    string noteToken = await IssueTokenAsync(AgentScopes.FeedbackNote);
    string severityToken = await IssueTokenAsync(AgentScopes.FeedbackSeverity);
    string statusToken = await IssueTokenAsync(AgentScopes.FeedbackStatus);
    string everyToken = await IssueTokenAsync(
      AgentScopes.FeedbackRead,
      AgentScopes.FeedbackNote,
      AgentScopes.FeedbackSeverity,
      AgentScopes.FeedbackStatus);

    using (HttpClient reader = Bearer(readToken))
    {
      HttpResponseMessage list = await reader.GetAsync(new ListFeedback.Query().GetRouteWithQueryString());
      list.StatusCode.ShouldBe(HttpStatusCode.OK);
      (await list.Content.ReadAsStringAsync()).ShouldContain(itemId.ToString());
      (await PostNoteAsync(reader, itemId, "from read")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutSeverityAsync(reader, itemId, FeedbackSeverity.Low)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutStatusAsync(reader, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(reader, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    using (HttpClient note = Bearer(noteToken))
    {
      (await note.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode
        .ShouldBe(HttpStatusCode.Forbidden);
      (await PutSeverityAsync(note, itemId, FeedbackSeverity.Low)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutStatusAsync(note, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(note, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      (await PostNoteAsync(note, itemId, "internal")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    using (HttpClient severity = Bearer(severityToken))
    {
      (await severity.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode
        .ShouldBe(HttpStatusCode.Forbidden);
      (await PostNoteAsync(severity, itemId, "nope")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutStatusAsync(severity, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(severity, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      (await PutSeverityAsync(severity, itemId, FeedbackSeverity.Low)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    using (HttpClient status = Bearer(statusToken))
    {
      (await status.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode
        .ShouldBe(HttpStatusCode.Forbidden);
      (await PostNoteAsync(status, itemId, "nope")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutSeverityAsync(status, itemId, FeedbackSeverity.High)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostReplyAsync(status, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      (await PutStatusAsync(status, itemId, FeedbackStatus.UnderReview)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    using HttpClient every = Bearer(everyToken);
    (await PostReplyAsync(every, itemId)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Ok_Given_Four_Scope_Token_Processes_A_New_Item()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    Guid itemId;
    using (HttpClient filer = Client(filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Reviewer loop target");
    }

    string token = await IssueTokenAsync(
      AgentScopes.FeedbackRead,
      AgentScopes.FeedbackNote,
      AgentScopes.FeedbackSeverity,
      AgentScopes.FeedbackStatus);

    using HttpClient reviewer = Bearer(token);
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
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    string displayName = await EnsureProfileAsync(filerId);
    Guid itemId;
    Guid attachmentId;
    using (HttpClient filer = Client(filerCookie))
    {
      attachmentId = await UploadAttachmentAsync(filer, "notes.txt", "attached by the filer"u8.ToArray());
      itemId = await FileFeedbackAsync(filer, "Role assigned agent", attachmentId);
    }

    (PrincipalId notePrincipal, string noteToken) = await IssueTokenForPrincipalAsync(AgentScopes.FeedbackNote);
    await SetRolesAsync(notePrincipal, RoleIds.Administrator);
    using (HttpClient note = Bearer(noteToken))
    {
      (await note.GetAsync(DownloadFeedbackAttachment.DownloadPath(attachmentId))).StatusCode
        .ShouldBe(HttpStatusCode.NotFound);
    }

    (PrincipalId readPrincipal, string readToken) = await IssueTokenForPrincipalAsync(AgentScopes.FeedbackRead);
    await SetRolesAsync(readPrincipal, RoleIds.Administrator);
    using HttpClient reader = Bearer(readToken);
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
