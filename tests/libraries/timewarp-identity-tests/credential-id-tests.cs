namespace CredentialId_;

public class Mint
{
  public void Returns_non_empty_id()
  {
    CredentialId id = CredentialId.New();
    id.Value.ShouldNotBe(Guid.Empty);
    id.IsEmpty.ShouldBeFalse();
  }

  public void Returns_distinct_ids()
  {
    CredentialId a = CredentialId.New();
    CredentialId b = CredentialId.New();
    a.ShouldNotBe(b);
  }
}

public class From
{
  public void Accepts_non_empty_guid()
  {
    Guid value = Guid.CreateVersion7();
    CredentialId id = CredentialId.From(value);
    id.Value.ShouldBe(value);
  }

  public void Rejects_empty_guid() =>
    Should.Throw<ArgumentException>(() => CredentialId.From(Guid.Empty));
}
