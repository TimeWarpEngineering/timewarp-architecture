#region Purpose
// Code-behind for the signed-out confirmation page: [Page] drives source-generated routing and navigation plumbing.
#endregion

#region Design
// Task 147-005: uses TimeWarpFocusedPage (auth-adjacent focused chrome) rather than TimeWarpPage
// so logout confirmation matches login — not a page inside the product shell.
#endregion

namespace TimeWarp.Architecture.Features.Account;

// Public confirmation landing after SignOut (principal may already be anonymous).
[Page("/Logout")]
partial class LogoutPage;
