#region Purpose
// Forwards SharedProblemDetails to TimeWarp.Contracts so existing references keep resolving.
#endregion

#region Design
// The type's namespace stays TimeWarp.Foundation.Types. TypeForwardedTo preserves that identity.
// A second definition in this assembly would be a different type and would split OneOf arms.
#endregion

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(TimeWarp.Foundation.Types.SharedProblemDetails))]
