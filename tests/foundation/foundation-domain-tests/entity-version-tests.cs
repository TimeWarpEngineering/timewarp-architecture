namespace EntityVersion_;

public class Next
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Next>();

  public static Task Increments_from_zero()
  {
    EntityVersion.Next(0).ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task Increments_from_nonzero()
  {
    EntityVersion.Next(41).ShouldBe(42);
    return Task.CompletedTask;
  }

  public static Task Never_returns_the_original_value()
  {
    long original = 7;
    EntityVersion.Next(original).ShouldNotBe(original);
    return Task.CompletedTask;
  }
}
