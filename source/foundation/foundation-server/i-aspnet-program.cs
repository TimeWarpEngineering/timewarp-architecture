#region Purpose
// Marks the composition-root module of an ASP.NET host, distinguishing the program itself from the feature modules it composes.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Composition-root ASP.NET module for a host Program, as opposed to feature modules it composes.
/// </summary>
public interface IAspNetProgram : IAspNetModule;
