// In-memory fixture for the shared IPrincipalStore contract (task 104-032).

namespace PrincipalStoreContract_.InMemory;

file sealed class InMemoryPrincipalStoreFactory : IPrincipalStoreFactory
{
  public IPrincipalStore CreateStore() => new InMemoryPrincipalStore();
}

file static class Fixture
{
  public static readonly IPrincipalStoreFactory Factory = new InMemoryPrincipalStoreFactory();
}

public class Principals : PrincipalStoreContract_.Principals
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class Credentials : PrincipalStoreContract_.Credentials
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class SnapshotSemantics : PrincipalStoreContract_.SnapshotSemantics
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class AddPersistsVersionAsIs : PrincipalStoreContract_.AddPersistsVersionAsIs
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class StalePrincipalUpdate : PrincipalStoreContract_.StalePrincipalUpdate
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class CallerAheadConflict : PrincipalStoreContract_.CallerAheadConflict
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class RevokeResurrectionRace : PrincipalStoreContract_.RevokeResurrectionRace
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class QuarantineLossRace : PrincipalStoreContract_.QuarantineLossRace
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class TierDemotionRace : PrincipalStoreContract_.TierDemotionRace
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class AttachBumpsPrincipalVersion : PrincipalStoreContract_.AttachBumpsPrincipalVersion
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}

public class CallerInstanceNotAdvanced : PrincipalStoreContract_.CallerInstanceNotAdvanced
{
  protected override IPrincipalStoreFactory Factory => Fixture.Factory;
}
