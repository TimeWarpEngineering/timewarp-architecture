#region Purpose
// Grants Web.Contracts internal visibility to the SPA, the server, and their integration tests so contract internals stay inside the web boundary.
#endregion

[assembly: InternalsVisibleTo("Web.Spa")]
[assembly: InternalsVisibleTo("Web.Spa.Integration.Tests")]
[assembly: InternalsVisibleTo("Web.Server")]
[assembly: InternalsVisibleTo("Web.Server.Integration.Tests")]
