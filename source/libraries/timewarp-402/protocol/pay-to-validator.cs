#region Purpose
// Validates EVM receive addresses so misconfigured payTo never emits a 402 challenge.
#endregion

namespace TimeWarp.X402;

using System.Text.RegularExpressions;
/// <summary>EVM <c>payTo</c> validation shared by options checks and challenge builders.</summary>
public static partial class PayToValidator
{
  private const string ZeroAddress = "0x0000000000000000000000000000000000000000";

  /// <summary>
  /// Returns true when <paramref name="payTo"/> is a 40-hex EVM address and not the zero address.
  /// </summary>
  public static bool IsValid(string? payTo)
  {
    if (string.IsNullOrWhiteSpace(payTo))
    {
      return false;
    }

    if (!EvmAddressRegex().IsMatch(payTo))
    {
      return false;
    }

    return !string.Equals(payTo, ZeroAddress, StringComparison.OrdinalIgnoreCase);
  }

  [GeneratedRegex("^0x[a-fA-F0-9]{40}$", RegexOptions.CultureInvariant)]
  private static partial Regex EvmAddressRegex();
}
