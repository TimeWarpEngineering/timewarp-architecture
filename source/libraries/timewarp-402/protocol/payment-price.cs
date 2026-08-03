#region Purpose
// Parse seller price strings (e.g. "$0.10") into major-unit decimals for ledger debit/credit.
#endregion

#region Design
// PaymentOptions.Price is a wire string for the exact scheme challenge ("$0.10"); the credit ledger
// stores decimal major units of the account currency. Parsing is invariant-culture, optional leading
// '$', fail-closed (non-positive / unparseable → false). Hosts must not invent a second price source —
// the same options.Price drives both the x402 challenge and the ledger debit amount.
#endregion

namespace TimeWarp.X402;

using System.Globalization;

/// <summary>Helpers for converting x402 price strings to ledger major units.</summary>
public static class PaymentPrice
{
  /// <summary>
  /// Parses <paramref name="price"/> into a positive major-unit amount (e.g. <c>"$0.10"</c> → 0.10m).
  /// </summary>
  public static bool TryParseMajorUnits(string? price, out decimal amount)
  {
    amount = 0m;
    if (string.IsNullOrWhiteSpace(price))
    {
      return false;
    }

    ReadOnlySpan<char> span = price.AsSpan().Trim();
    if (span.Length > 0 && span[0] == '$')
    {
      span = span[1..].TrimStart();
    }

    if (!decimal.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
    {
      return false;
    }

    if (parsed <= 0m)
    {
      return false;
    }

    amount = parsed;
    return true;
  }
}
