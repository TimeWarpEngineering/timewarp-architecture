#region Purpose
// Registers /Login/Microsoft365/Choose; markup and behavior live in ChooseMicrosoft365Page.razor.
#endregion

#region Design
// Anonymous focused chrome (same as Login). Parked Entra claims are server-side; this page
// only peeks validity and posts create vs already-have. Already-have reuses StartPasskeyAuthentication
// then CompleteEntraBootstrapExisting so the Entra attach and session happen together.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[Page("/Login/Microsoft365/Choose")]
partial class ChooseMicrosoft365Page;
