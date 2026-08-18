namespace Behsazan.Application.DTOs;

public class InvoiceExcelParseResultDto
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime? InvoiceDate { get; init; }

    public string? Title { get; init; }

    public decimal? ShippingCost { get; init; }

    public List<InvoiceItemFormDto> Items { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static InvoiceExcelParseResultDto Fail(string message) => new()
    {
        Succeeded = false,
        Message = message
    };

    public static InvoiceExcelParseResultDto Ok(
        List<InvoiceItemFormDto> items,
        DateTime? invoiceDate,
        decimal? shippingCost,
        string? title = null,
        IReadOnlyList<string>? warnings = null) => new()
    {
        Succeeded = true,
        Message = items.Count == 1
            ? "۱ قلم از فایل خوانده شد"
            : $"{items.Count} قلم از فایل خوانده شد",
        Items = items,
        InvoiceDate = invoiceDate,
        ShippingCost = shippingCost,
        Title = title,
        Warnings = warnings ?? Array.Empty<string>()
    };
}
