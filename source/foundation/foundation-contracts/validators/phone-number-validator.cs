#region Purpose
// FluentValidation property validator for phone numbers backed by libphonenumber.
#endregion

#region Design
// Delegates to PhoneNumberUtil rather than a regex: real-world numbering plans vary by country
// and change; the library encodes them.
// Parse is called with a null default region, so only numbers in international E.164 form
// (leading +country code) validate — contracts require unambiguous, region-independent input.
// Null/whitespace is invalid here; optionality belongs to the calling rule chain (use When/
// NotEmpty there), not this validator.
#endregion

namespace TimeWarp.Foundation.Validators;

/// <summary>
/// FluentValidation property validator for E.164 phone numbers via libphonenumber.
/// </summary>
public class PhoneNumberValidator<T> : PropertyValidator<T, string?>
{
  private readonly PhoneNumberUtil PhoneNumberUtil = PhoneNumberUtil.GetInstance();

  /// <summary>
  /// Validator name reported in FluentValidation metadata.
  /// </summary>
  public override string Name => "PhoneNumberValidator";

  /// <summary>
  /// True when <paramref name="value"/> parses as a valid international phone number.
  /// </summary>
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

  /// <summary>
  /// Default failure message template for invalid phone numbers.
  /// </summary>
  protected override string GetDefaultMessageTemplate(string errorCode) =>
    "{PropertyName} is not a valid phone number.";
}
