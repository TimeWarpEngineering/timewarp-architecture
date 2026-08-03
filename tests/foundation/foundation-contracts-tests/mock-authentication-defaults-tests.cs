#region Purpose
// Fail-closed activation matrix for MockAuthenticationDefaults (task 145-009).
#endregion

namespace MockAuthenticationDefaults_;

using TimeWarp.Architecture.Configuration;

public class IsMockAuthActive_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsMockAuthActive_Given_>();

  public static Task Production_With_Flag_True_Should_Be_False()
  {
    MockAuthenticationDefaults.IsMockAuthActive("Production", Config(useMock: true)).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Development_With_Flag_False_Should_Be_False()
  {
    MockAuthenticationDefaults.IsMockAuthActive("Development", Config(useMock: false)).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Development_With_Flag_True_Should_Be_True()
  {
    MockAuthenticationDefaults.IsMockAuthActive("Development", Config(useMock: true)).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Testing_With_Flag_True_Should_Be_True()
  {
    MockAuthenticationDefaults.IsMockAuthActive("Testing", Config(useMock: true)).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Staging_With_Flag_True_Should_Be_False()
  {
    MockAuthenticationDefaults.IsMockAuthActive("Staging", Config(useMock: true)).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Null_Environment_Should_Be_False()
  {
    MockAuthenticationDefaults.IsMockAuthActive(null, Config(useMock: true)).ShouldBeFalse();
    return Task.CompletedTask;
  }

  private static IReadOnlyDictionary<string, string?> Config(bool useMock) =>
    new Dictionary<string, string?>
    {
      [MockAuthenticationDefaults.UseMockKey] = useMock ? "true" : "false"
    };
}
