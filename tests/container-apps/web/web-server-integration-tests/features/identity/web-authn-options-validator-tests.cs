#region Purpose
// Host-free validator tests for WebAuthnOptions.AllowedRpIds (task 104-031): entries must be bare
// DNS host names, and the list must be non-empty.
#endregion

namespace WebAuthnOptionsValidator_;

public class Validate_Should
{
  private WebAuthnOptionsValidator Validator = new();

  public void Be_Valid_Given_Dns_Entries()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["localhost", "arch.timewarp.work"] };

    ValidationResult result = Validator.Validate(options);

    result.IsValid.ShouldBeTrue();
  }

  public void Have_Error_Given_Empty_List()
  {
    var options = new WebAuthnOptions { AllowedRpIds = [] };

    Validator.Validate(options).IsValid.ShouldBeFalse();
  }

  public void Have_Error_Given_Scheme_Prefixed_Entry()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["https://arch.timewarp.work"] };

    Validator.Validate(options).IsValid.ShouldBeFalse();
  }

  public void Have_Error_Given_Port_Suffixed_Entry()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["arch.timewarp.work:443"] };

    Validator.Validate(options).IsValid.ShouldBeFalse();
  }

  public void Have_Error_Given_Path_In_Entry()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["arch.timewarp.work/passkeys"] };

    Validator.Validate(options).IsValid.ShouldBeFalse();
  }

  public void Have_Error_Given_Empty_Entry()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["localhost", ""] };

    Validator.Validate(options).IsValid.ShouldBeFalse();
  }

  public void Have_Error_Given_Ip_Literal_Entry()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["127.0.0.1"] };

    Validator.Validate(options).IsValid.ShouldBeFalse();
  }

  public void Setup() => Validator = new WebAuthnOptionsValidator();
}
