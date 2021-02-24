namespace __RootNamespace__.Features.__FeatureName__s
{
  using FluentValidation;
  
  public class __FeatureName__CreateRequestValidator : AbstractValidator<__FeatureName__CreateResponse>
  {

    public __FeatureName__CreateRequestValidator()
    {

      // Declare message as const here. Or could use an injected Language Service

      // public const string InsufficentFundsMessage = "You can not send more than your balance";
      // public const string MustBeGreaterThanZeroMessage = "'{PropertyName}' must be greater than zero";

      RuleFor(a__FeatureName__CreateRequest => a__FeatureName__CreateRequest.Price)
        .NotEmpty().GreaterThan(0);
      RuleFor(a__FeatureName__CreateRequest => a__FeatureName__CreateRequest.Name)
        .NotEmpty().MinimumLength(2);

      // https://docs.fluentvalidation.net/
      // Example Rules

      // RuleFor(aSendAction => aSendAction.Fee)
      //   .NotEmpty();

      // RuleFor(aConversionRequest => aConversionRequest.FromCurrency.ToLower())
      //   .Must(aToken => validTokens.Contains(aToken));

      // RuleFor(aGetNativeAmountRequest => aGetNativeAmountRequest.ToCurrency.ToLower())
      //   .Equal(UsdCurrencyCode.ToLower());

      // RuleFor(aSendAction => aSendAction)
      //   .Must(aSendAction => BalanceGreaterThanSendAmount(aSendAction))
      //   .WithMessage(InsufficentFundsMessage)
      //   .When(aSendAction => CurrencyMustExistInWallet(aSendAction))
      //   .When(aSendAction => WalletMustExist(aSendAction.EdgeCurrencyWalletId))
      //   .When(aSendAction => !string.IsNullOrWhiteSpace(aSendAction.NativeAmount));
    }
  }
}