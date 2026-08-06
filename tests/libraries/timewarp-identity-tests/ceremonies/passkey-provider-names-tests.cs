// ReSharper disable InconsistentNaming
namespace PasskeyProviderNames_;

public class Resolve
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Resolve>();

  public static Task Proton_Pass_aaguid_resolves()
  {
    // ASCII "Prot" "onPa" "ss" "Proton..." — community list key
    // 50726f74-6f6e-5061-7373-50726f746f6e
    byte[] aaguid =
    [
      0x50, 0x72, 0x6f, 0x74, 0x6f, 0x6e, 0x50, 0x61,
      0x73, 0x73, 0x50, 0x72, 0x6f, 0x74, 0x6f, 0x6e
    ];

    PasskeyProviderNames.TryResolve(aaguid).ShouldBe("Proton Pass");
    return Task.CompletedTask;
  }

  public static Task Zero_aaguid_resolves_null()
  {
    PasskeyProviderNames.TryResolve(new byte[16]).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task OnePassword_resolves()
  {
    // bada5566-a7aa-401f-bd96-45619a55120d
    byte[] aaguid =
    [
      0xba, 0xda, 0x55, 0x66, 0xa7, 0xaa, 0x40, 0x1f,
      0xbd, 0x96, 0x45, 0x61, 0x9a, 0x55, 0x12, 0x0d
    ];

    PasskeyProviderNames.TryResolve(aaguid).ShouldBe("1Password");
    return Task.CompletedTask;
  }
}
