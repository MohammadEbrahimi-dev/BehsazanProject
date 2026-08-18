using System.Globalization;
using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Application.Validation;
using Behsazan.Domain.Enums;
using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Behsazan.Infrastructure.Services;

public sealed class InvoicePdfService : IInvoicePdfService
{
    private const string ContentType = "application/pdf";
    private const string FontName = "B Nazanin";

    private const string CompanyName = "بهسازان تیرچه";
    private const string CompanyTagline = "تولید کننده تیرچه استاندارد";
    private const string CompanySpecialty = "صنعتی نقطه جوش";
    private const string Greeting =
        "احتراما، بدینوسیله فاکتور تیرچه سقف های شما حضورتان ارسال میگردد . با تشکر";
    private const string Terms1 =
        "بعد از تحویل گرفتن محموله توسط خریدار یا نماینده ایشان ، بار فوق تا پایان تسویه حساب تیرچه ، فوم ( یونولیت) به طور امانت تحویل گیرنده یا خریدار ( مالک ، صاحبکار یا پیمانکار ) می گردد .";
    private const string Terms2 =
        "ایشان متعهد به پرداخت کل حساب و تسویه کامل حساب می باشد.";

    private const char Lrm = '\u200E';
    private const float rowItemFontSize = 11f;

    private static readonly string[] ColumnHeaders =
    [
        "ردیف",
        "تعداد",
        "طول تیرچه ( متر )",
        "میلگرد پایین",
        "میلگرد بالا",
        "میلگرد تقویت",
        "طول تقویت",
        "زیگزاگ",
        "قیمت هر متر ( ريال )",
        "طول کل ( متر )",
        "قیمت کل ( ریال )"
    ];

    private static readonly object FontLock = new();
    private static bool _fontRegistered;

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public InvoicePdfService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        EnsureFontRegistered();
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<FileDownloadDto?> ExportAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var invoice = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .Select(i => new
            {
                i.InvoiceNumber,
                i.InvoiceDate,
                i.Title,
                i.ShippingCost,
                ProjectAddress = i.Project.Address,
                JoistType = i.Project.JoistType,
                CustomerFullName = i.Project.Customer.FirstName + " " + i.Project.Customer.LastName,
                Items = i.InvoiceItems
                    .OrderBy(x => x.Id)
                    .Select(x => new InvoiceItemPreviewDto
                    {
                        Id = x.Id,
                        Length = x.Length,
                        Count = x.Count,
                        BottomRebar = x.BottomRebar,
                        TopRebar = x.TopRebar,
                        ReinforcementBar = x.ReinforcementBar,
                        ReinforcementPercent = x.ReinforcementPercent,
                        Zigzag = x.Zigzag,
                        UnitPrice = x.UnitPrice,
                        TotalAmount = x.TotalAmount,
                        TotalPrice = x.TotalPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
            return null;

        var invoiceTitle = string.IsNullOrWhiteSpace(invoice.Title)
            ? "—"
            : invoice.Title.Trim();

        var model = new InvoicePdfModel(
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.CustomerFullName.Trim(),
            invoiceTitle,
            invoice.ProjectAddress,
            GetJoistTypeShortLabel(invoice.JoistType),
            invoice.ShippingCost,
            invoice.Items);

        var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginLeft(10);
                    page.MarginRight(10);
                    page.MarginTop(8);
                    page.MarginBottom(8);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily(FontName).FontSize(10).DirectionFromRightToLeft());
                    page.ContentFromRightToLeft();

                    page.Header().Element(c => ComposeHeader(c, model));
                    page.Content().Element(c => ComposeContent(c, model));
                    page.Footer().Element(ComposeFooter);
                });
            })
            .GeneratePdf();

        return new FileDownloadDto
        {
            FileName = $"فاکتور-{invoice.InvoiceNumber}.pdf",
            ContentType = ContentType,
            Content = bytes
        };
    }

    #region Layout
    private static void ComposeHeader(IContainer container, InvoicePdfModel model)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("به نام خدا").FontSize(13).Bold();

            col.Item().PaddingTop(1).Row(row =>
            {
                row.RelativeItem().AlignRight().Text(CompanyName).FontSize(18).Bold();
                row.AutoItem().AlignMiddle().Text(text =>
                {
                    text.Span("تاریخ : ").FontSize(10).Bold();
                    text.Span(" ").FontSize(10);
                    text.Span(FormatPersianDate(model.InvoiceDate)).FontSize(10).Bold();
                });
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().AlignRight().Text(CompanyTagline).FontSize(12);
                row.AutoItem().AlignMiddle().Text(text =>
                {
                    text.Span("شماره : ").FontSize(10).Bold();
                    text.Span(" ").FontSize(10);
                    text.Span(model.InvoiceNumber.ToString(CultureInfo.InvariantCulture)).FontSize(10).Bold();
                });
            });

            col.Item().PaddingTop(3).PaddingBottom(3)
                .LineHorizontal(1.2f)
                .LineColor(Colors.Black);

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem(3).AlignRight().AlignMiddle()
                    .Text(CompanySpecialty).FontSize(12);

                row.RelativeItem(5).AlignCenter().AlignMiddle()
                    .Text(string.IsNullOrWhiteSpace(model.CustomerName) ? "—" : model.CustomerName)
                    .FontSize(12).Bold();

                row.RelativeItem(3).AlignCenter().AlignMiddle()
                    .Text(model.InvoiceTitle).FontSize(11).Bold();
            });

            col.Item().PaddingTop(3).ShowOnce().Row(row =>
            {
                row.RelativeItem(9)
                    .AlignRight()
                    .AlignMiddle()
                    .Text(Greeting)
                    .FontSize(11);

                row.RelativeItem(2)
                    .PaddingVertical(2)
                    .PaddingHorizontal(2)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(model.JoistLabel)
                    .FontSize(rowItemFontSize)
                    .Bold();
            });

            col.Item().PaddingTop(4);
        });
    }

    private static void ComposeContent(IContainer container, InvoicePdfModel model)
    {
        container.Column(col =>
        {
            col.Item().Element(c => ComposeItemsTable(c, model));

            col.Item().PaddingTop(6).AlignRight().Text(FormatAddress(model.Address))
                .FontSize(13);

            col.Item().PaddingTop(6).AlignRight().Text(Terms1)
                .FontSize(11);

            col.Item().PaddingTop(3).AlignRight().Text(Terms2)
                .FontSize(11);

            col.Item().PaddingTop(14).Row(row =>
            {
                row.RelativeItem().AlignRight().Text("امضاء فروشنده:").FontSize(12).Bold();
                row.RelativeItem().AlignCenter().Text("امضاء خریدار:").FontSize(12).Bold();
            });
        });
    }

    private static void ComposeItemsTable(IContainer container, InvoicePdfModel model)
    {
        var items = model.Items;
        var sumCount = items.Sum(i => i.Count);
        var sumTotalAmount = items.Sum(i => i.Count * i.Length);
        var sumTotalPrice = items.Sum(i => i.TotalPrice);
        var shipping = model.ShippingCost is > 0 ? model.ShippingCost.Value : 0m;
        var grandTotal = sumTotalPrice + shipping;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);   // ردیف
                columns.ConstantColumn(26);   // تعداد
                columns.RelativeColumn(1.05f); // طول تیرچه — wider
                columns.RelativeColumn(0.95f); // میلگرد پایین
                columns.ConstantColumn(28);   // میلگرد بالا
                columns.ConstantColumn(32);   // میلگرد تقویت
                columns.ConstantColumn(30);   // طول تقویت
                columns.RelativeColumn(0.8f);  // زیگزاگ
                columns.RelativeColumn(1.15f); // قیمت هر متر
                columns.RelativeColumn(0.95f); // طول کل
                columns.RelativeColumn(1.2f);  // قیمت کل
            });

            table.Header(header =>
            {
                foreach (var title in ColumnHeaders)
                    header.Cell().Element(HeaderCell).Text(title).FontSize(9.5f).Bold().AlignCenter();
            });

            var index = 1;
            foreach (var item in items)
            {
                var totalAmount = item.Count * item.Length;
                WriteCentered(table, index.ToString(CultureInfo.InvariantCulture), fontSize: 10.5f);
                WriteCentered(table, item.Count.ToString(CultureInfo.InvariantCulture), fontSize: rowItemFontSize);
                WriteCentered(table, FormatLength(item.Length), fontSize: rowItemFontSize);
                WriteCenteredLtr(table, FormatTimesTwoRtl(item.BottomRebar));
                WriteCentered(table, item.TopRebar.ToString(CultureInfo.InvariantCulture), fontSize: rowItemFontSize);
                WriteCentered(table, item.ReinforcementBar?.ToString(CultureInfo.InvariantCulture) ?? "ندارد", fontSize: rowItemFontSize);
                WriteCentered(table, item.ReinforcementPercent != null ? FormatPercent(item.ReinforcementPercent) : "ندارد", fontSize: rowItemFontSize);
                WriteCenteredLtr(table, FormatTimesTwoRtl(item.Zigzag));
                WriteCentered(table, item.UnitPrice.ToString("N0", CultureInfo.InvariantCulture), fontSize: rowItemFontSize);
                WriteCentered(table, FormatLength(totalAmount), fontSize: rowItemFontSize);
                WriteCentered(table, item.TotalPrice.ToString("N0", CultureInfo.InvariantCulture), fontSize: rowItemFontSize);
                index++;
            }

            if (items.Count == 0)
            {
                for (var c = 0; c < 11; c++)
                    WriteCentered(table, string.Empty);
            }

            for (var c = 0; c < 11; c++)
            {
                var text = c switch
                {
                    1 => sumCount.ToString(CultureInfo.InvariantCulture),
                    9 => FormatLength(sumTotalAmount),
                    10 => sumTotalPrice.ToString("N0", CultureInfo.InvariantCulture),
                    _ => string.Empty
                };
                table.Cell().Element(BodyCell).Text(text).FontSize(9).Bold().AlignCenter();
            }

            if (shipping > 0)
            {
                table.Cell().ColumnSpan(8)
                   .Text("")
                   .FontSize(9);

                table.Cell().ColumnSpan(2).Element(BodyCell)
                    .AlignCenter()
                    .PaddingRight(4)
                    .Text("هزینه حمل")
                    .FontSize(9);

                table.Cell().Element(BodyCell)
                    .Text(shipping.ToString("N0", CultureInfo.InvariantCulture))
                    .FontSize(9)
                    .Bold()
                    .AlignCenter();
            }

            table.Cell().ColumnSpan(8)
                  .Text("")
                  .FontSize(9);

            table.Cell().ColumnSpan(2).Element(BodyCell)
                .AlignCenter()
                .Text("جمع کل")
                .FontSize(10)
                .Bold();

            table.Cell().Element(BodyCell)
                .Text(grandTotal.ToString("N0", CultureInfo.InvariantCulture))
                .FontSize(10)
                .Bold()
                .AlignCenter();
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container
            .PaddingTop(2)
            .AlignCenter()
            .DefaultTextStyle(x => x.FontFamily(FontName).FontSize(8))
            .Text(text =>
            {
                text.Span("صفحه ");
                text.CurrentPageNumber();
                text.Span(" از ");
                text.TotalPages();
            });
    }

    private static void WriteCentered(TableDescriptor table, string value, float fontSize = 9f) =>
        table.Cell().Element(BodyCell).Text(value).FontSize(fontSize).AlignCenter();

    private static void WriteCenteredLtr(TableDescriptor table, string value) =>
        table.Cell().Element(BodyCell)
            .AlignCenter()
            .Text(value)
            .FontSize(rowItemFontSize)
            .DirectionFromLeftToRight();

    private static IContainer HeaderCell(IContainer c) =>
        c.Border(0.5f)
            .Background(Colors.Grey.Lighten1)
            .PaddingVertical(3)
            .PaddingHorizontal(1)
            .AlignMiddle();

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.4f)
            .PaddingVertical(1)
            .PaddingHorizontal(1)
            .MinHeight(14)
            .AlignMiddle();
    #endregion

    #region Helpers
    private static void EnsureFontRegistered()
    {
        if (_fontRegistered)
            return;

        lock (FontLock)
        {
            if (_fontRegistered)
                return;

            var assembly = typeof(InvoicePdfService).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("B-Nazanin.ttf", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
                throw new InvalidOperationException(
                    "Embedded font B-Nazanin.ttf was not found in Behsazan.Infrastructure.");

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not open embedded font '{resourceName}'.");

            var memory = new MemoryStream();
            stream.CopyTo(memory);
            memory.Position = 0;
            FontManager.RegisterFontWithCustomName(FontName, memory);

            _fontRegistered = true;
        }
    }

    private static string FormatTimesTwoRtl(int value) =>
        $"{Lrm}2{Lrm}×{Lrm}{value.ToString(CultureInfo.InvariantCulture)}{Lrm}";

    private static string GetJoistTypeShortLabel(JoistType joistType) => joistType switch
    {
        JoistType.Concrete25 => "25 بتنی",
        JoistType.Metal20 => "20 فلزی",
        JoistType.Metal25 => "25 فلزی",
        _ => ProjectValidationRules.GetJoistTypeLabel(joistType)
    };

    private static string FormatAddress(string? address) =>
        string.IsNullOrWhiteSpace(address)
            ? "آدرس پروژه : —"
            : $"آدرس پروژه : {address.Trim()}";

    private static string FormatPercent(int? percent) =>
        percent is null ? string.Empty : $"{percent.Value}%";

    private static string FormatLength(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string FormatPersianDate(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(local):0000}/{calendar.GetMonth(local):00}/{calendar.GetDayOfMonth(local):00}";
    }

    private sealed record InvoicePdfModel(
        int InvoiceNumber,
        DateTime InvoiceDate,
        string CustomerName,
        string InvoiceTitle,
        string Address,
        string JoistLabel,
        decimal? ShippingCost,
        IReadOnlyList<InvoiceItemPreviewDto> Items);
    #endregion
}
