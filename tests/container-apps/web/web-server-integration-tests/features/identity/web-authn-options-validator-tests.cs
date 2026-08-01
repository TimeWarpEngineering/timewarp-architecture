#region Purpose
// Host-free validator tests for WebAuthnOptions.AllowedRpIds (task 104-031): entries must be bare
// DNS host names, and the list must be non-empty.
#endregion

namespace WebAuthnOptionsValidator_;

public class Validate_Should
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Validate_Should>();

  public static Task Be_Valid_Given_Dns_Entries()

  {
    var options = new WebAuthnOptions { AllowedRpIds = ["localhost", "arch.timewarp.work"] };

    ValidationResult result = new WebAuthnOptionsValidator().Validate(options);

    result.IsValid.ShouldBeTrue();

    return Task.CompletedTask;

  }

  public static Task Have_Error_Given_Empty_List()

  {
    var options = new WebAuthnOptions { AllowedRpIds = [] };

    new WebAuthnOptionsValidator().Validate(options).IsValid.ShouldBeFalse();

    return Task.CompletedTask;

  }

  public static Task Have_Error_Given_Scheme_Prefixed_Entry()

  {
    var options = new WebAuthnOptions { AllowedRpIds = ["https://arch.timewarp.work"] };

    new WebAuthnOptionsValidator().Validate(options).IsValid.ShouldBeFalse();

    return Task.CompletedTask;

  }

  public static Task Have_Error_Given_Port_Suffixed_Entry()

  {
    var options = new WebAuthnOptions { AllowedRpIds = ["arch.timewarp.work:443"] };

    new WebAuthnOptionsValidator().Validate(options).IsValid.ShouldBeFalse();

    return Task.CompletedTask;

  }

  public static Task Have_Error_Given_Path_In_Entry()

  {
    var options = new WebAuthnOptions { AllowedRpIds = ["arch.timewarp.work/passkeys"] };

    new WebAuthnOptionsValidator().Validate(options).IsValid.ShouldBeFalse();

    return Task.CompletedTask;

  }

  public static Task Have_Error_Given_Empty_Entry()

  {
    var options = new WebAuthnOptions { AllowedRpIds = ["localhost", ""] };

    new WebAuthnOptionsValidator().Validate(options).IsValid.ShouldBeFalse();

    return Task.CompletedTask;

  }

  public static Task Have_Error_Given_Ip_Literal_Entry()

  {
    var options = new WebAuthnOptions { AllowedRpIds = ["127.0.0.1"] };

    new WebAuthnOptionsValidator().Validate(options).IsValid.ShouldBeFalse();

    return Task.CompletedTask;

  }

}
