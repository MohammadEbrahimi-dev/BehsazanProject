using Behsazan.Application.DTOs;

namespace Behsazan.Application.Validation;

public static class DepositValidationRules
{
    public const int MaxFromAccountNoLength = 100;
    public const int MaxToAccountNoLength = 100;
    public const int MaxDescriptionLength = 250;
    public const int MaxTrackingNumberLength = 50;
    public const int MaxReferenceNumberLength = 50;
    public const decimal MaxAmount = 999_999_999_999.99m;

    public static string? ValidateProjectId(int projectId) =>
        projectId <= 0 ? "انتخاب پروژه الزامی است" : null;

    public static string? ValidateDepositDate(DateTime depositDate)
    {
        if (depositDate == default)
            return "تاریخ واریزی الزامی است";

        if (depositDate.Date > DateTime.Today.AddDays(30))
            return "تاریخ واریزی نمی‌تواند بیش از ۳۰ روز در آینده باشد";

        if (depositDate.Year < 2000)
            return "تاریخ واریزی نامعتبر است";

        return null;
    }

    public static string? ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            return "مبلغ باید بزرگ‌تر از صفر باشد";

        if (amount > MaxAmount)
            return "مبلغ بیش از حد مجاز است";

        return null;
    }

    public static string? ValidateFromAccountNo(string? fromAccountNo)
    {
        if (string.IsNullOrWhiteSpace(fromAccountNo))
            return "شماره حساب مبدا الزامی است";

        if (fromAccountNo.Trim().Length > MaxFromAccountNoLength)
            return $"شماره حساب مبدا نباید بیش از {MaxFromAccountNoLength} کاراکتر باشد";

        return null;
    }

    public static string? ValidateToAccountNo(string? toAccountNo)
    {
        if (string.IsNullOrWhiteSpace(toAccountNo))
            return "شماره حساب مقصد الزامی است";

        if (toAccountNo.Trim().Length > MaxToAccountNoLength)
            return $"شماره حساب مقصد نباید بیش از {MaxToAccountNoLength} کاراکتر باشد";

        return null;
    }

    public static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        if (description.Trim().Length > MaxDescriptionLength)
            return $"شرح نباید بیش از {MaxDescriptionLength} کاراکتر باشد";

        return null;
    }

    public static string? ValidateTrackingNumber(string? trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return null;

        if (trackingNumber.Trim().Length > MaxTrackingNumberLength)
            return $"شماره پیگیری نباید بیش از {MaxTrackingNumberLength} کاراکتر باشد";

        return null;
    }

    public static string? ValidateReferenceNumber(string? referenceNumber)
    {
        if (string.IsNullOrWhiteSpace(referenceNumber))
            return null;

        if (referenceNumber.Trim().Length > MaxReferenceNumberLength)
            return $"شماره مرجع نباید بیش از {MaxReferenceNumberLength} کاراکتر باشد";

        return null;
    }

    public static List<string> ValidateForm(DepositFormDto form)
    {
        var errors = new List<string>();

        Add(errors, ValidateProjectId(form.ProjectId));
        Add(errors, ValidateDepositDate(form.DepositDate));
        Add(errors, ValidateAmount(form.Amount));
        Add(errors, ValidateFromAccountNo(form.FromAccountNo));
        Add(errors, ValidateToAccountNo(form.ToAccountNo));
        Add(errors, ValidateDescription(form.Description));
        Add(errors, ValidateTrackingNumber(form.TrackingNumber));
        Add(errors, ValidateReferenceNumber(form.ReferenceNumber));

        return errors;
    }

    private static void Add(List<string> errors, string? error)
    {
        if (error is not null)
            errors.Add(error);
    }
}
