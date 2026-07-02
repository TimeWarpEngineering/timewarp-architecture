#region Purpose
// Marks the composition-root module of an ASP.NET host, distinguishing the program itself from the feature modules it composes.
#endregion

namespace TimeWarp.Foundation;
public interface IAspNetProgram : IAspNetModule { }
