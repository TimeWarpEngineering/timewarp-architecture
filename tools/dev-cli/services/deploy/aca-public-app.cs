#region Purpose
// The external container app `dev open` and `dev deploy` smoke resolve on aca and aca-neon.
#endregion

namespace DevCli.Services;

/// <summary>The external container app of an aca or aca-neon deployment: its name and public FQDN (task 527).</summary>
internal sealed record AcaPublicApp(string Name, string Fqdn);
