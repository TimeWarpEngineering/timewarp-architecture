#region Purpose
// Case-insensitive test-assembly gate for SPA debug seeders.
#endregion

#region Design
// TimeWarp.State 12.0.0-beta.3 ThrowIfNotTestAssembly uses
// assembly.FullName.Contains("Test") (ordinal, case-sensitive). The SPA integration
// assembly is web-spa-integration-tests, which contains "tests" but not "Test", so that
// helper throws FieldAccessException for a real test caller (timewarp-state#607).
// Seeders pass Assembly.GetCallingAssembly() here. Production assemblies still throw
// FieldAccessException. Do not rename the test assembly to satisfy the State guard.
#endregion

namespace TimeWarp.Architecture;

using System.Reflection;

internal static class TestCaller
{
  internal static void Ensure(Assembly callingAssembly)
  {
    if (callingAssembly.FullName?.Contains("test", StringComparison.OrdinalIgnoreCase) == true)
    {
      return;
    }

    throw new FieldAccessException(
      "Do not use this in production. This method is intended for Test access only!");
  }
}
