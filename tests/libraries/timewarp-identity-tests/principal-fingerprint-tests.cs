#region Purpose
// Task 253: PrincipalFingerprint shape/stability and Principal.Create with a pre-allocated id.
#endregion

// ReSharper disable InconsistentNaming
namespace PrincipalFingerprint_;

public class Compute
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Compute>();

  public static Task Is_eight_lowercase_hex_chars()
  {
    string fingerprint = PrincipalFingerprint.Compute(PrincipalId.New());

    fingerprint.Length.ShouldBe(PrincipalFingerprint.Length);
    fingerprint.ShouldAllBe(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c));
    return Task.CompletedTask;
  }

  public static Task Is_stable_for_one_principal()
  {
    PrincipalId id = PrincipalId.New();

    PrincipalFingerprint.Compute(id).ShouldBe(PrincipalFingerprint.Compute(PrincipalId.From(id.Value)));
    return Task.CompletedTask;
  }

  public static Task Is_last_eight_hex_of_sha256_of_guid_bytes()
  {
    PrincipalId id = PrincipalId.From(Guid.Parse("0197f3a1-2b4c-7d5e-8f60-718293a4b5c6"));
    string expected = Convert.ToHexStringLower(SHA256.HashData(id.Value.ToByteArray()))[^8..];

    PrincipalFingerprint.Compute(id).ShouldBe(expected);
    return Task.CompletedTask;
  }

  public static Task Differs_between_principals()
  {
    PrincipalFingerprint.Compute(PrincipalId.New()).ShouldNotBe(PrincipalFingerprint.Compute(PrincipalId.New()));
    return Task.CompletedTask;
  }

  public static Task Never_contains_the_raw_id()
  {
    PrincipalId id = PrincipalId.New();
    string fingerprint = PrincipalFingerprint.Compute(id);

    id.Value.ToString("N").ShouldNotContain(fingerprint);
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_id()
  {
    Should.Throw<ArgumentException>(() => PrincipalFingerprint.Compute(default));
    return Task.CompletedTask;
  }
}

public class Create_With_Id
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Create_With_Id>();

  public static Task Uses_the_pre_allocated_id_and_keeps_invariants()
  {
    PrincipalId id = PrincipalId.New();

    var principal = Principal.Create(PrincipalKind.Human, id);

    principal.Id.ShouldBe(id);
    principal.TrustTier.ShouldBe(TrustTier.Provisional);
    principal.IsQuarantined.ShouldBeFalse();
    principal.Version.ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_id()
  {
    Should.Throw<ArgumentException>(() => Principal.Create(PrincipalKind.Human, default));
    return Task.CompletedTask;
  }

  public static Task Rejects_none_kind()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => Principal.Create(PrincipalKind.None, PrincipalId.New()));
    return Task.CompletedTask;
  }
}
