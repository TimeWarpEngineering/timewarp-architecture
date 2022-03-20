namespace TimeWarp.Architecture.Testing
{
  /// <summary>
  /// List of tags that can be used to group tests.
  /// </summary>
  /// <remarks>Add any tags you would like here. So tests can be grouped by the tag</remarks>
  /// <example>dotnet fixie -- --Tag Joe --Tag Bob</example>
  ///

  public static class TestTags
  {
    // Tags based on how fast the test runs
    public const string Fast = nameof(Fast);
    public const string Slow = nameof(Slow);

    // Tags based on type of test
    public const string Unit = nameof(Unit);
    public const string Smoke = nameof(Smoke);
    public const string Performance = nameof(Performance);

    // Tags based on author of test
    public const string Cramer = nameof(Cramer);
  }
}
