namespace PrincipalId_;

public class Mint
{
  public void Returns_non_empty_id()
  {
    PrincipalId id = PrincipalId.New();
    id.Value.ShouldNotBe(Guid.Empty);
  }

  public void Returns_distinct_ids()
  {
    PrincipalId a = PrincipalId.New();
    PrincipalId b = PrincipalId.New();
    a.ShouldNotBe(b);
  }
}

public class From
{
  public void Accepts_non_empty_guid()
  {
    Guid value = Guid.CreateVersion7();
    PrincipalId id = PrincipalId.From(value);
    id.Value.ShouldBe(value);
  }

  public void Rejects_empty_guid() =>
    Should.Throw<ArgumentException>(() => PrincipalId.From(Guid.Empty));
}
