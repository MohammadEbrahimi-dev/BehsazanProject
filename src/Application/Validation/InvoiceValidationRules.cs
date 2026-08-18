using Behsazan.Application.DTOs;

namespace Behsazan.Application.Validation;

public static class InvoiceValidationRules
{
    public const int MaxItems = 200;
    public const int MaxTitleLength = 100;
    public const decimal MaxLength = 99.9999m;
    public const decimal MaxUnitPrice = 999_999_999m;
    public const int MaxCount = 99_999;
    public const int MaxRebar = 99_999;

    public static string? ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        if (title.Trim().Length > MaxTitleLength)
            return $"عنوان سقف نباید بیش از {MaxTitleLength} کاراکتر باشد";

        return null;
    }

    public static string? ValidateProjectId(int projectId) =>
        projectId <= 0 ? "انتخاب پروژه الزامی است" : null;

    public static string? ValidateInvoiceDate(DateTime invoiceDate)
    {
        if (invoiceDate == default)
            return "تاریخ فاکتور الزامی است";

        if (invoiceDate.Date > DateTime.Today.AddDays(30))
            return "تاریخ فاکتور نمی‌تواند بیش از ۳۰ روز در آینده باشد";

        if (invoiceDate.Year < 2000)
            return "تاریخ فاکتور نامعتبر است";

        return null;
    }

    public static string? ValidateShippingCost(decimal? shippingCost)
    {
        if (shippingCost is null)
            return null;

        if (shippingCost < 0)
            return "هزینه حمل نمی‌تواند منفی باشد";

        if (shippingCost > MaxUnitPrice)
            return "هزینه حمل بیش از حد مجاز است";

        return null;
    }

    public static string? ValidateItems(IReadOnlyList<InvoiceItemFormDto>? items)
    {
        if (items is null || items.Count == 0)
            return "حداقل یک قلم برای فاکتور لازم است";

        if (items.Count > MaxItems)
            return $"تعداد اقلام نباید بیش از {MaxItems} باشد";

        for (var i = 0; i < items.Count; i++)
        {
            var row = i + 1;
            var itemErrors = ValidateItem(items[i]);
            if (itemErrors.Count > 0)
                return $"ردیف {row}: {itemErrors[0]}";
        }

        return null;
    }

    public static List<string> ValidateItem(InvoiceItemFormDto item)
    {
        var errors = new List<string>();

        if (item.Length <= 0)
            errors.Add("طول باید بزرگ‌تر از صفر باشد");
        else if (item.Length > MaxLength)
            errors.Add($"طول نباید بیش از {MaxLength} باشد");

        if (item.Count <= 0)
            errors.Add("تعداد باید بزرگ‌تر از صفر باشد");
        else if (item.Count > MaxCount)
            errors.Add("تعداد بیش از حد مجاز است");

        if (item.UnitPrice < 0)
            errors.Add("قیمت واحد نمی‌تواند منفی باشد");
        else if (item.UnitPrice > MaxUnitPrice)
            errors.Add("قیمت واحد بیش از حد مجاز است");

        if (item.BottomRebar < 0 || item.BottomRebar > MaxRebar)
            errors.Add("میلگرد پایین نامعتبر است");

        if (item.TopRebar < 0 || item.TopRebar > MaxRebar)
            errors.Add("میلگرد بالا نامعتبر است");

        if (item.Zigzag < 0 || item.Zigzag > MaxRebar)
            errors.Add("زیگزاگ نامعتبر است");

        if (item.ReinforcementBar is < 0 or > MaxRebar)
            errors.Add("میلگرد تقویتی نامعتبر است");

        if (item.ReinforcementPercent is < 0 or > 100)
            errors.Add("درصد تقویت باید بین ۰ تا ۱۰۰ باشد");

        return errors;
    }

    public static string FormatTimesTwo(int value)
    {
        var ascii = value.ToString();
        return $"2×{ascii}";
    }

    public static List<string> ValidateForm(InvoiceFormDto form)
    {
        var errors = new List<string>();

        var projectError = ValidateProjectId(form.ProjectId);
        if (projectError is not null)
            errors.Add(projectError);

        var dateError = ValidateInvoiceDate(form.InvoiceDate);
        if (dateError is not null)
            errors.Add(dateError);

        var shippingError = ValidateShippingCost(form.ShippingCost);
        if (shippingError is not null)
            errors.Add(shippingError);

        var titleError = ValidateTitle(form.Title);
        if (titleError is not null)
            errors.Add(titleError);

        var itemsError = ValidateItems(form.Items);
        if (itemsError is not null)
            errors.Add(itemsError);

        return errors;
    }
}
