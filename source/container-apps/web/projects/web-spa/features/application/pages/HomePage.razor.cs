#region Purpose
// Registers the root route for the app's landing page; markup and button handlers live in HomePage.razor.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

// Public marketing / first-run entry (147-005 will polish chrome). Anonymous by design.
[Page("/")]
partial class HomePage;
