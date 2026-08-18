using System.Text;
using System.Text.RegularExpressions;
using Behsazan.Domain.Enums;

namespace Behsazan.Application.Validation;

public static class CustomerValidationRules
{
    public const int NationalCodeLength = 10;
    public const int MaxNameLength = 100;
    public const int MaxPhoneLength = 11;

    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);
    private static readonly Regex MobilePattern = new(@"^09\d{9}$", RegexOptions.Compiled);
    private static readonly Regex LandLinePattern = new(@"^0\d{10}$", RegexOptions.Compiled);

    #region Digit normalization
    public static string NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);

        foreach (var ch in value.Trim())
        {
            switch (ch)
            {
                case >= '\u06F0' and <= '\u06F9':
                    builder.Append((char)('0' + (ch - '\u06F0')));
                    break;

                case >= '\u0660' and <= '\u0669':
                    builder.Append((char)('0' + (ch - '\u0660')));
                    break;

                case ' ' or '-' or '(' or ')' or '\u200C':
                    break;

                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }
    #endregion

    #region Names
    public static string? ValidateFirstName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (trimmed.Length < 2)
            return "نام باید حداقل ۲ حرف باشد";

        if (trimmed.Length > MaxNameLength)
            return $"نام نمی‌تواند بیشتر از {MaxNameLength} حرف باشد";

        return null;
    }

    public static string? ValidateLastName(string? value) =>
        ValidateName(value, "نام خانوادگی");

    private static string? ValidateName(string? value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
            return $"{fieldLabel} الزامی است";

        var trimmed = value.Trim();

        if (trimmed.Length < 2)
            return $"{fieldLabel} باید حداقل ۲ حرف باشد";

        if (trimmed.Length > MaxNameLength)
            return $"{fieldLabel} نمی‌تواند بیشتر از {MaxNameLength} حرف باشد";

        return null;
    }
    #endregion

    #region National code
    public static string? ValidateNationalCode(string? value)
    {
        var code = NormalizeDigits(value);

        if (code.Length == 0)
            return null;

        if (!DigitsOnly.IsMatch(code))
            return "کد ملی باید فقط شامل رقم باشد";

        if (code.Length != NationalCodeLength)
            return $"کد ملی باید {NationalCodeLength} رقم باشد";

        if (!IsValidNationalCodeChecksum(code))
            return "کد ملی وارد شده معتبر نیست";

        return null;
    }

    private static bool IsValidNationalCodeChecksum(string code)
    {
        if (code.Distinct().Count() == 1)
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);

        var remainder = sum % 11;
        var checkDigit = code[9] - '0';

        return remainder < 2
            ? checkDigit == remainder
            : checkDigit == 11 - remainder;
    }
    #endregion

    #region Phone numbers
    public static string? ValidatePhoneNumber(string? value, PhoneType phoneType)
    {
        var number = NormalizeDigits(value);

        if (number.Length == 0)
            return "شماره تماس الزامی است";

        if (!DigitsOnly.IsMatch(number))
            return "شماره تماس باید فقط شامل رقم باشد";

        if (number.Length > MaxPhoneLength)
            return $"شماره تماس نمی‌تواند بیشتر از {MaxPhoneLength} رقم باشد";

        return phoneType switch
        {
            PhoneType.Mobile when !MobilePattern.IsMatch(number) =>
                "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود",

            PhoneType.Telephone when !LandLinePattern.IsMatch(number) =>
                "شماره تلفن باید ۱۱ رقم و با کد شهر شروع شود (مثال: ۰۲۱۱۲۳۴۵۶۷۸)",

            PhoneType.Fax when !LandLinePattern.IsMatch(number) =>
                "شماره فکس باید ۱۱ رقم و با کد شهر شروع شود (مثال: ۰۲۱۱۲۳۴۵۶۷۸)",

            _ => null
        };
    }

    public static string GetPhoneTypeLabel(PhoneType phoneType) => phoneType switch
    {
        PhoneType.Mobile => "موبایل",
        PhoneType.Telephone => "تلفن",
        PhoneType.Fax => "فکس",
        _ => phoneType.ToString()
    };
    #endregion
}
