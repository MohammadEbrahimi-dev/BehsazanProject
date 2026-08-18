using Behsazan.Domain.Enums;

namespace Behsazan.Application.Validation;

public static class ProjectValidationRules
{
    public const int MaxNameLength = 150;
    public const int MaxAddressLength = 500;

    public static string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "نام پروژه الزامی است";

        if (name.Trim().Length > MaxNameLength)
            return $"نام پروژه نباید بیش از {MaxNameLength} کاراکتر باشد";

        return null;
    }

    public static string? ValidateAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "آدرس پروژه الزامی است";

        if (address.Trim().Length > MaxAddressLength)
            return $"آدرس پروژه نباید بیش از {MaxAddressLength} کاراکتر باشد";

        return null;
    }

    public static string? ValidateCustomerId(int customerId) =>
        customerId <= 0 ? "انتخاب مشتری الزامی است" : null;

    public static string? ValidateJoistType(JoistType joistType) =>
        Enum.IsDefined(joistType) ? null : "نوع تیرچه نامعتبر است";

    public static string? ValidateGeneralLedgerNumber(int? value)
    {
        if (value is null)
            return null;

        if (value <= 0)
            return "شماره دفتر کل باید عددی مثبت باشد";

        return null;
    }

    public static string GetJoistTypeLabel(JoistType joistType) => joistType switch
    {
        JoistType.Concrete25 => "تیرچه بتنی ۲۵",
        JoistType.Metal20 => "تیرچه فلزی ۲۰",
        JoistType.Metal25 => "تیرچه فلزی ۲۵",
        _ => joistType.ToString()
    };
}
