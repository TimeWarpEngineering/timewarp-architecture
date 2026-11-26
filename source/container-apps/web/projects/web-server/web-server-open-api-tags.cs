#region Purpose
// Web-server OpenAPI document facts: the description of each operation tag, plus contact and license from repository metadata.
#endregion

#region Design
// Task 458-011: the document declares every operation tag at the root (OpenApiRootTags), and each
// needs a description that says who the group is for: anonymous callers, a signed-in user, an
// agent bearer, or an admin. The tags come from the generator (the namespace leaf under
// Features, so …Features.Admin.Roles is "Roles"), hand-written endpoints, and the shared
// discovery operations ("Health"). Because web-server passes this map, the document fails with
// the tag name when an operation uses a tag that is not listed here, and the root-tags test fails
// on an entry that no operation uses. Contact and license come from the assembly metadata
// msbuild/open-api-document-metadata.props writes from Directory.Build.props.
#endregion

namespace TimeWarp.Architecture.Web.Server;

/// <summary>
/// Web-server OpenAPI contact, license, and tag descriptions.
/// </summary>
internal static class WebServerOpenApiTags
{
  internal static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>(StringComparer.Ordinal)
  {
    ["AgentChats"] =
      "Assistant chat for a signed-in user, and the xAI API key: a user's own key under /api/users/current, "
      + "and the site key under /api/admin (admin only).",
    ["AgentLinks"] =
      "Links an agent to a human account. The agent requests a link and reads its human-ux page with its "
      + "bearer; the signed-in human lists, approves, or denies links in the browser session.",
    ["Analytics"] = "Client analytics events the SPA posts. Anonymous.",
    ["BrowserLogs"] = "Browser log records the SPA forwards to the server. Anonymous.",
#if postgres
    ["Feedback"] =
      "Feedback with attachments. A signed-in user submits, lists, and replies to their own items under "
      + "/api/feedback; /api/admin/feedback is the admin triage surface (status, severity, notes, replies).",
#endif
    ["Health"] = "Web server health check. Anonymous, text/plain.",
    ["Hellos"] = "Sample greeting endpoint. Anonymous.",
    ["Identity"] =
      "Sign-in and credentials: browser session routes (Entra challenge, sign-out), the signed-in user's "
      + "credentials, and agent key registration and bearer token issuance under /api/identity/agent.",
    ["MeteredCapability"] =
      "Agent surface. Demo capability an agent calls with its bearer and pays for per request with x402; "
      + "the 402 response carries the payment terms.",
    ["Principals"] = "Admin only. Lists principals (humans and agents) and sets their roles and agent scopes.",
    ["Profiles"] =
      "The current user's profile. GET is anonymous and returns the demo profile to an anonymous caller; "
      + "PUT needs the signed-in user.",
    ["Roles"] =
      "Admin only. Lists, reads, creates, updates, and deletes roles and sets their permissions and agent scopes.",
    ["Settings"] =
      "Site settings for admins (settings read and write permissions): sign-in tenant, xAI key mode, and the "
      + "assistant name. GET /api/settings/assistant is anonymous; every page shows that name.",
    ["Tip"] = "Tip the site operator with x402. Anonymous; GET and POST /api are discovery aliases of /api/tip.",
  };

  /// <summary>Contact and license from this assembly's repository metadata, with <see cref="Descriptions"/>.</summary>
  internal static OpenApiDocumentInfo DocumentInfo() =>
    OpenApiDocumentInfo.FromAssembly(typeof(WebServerOpenApiTags).Assembly, Descriptions);
}
