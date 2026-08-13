using System.Text;
using Fynexpay.Application.DTOs;
using Fynexpay.Domain.Entities;

namespace Fynexpay.Application.Services;

public static class MerchantBankAccount
{
    public static bool IsComplete(Merchant merchant) =>
        !string.IsNullOrWhiteSpace(merchant.BankName)
        && !string.IsNullOrWhiteSpace(merchant.BankAccountHolder)
        && !string.IsNullOrWhiteSpace(merchant.BankAccountNumber);

    public static MerchantPayoutAccountDto Map(Merchant merchant) => new(
        merchant.BankName,
        merchant.BankAccountHolder,
        merchant.BankAccountNumber,
        merchant.BankIban,
        IsComplete(merchant));

    public static string FormatDetails(Merchant merchant)
    {
        var sb = new StringBuilder();
        sb.Append("صاحب الحساب: ").Append(merchant.BankAccountHolder?.Trim());
        sb.Append(" | البنك: ").Append(merchant.BankName?.Trim());
        sb.Append(" | رقم الحساب: ").Append(NormalizeAccount(merchant.BankAccountNumber));
        if (!string.IsNullOrWhiteSpace(merchant.BankIban))
            sb.Append(" | IBAN: ").Append(NormalizeIban(merchant.BankIban));
        return sb.ToString();
    }

    public static string FormatWhatsAppBlock(Merchant merchant)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"البنك: {merchant.BankName?.Trim()}");
        sb.AppendLine($"صاحب الحساب: {merchant.BankAccountHolder?.Trim()}");
        sb.Append($"رقم الحساب: {NormalizeAccount(merchant.BankAccountNumber)}");
        if (!string.IsNullOrWhiteSpace(merchant.BankIban))
            sb.AppendLine().Append($"IBAN: {NormalizeIban(merchant.BankIban)}");
        return sb.ToString();
    }

    public static string NormalizeAccount(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return new string(raw.Where(char.IsLetterOrDigit).ToArray());
    }

    public static string NormalizeIban(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return new string(raw.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();
    }
}
