namespace TimeWarp.Architecture.Validators;

public class PhoneNumberValidator<T> : PropertyValidator<T, string?>
{
  private readonly PhoneNumberUtil PhoneNumberUtil = PhoneNumberUtil.GetInstance();

  public override string Name => "PhoneNumberValidator";

  public override bool IsValid(ValidationContext<T> context, string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return false;
	
    try
    {
      PhoneNumber? phoneNumber = PhoneNumberUtil.Parse(value, null);
      return PhoneNumberUtil.IsValidNumber(phoneNumber);
    }
    catch (NumberParseException)
    {
      return false;
    }
  }

  protected override string GetDefaultMessageTemplate(string errorCode) =>
    "{PropertyName} is not a valid phone number.";
}
