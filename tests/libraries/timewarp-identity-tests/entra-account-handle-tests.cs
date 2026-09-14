namespace EntraAccountHandle_;

public class Encode
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Encode>();

  public static Task Emits_utf8_lowercase_tid_colon_oid()
  {
    Guid tenantId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
    Guid objectId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");

    byte[] handle = EntraAccountHandle.Encode(tenantId, objectId);

    Encoding.UTF8.GetString(handle)
      .ShouldBe("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    return Task.CompletedTask;
  }
}

public class TryDecode
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryDecode>();

  public static Task Round_trips_canonical_handle()
  {
    Guid tenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    Guid objectId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    byte[] handle = EntraAccountHandle.Encode(tenantId, objectId);

    EntraAccountHandle.TryDecode(handle, out Guid decodedTenantId, out Guid decodedObjectId).ShouldBeTrue();
    decodedTenantId.ShouldBe(tenantId);
    decodedObjectId.ShouldBe(objectId);
    return Task.CompletedTask;
  }

  public static Task Rejects_uppercase_guid_text()
  {
    byte[] handle = Encoding.UTF8.GetBytes(
      "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    EntraAccountHandle.TryDecode(handle, out Guid tenantId, out Guid objectId).ShouldBeFalse();
    tenantId.ShouldBe(Guid.Empty);
    objectId.ShouldBe(Guid.Empty);
    return Task.CompletedTask;
  }

  public static Task Rejects_n_format_without_hyphens()
  {
    byte[] handle = Encoding.UTF8.GetBytes(
      "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

    EntraAccountHandle.TryDecode(handle, out _, out _).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Rejects_missing_colon()
  {
    byte[] handle = Encoding.UTF8.GetBytes(
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaabbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    EntraAccountHandle.TryDecode(handle, out _, out _).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Rejects_empty()
  {
    EntraAccountHandle.TryDecode([], out _, out _).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Rejects_non_ascii()
  {
    byte[] handle = EntraAccountHandle.Encode(
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    handle[0] = 0x80;

    EntraAccountHandle.TryDecode(handle, out _, out _).ShouldBeFalse();
    return Task.CompletedTask;
  }
}
