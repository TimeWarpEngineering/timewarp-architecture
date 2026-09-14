namespace EntraIssuerMaterial_;

public class FromTenantId
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FromTenantId>();

  public static Task Emits_utf8_v2_issuer_with_lowercase_tid()
  {
    Guid tenantId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");

    byte[] material = EntraIssuerMaterial.FromTenantId(tenantId);

    Encoding.UTF8.GetString(material)
      .ShouldBe("https://login.microsoftonline.com/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/v2.0");
    return Task.CompletedTask;
  }
}
