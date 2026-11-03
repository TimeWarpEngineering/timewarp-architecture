#region Purpose
// End-to-end proof that the admin feedback endpoints require admin.feedback.read / .manage for human
// sessions: anonymous, member, administrator, read-only, attachment download and owner display name (tasks 314, 336).
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

using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Identity;
using static AdminFeedbackAuthorization_.AdminFeedbackAuthorizationFixture;

public class Human_Returns_
{
  public static Task SetupOnce() => AdminFeedbackAuthorizationFixture.AcquireAsync();

  public static Task CleanUpOnce() => AdminFeedbackAuthorizationFixture.ReleaseAsync();

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Human_Returns_>();

  public static async Task Unauthorized_Given_Anonymous_List()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/admin/feedback");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Forbidden_Given_Member_Only_Session()
  {
    (string cookie, PrincipalId principalId) = await MintIdentitySessionCookie();
    await SetRolesAsync(principalId, RoleIds.Member);
    using HttpClient client = Client(cookie);

    Guid itemId = await FileFeedbackAsync(client, "Member filing");

    HttpResponseMessage list = await client.GetAsync(new ListFeedback.Query().GetRouteWithQueryString());
    list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

    HttpResponseMessage change = await PutStatusAsync(client, itemId, FeedbackStatus.Planned);
    change.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Ok_Given_Administrator_And_Filer_Sees_The_Outcome()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    using HttpClient filer = Client(filerCookie);
    Guid itemId = await FileFeedbackAsync(filer, "Please triage me");

    (string adminCookie, PrincipalId adminId) = await MintIdentitySessionCookie();
    await SetRolesAsync(adminId, RoleIds.Member, RoleIds.Administrator);
    using HttpClient admin = Client(adminCookie);

    HttpResponseMessage list = await admin.GetAsync(new ListFeedback.Query().GetRouteWithQueryString());
    list.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await list.Content.ReadAsStringAsync()).ShouldContain(itemId.ToString());

    (await PutStatusAsync(admin, itemId, FeedbackStatus.Planned)).StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PostAsync(admin, $"api/admin/feedback/{itemId}/replies", new { body = "On the roadmap." }))
      .StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PostAsync(admin, $"api/admin/feedback/{itemId}/notes", new { body = "Secret triage note" }))
      .StatusCode.ShouldBe(HttpStatusCode.OK);

    HttpResponseMessage own = await filer.GetAsync($"api/Feedback/{itemId}");
    own.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await own.Content.ReadAsStringAsync();
    json.ShouldContain("\"status\":\"Planned\"");
    json.ShouldContain("On the roadmap.");
    json.ShouldNotContain("Secret triage note");
    json.Contains("severity", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();

    (await PutSeverityAsync(admin, itemId, FeedbackSeverity.High)).StatusCode.ShouldBe(HttpStatusCode.OK);
    HttpResponseMessage afterReview = await filer.GetAsync($"api/Feedback/{itemId}");
    (await afterReview.Content.ReadAsStringAsync()).Contains("severity", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();

    (await PostAsync(filer, $"api/Feedback/{itemId}/replies", new { body = "Thank you!" }))
      .StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PostAsync(filer, $"api/admin/feedback/{itemId}/replies", new { body = "Pretending to be admin" }))
      .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Forbidden_Manage_Given_Administrator_With_Feedback_Read_Only()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    Guid itemId;
    using (HttpClient filer = Client(filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Read-only admin");
    }

    (string cookie, PrincipalId principalId) = await MintIdentitySessionCookie();
    await SetRolesAsync(principalId, RoleIds.Member, RoleIds.Administrator);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    IReadOnlyList<string> original = await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);
    try
    {
      await permissionStore.SetPermissionIdsForRoleAsync(
        RoleIds.Administrator,
        [PermissionIds.AdminAccess, PermissionIds.AdminFeedbackRead, PermissionIds.FeedbackRead, PermissionIds.FeedbackFileSelf]);

      using HttpClient client = Client(cookie);
      (await client.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode.ShouldBe(HttpStatusCode.OK);
      (await client.GetAsync($"api/admin/feedback/{itemId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
      (await PutStatusAsync(client, itemId, FeedbackStatus.Declined)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PutSeverityAsync(client, itemId, FeedbackSeverity.Low)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostAsync(client, $"api/admin/feedback/{itemId}/notes", new { body = "nope" }))
        .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Administrator, original);
    }
  }

  public static async Task Ok_Download_Given_Administrator_With_Feedback_Read_And_No_File_Self()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    Guid attachmentId;
    using (HttpClient filer = Client(filerCookie))
    {
      attachmentId = await UploadAttachmentAsync(filer, "notes.txt", "attached by the filer"u8.ToArray());
      await FileFeedbackAsync(filer, "With an attachment", attachmentId);
      (await filer.GetAsync(DownloadFeedbackAttachment.DownloadPath(attachmentId))).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    (string strangerCookie, PrincipalId strangerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(strangerId, RoleIds.Member);
    using (HttpClient stranger = Client(strangerCookie))
    {
      (await stranger.GetAsync(DownloadFeedbackAttachment.DownloadPath(attachmentId))).StatusCode
        .ShouldBe(HttpStatusCode.NotFound);
    }

    (string cookie, PrincipalId principalId) = await MintIdentitySessionCookie();
    await SetRolesAsync(principalId, RoleIds.Administrator);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    IReadOnlyList<string> original = await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);
    try
    {
      await permissionStore.SetPermissionIdsForRoleAsync(
        RoleIds.Administrator,
        [PermissionIds.AdminAccess, PermissionIds.AdminFeedbackRead]);

      using HttpClient client = Client(cookie);
      HttpResponseMessage download = await client.GetAsync(DownloadFeedbackAttachment.DownloadPath(attachmentId));
      download.StatusCode.ShouldBe(HttpStatusCode.OK);
      (await download.Content.ReadAsStringAsync()).ShouldBe("attached by the filer");
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Administrator, original);
    }
  }

  public static async Task Ok_Owner_Display_Name_Given_Administrator()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    string displayName = await EnsureProfileAsync(filerId);
    Guid itemId;
    using (HttpClient filer = Client(filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Named owner");
    }

    (string adminCookie, PrincipalId adminId) = await MintIdentitySessionCookie();
    await SetRolesAsync(adminId, RoleIds.Member, RoleIds.Administrator);
    using HttpClient admin = Client(adminCookie);

    ListFeedback.Response list = await ReadAsync<ListFeedback.Response>(
      await admin.GetAsync(new ListFeedback.Query { PageSize = ListFeedback.MaxPageSize }.GetRouteWithQueryString()));
    list.Items.Single(item => item.FeedbackItemId == itemId).OwnerDisplayName.ShouldBe(displayName);

    GetFeedbackForAdmin.Response detail = await ReadAsync<GetFeedbackForAdmin.Response>(
      await admin.GetAsync($"api/admin/feedback/{itemId}"));
    detail.Item.OwnerDisplayName.ShouldBe(displayName);
  }

  public static async Task Ok_Feedback_Reader_Without_Profiles_Read_Sees_No_Owner_Name()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    string displayName = await EnsureProfileAsync(filerId);
    Guid itemId;
    using (HttpClient filer = Client(filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Unnamed owner");
    }

    (string cookie, PrincipalId principalId) = await MintIdentitySessionCookie();
    await SetRolesAsync(principalId, RoleIds.Member, RoleIds.Operator);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    IReadOnlyList<string> original = await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Operator);
    try
    {
      await permissionStore.SetPermissionIdsForRoleAsync(
        RoleIds.Operator,
        [.. original, PermissionIds.AdminAccess, PermissionIds.AdminFeedbackRead, PermissionIds.FeedbackRead]);

      using HttpClient client = Client(cookie);
      HttpResponseMessage listResponse = await client.GetAsync(
        new ListFeedback.Query { PageSize = ListFeedback.MaxPageSize }.GetRouteWithQueryString());
      listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
      string listJson = await listResponse.Content.ReadAsStringAsync();
      listJson.ShouldContain(itemId.ToString());
      listJson.ShouldNotContain(displayName);
      ListFeedback.Response list = JsonSerializer
        .Deserialize<ListFeedback.Response>(listJson, ContractSerializationDefaults.Options)
        .ShouldNotBeNull();
      list.Items.ShouldAllBe(item => item.OwnerDisplayName == null);
      list.Items.Single(item => item.FeedbackItemId == itemId).OwnerPrincipalId.ShouldBe(filerId.Value);

      HttpResponseMessage getResponse = await client.GetAsync($"api/admin/feedback/{itemId}");
      getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
      string getJson = await getResponse.Content.ReadAsStringAsync();
      getJson.ShouldNotContain(displayName);
      JsonSerializer.Deserialize<GetFeedbackForAdmin.Response>(getJson, ContractSerializationDefaults.Options)
        .ShouldNotBeNull().Item.OwnerDisplayName.ShouldBeNull();
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Operator, original);
    }
  }
}
