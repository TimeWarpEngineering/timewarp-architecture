#region Purpose
// Code-behind for the change-password placeholder page: [Page] drives source-generated routing and navigation plumbing.
#endregion

namespace TimeWarp.Architecture.Features.Account;

// Legacy placeholder (no password product). Still auth-gated so anonymous cannot open it.
[Page("/changePassword", Policy = Policies.Authenticated)]
[Authorize(Policy = Policies.Authenticated)]
partial class ChangePasswordPage;
