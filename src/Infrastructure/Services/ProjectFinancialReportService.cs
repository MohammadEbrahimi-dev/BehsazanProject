using System.Globalization;
using System.Text;
using Behsazan.Application.DTOs;
using Behsazan.Application.Enums;
using Behsazan.Application.Interfaces;
using ClosedXML.Excel;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Behsazan.Infrastructure.Services;

public sealed class ProjectFinancialReportService : IProjectFinancialReportService
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    private const string FontName = "B Nazanin";
    private const string CompanyName = "بهسازان تیرچه";
    private const string CompanyTagline = "تولید کننده تیرچه استاندارد";
    private const string CompanySpecialty = "صنعتی نقطه جوش";

    private static readonly object FontLock = new();
    private static bool _fontRegistered;

    private const float rowItemFontSize = 11f;

    private readonly IProjectLedgerService _ledgerService;

    public ProjectFinancialReportService(IProjectLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
        EnsureFontRegistered();
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<FileDownloadDto?> ExportExcelAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var ledger = await _ledgerService.GetByProjectIdAsync(projectId, cancellationToken);
        if (ledger is null)
            return null;

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("دفتر کل");
        ws.RightToLeft = true;
        ws.Style.Font.FontName = FontName;
        ws.Style.Font.FontSize = 12;

        SetColumnWidths(ws);
        var headerEndRow = BuildExcelHeader(ws, ledger);
        var summaryEndRow = WriteExcelSummary(ws, headerEndRow + 1, ledger);
        WriteExcelEntriesTable(ws, summaryEndRow + 2, ledger);
        WriteExcelSignatures(ws, summaryEndRow + 2 + 1 + Math.Max(ledger.Entries.Count, 1) + 2);
        ApplyExcelPrintSettings(ws);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new FileDownloadDto
        {
            FileName = BuildFileName(ledger.ProjectName, "xlsx"),
            ContentType = ExcelContentType,
            Content = stream.ToArray()
        };
    }

    public async Task<FileDownloadDto?> ExportPdfAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var ledger = await _ledgerService.GetByProjectIdAsync(projectId, cancellationToken);
        if (ledger is null)
            return null;

        var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginLeft(18);
                    page.MarginRight(18);
                    page.MarginTop(14);
                    page.MarginBottom(14);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily(FontName).FontSize(10).DirectionFromRightToLeft());
                    page.ContentFromRightToLeft();

                    page.Header().Element(c => ComposePdfHeader(c, ledger));
                    page.Content().Element(c => ComposePdfContent(c, ledger));
                    page.Footer().Element(ComposePdfFooter);
                });
            })
            .GeneratePdf();

        return new FileDownloadDto
        {
            FileName = BuildFileName(ledger.ProjectName, "pdf"),
            ContentType = PdfContentType,
            Content = bytes
        };
    }

    #region Excel
    private static void SetColumnWidths(IXLWorksheet ws)
    {
        ws.Column(1).Width = 14;  // تاریخ
        ws.Column(2).Width = 10;  // نوع
        ws.Column(3).Width = 36;  // شرح
        ws.Column(4).Width = 16;  // بدهکار
        ws.Column(5).Width = 16;  // بستانکار
        ws.Column(6).Width = 16;  // مانده
    }

    private static int BuildExcelHeader(IXLWorksheet ws, ProjectLedgerDto ledger)
    {
        ws.Row(1).Height = 22;
        ws.Row(2).Height = 24;
        ws.Row(3).Height = 20;
        ws.Row(4).Height = 20;
        ws.Row(5).Height = 22;

        ws.Range(1, 1, 1, 6).Merge().Value = "به نام خدا";
        StyleTitle(ws.Range(1, 1, 1, 6), 14, bold: true);

        ws.Range(2, 1, 2, 3).Merge().Value = CompanyName;
        StyleTitle(ws.Range(2, 1, 2, 3), 16, bold: true);

        ws.Range(2, 4, 2, 6).Merge().Value = "گزارش مالی پروژه (دفتر کل)";
        StyleTitle(ws.Range(2, 4, 2, 6), 13, bold: true);

        ws.Range(3, 1, 3, 3).Merge().Value = CompanyTagline;
        StyleTitle(ws.Range(3, 1, 3, 3), 11, bold: false);

        ws.Cell(3, 4).Value = "تاریخ گزارش:";
        StyleLabel(ws.Cell(3, 4));
        ws.Range(3, 5, 3, 6).Merge().Value = FormatPersianDate(DateTime.Now);
        StyleValue(ws.Range(3, 5, 3, 6), bold: true);

        ws.Range(4, 1, 4, 3).Merge().Value = CompanySpecialty;
        StyleTitle(ws.Range(4, 1, 4, 3), 11, bold: false);

        ws.Cell(4, 4).Value = "شماره دفتر کل:";
        StyleLabel(ws.Cell(4, 4));
        ws.Range(4, 5, 4, 6).Merge().Value = ledger.GeneralLedgerNumber?.ToString(CultureInfo.InvariantCulture) ?? "—";
        StyleValue(ws.Range(4, 5, 4, 6), bold: true);

        ws.Cell(5, 1).Value = "مشتری:";
        StyleLabel(ws.Cell(5, 1));
        ws.Range(5, 2, 5, 3).Merge().Value =
            string.IsNullOrWhiteSpace(ledger.CustomerFullName) ? "—" : ledger.CustomerFullName.Trim();
        StyleValue(ws.Range(5, 2, 5, 3), bold: true);
        ws.Range(5, 2, 5, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(5, 4).Value = "پروژه:";
        StyleLabel(ws.Cell(5, 4));
        ws.Range(5, 5, 5, 6).Merge().Value =
            string.IsNullOrWhiteSpace(ledger.ProjectName) ? "—" : ledger.ProjectName.Trim();
        StyleValue(ws.Range(5, 5, 5, 6), bold: true);
        ws.Range(5, 5, 5, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var addressRow = 6;
        ws.Row(addressRow).Height = 26;
        var addressText = string.IsNullOrWhiteSpace(ledger.ProjectAddress)
            ? "آدرس پروژه : —"
            : $"آدرس پروژه : {ledger.ProjectAddress.Trim()}";
        ws.Range(addressRow, 1, addressRow, 6).Merge().Value = addressText;
        ws.Range(addressRow, 1, addressRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Range(addressRow, 1, addressRow, 6).Style.Alignment.WrapText = true;
        ws.Range(addressRow, 1, addressRow, 6).Style.Font.FontSize = 11;

        return addressRow;
    }

    private static int WriteExcelSummary(IXLWorksheet ws, int startRow, ProjectLedgerDto ledger)
    {
        ws.Row(startRow).Height = 20;
        ws.Range(startRow, 1, startRow, 6).Merge().Value = "خلاصه مالی";
        StyleTitle(ws.Range(startRow, 1, startRow, 6), 12, bold: true);
        ws.Range(startRow, 1, startRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");
        BorderBox(ws.Range(startRow, 1, startRow, 6));

        string[] labels =
        [
            "تعداد فاکتور",
            "جمع فاکتورها (بدهکار)",
            "تعداد واریزی",
            "جمع واریزی‌ها (بستانکار)",
            "مانده حساب"
        ];
        string[] values =
        [
            ledger.InvoiceCount.ToString("N0", CultureInfo.InvariantCulture),
            ledger.InvoiceTotal.ToString("N0", CultureInfo.InvariantCulture),
            ledger.DepositCount.ToString("N0", CultureInfo.InvariantCulture),
            ledger.DepositTotal.ToString("N0", CultureInfo.InvariantCulture),
            ledger.OutstandingBalance.ToString("N0", CultureInfo.InvariantCulture)
        ];

        for (var i = 0; i < labels.Length; i++)
        {
            var row = startRow + 1 + i;
            ws.Row(row).Height = 18;
            ws.Range(row, 1, row, 3).Merge().Value = labels[i];
            ws.Range(row, 1, row, 3).Style.Font.Bold = true;
            ws.Range(row, 1, row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            BorderBox(ws.Range(row, 1, row, 3));

            ws.Range(row, 4, row, 6).Merge().Value = values[i];
            ws.Range(row, 4, row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 4, row, 6).Style.Font.Bold = i == labels.Length - 1;
            if (i == labels.Length - 1)
                ws.Range(row, 4, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
            BorderBox(ws.Range(row, 4, row, 6));
        }

        return startRow + labels.Length;
    }

    private static void WriteExcelEntriesTable(IXLWorksheet ws, int headerRow, ProjectLedgerDto ledger)
    {
        ws.Row(headerRow).Height = 28;
        string[] headers = ["تاریخ", "نوع", "شرح", "بدهکار", "بستانکار", "مانده"];
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");
            BorderBox(cell);
        }

        if (ledger.Entries.Count == 0)
        {
            var emptyRow = headerRow + 1;
            ws.Range(emptyRow, 1, emptyRow, 6).Merge().Value = "هنوز فاکتور یا واریزی‌ای ثبت نشده است.";
            ws.Range(emptyRow, 1, emptyRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            BorderBox(ws.Range(emptyRow, 1, emptyRow, 6));
            return;
        }

        for (var i = 0; i < ledger.Entries.Count; i++)
        {
            var entry = ledger.Entries[i];
            var row = headerRow + 1 + i;
            ws.Row(row).Height = 18;

            ws.Cell(row, 1).Value = FormatPersianDate(entry.Date);
            ws.Cell(row, 2).Value = entry.EntryType == ProjectLedgerEntryType.Invoice ? "فاکتور" : "واریزی";
            ws.Cell(row, 3).Value = entry.Description;

            if (entry.Debit > 0)
                ws.Cell(row, 4).Value = (double)entry.Debit;
            else
                ws.Cell(row, 4).Value = "—";

            if (entry.Credit > 0)
                ws.Cell(row, 5).Value = (double)entry.Credit;
            else
                ws.Cell(row, 5).Value = "—";

            ws.Cell(row, 6).Value = (double)entry.RunningBalance;

            for (var c = 1; c <= 6; c++)
            {
                var cell = ws.Cell(row, c);
                cell.Style.Alignment.Horizontal = c is 3
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                BorderBox(cell);
            }

            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 6).Style.Font.Bold = true;
        }
    }

    private static void WriteExcelSignatures(IXLWorksheet ws, int startRow)
    {
        if (startRow < 1)
            startRow = 1;

        ws.Row(startRow).Height = 22;
        ws.Range(startRow, 1, startRow, 2).Merge().Value = "امضاء مسئول مالی:";
        ws.Range(startRow, 1, startRow, 2).Style.Font.Bold = true;
        ws.Range(startRow, 4, startRow, 6).Merge().Value = "امضاء مدیر:";
        ws.Range(startRow, 4, startRow, 6).Style.Font.Bold = true;
    }

    private static void ApplyExcelPrintSettings(IXLWorksheet ws)
    {
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Left = 0.4;
        ws.PageSetup.Margins.Right = 0.4;
        ws.PageSetup.Margins.Top = 0.5;
        ws.PageSetup.Margins.Bottom = 0.5;
    }

    private static void StyleTitle(IXLRange range, double size, bool bold)
    {
        range.Style.Font.FontSize = size;
        range.Style.Font.Bold = bold;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void StyleLabel(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void StyleValue(IXLRange range, bool bold)
    {
        range.Style.Font.Bold = bold;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void BorderBox(IXLRange range) =>
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

    private static void BorderBox(IXLCell cell) =>
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    #endregion

    #region PDF
    private static void ComposePdfHeader(IContainer container, ProjectLedgerDto ledger)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("به نام خدا").FontSize(13).Bold();

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().AlignRight().Text(CompanyName).FontSize(16).Bold();
                row.AutoItem().AlignMiddle().Text(text =>
                {
                    text.Span("تاریخ گزارش : ").FontSize(11).Bold();
                    text.Span(FormatPersianDate(DateTime.Now)).FontSize(11).Bold();
                });
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().AlignRight().Text(CompanyTagline).FontSize(12);
                row.AutoItem().AlignMiddle().Text("گزارش مالی پروژه (دفتر کل)").FontSize(11).Bold();
            });

            col.Item().PaddingTop(4).PaddingBottom(4)
                .LineHorizontal(1.2f)
                .LineColor(Colors.Black);

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("مشتری : ").FontSize(rowItemFontSize).Bold();
                    text.Span(string.IsNullOrWhiteSpace(ledger.CustomerFullName)
                        ? "—"
                        : ledger.CustomerFullName.Trim()).FontSize(rowItemFontSize);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("پروژه : ").FontSize(rowItemFontSize).Bold();
                    text.Span(string.IsNullOrWhiteSpace(ledger.ProjectName)
                        ? "—"
                        : ledger.ProjectName.Trim()).FontSize(rowItemFontSize);
                });
            });

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("شماره دفتر کل : ").FontSize(rowItemFontSize).Bold();
                    text.Span(ledger.GeneralLedgerNumber?.ToString(CultureInfo.InvariantCulture) ?? "—").FontSize(rowItemFontSize);
                });

                row.RelativeItem().AlignRight().Text(FormatAddress(ledger.ProjectAddress)).FontSize(rowItemFontSize);
            });

            col.Item().PaddingTop(6);
        });
    }

    private static void ComposePdfContent(IContainer container, ProjectLedgerDto ledger)
    {
        container.Column(col =>
        {
            col.Item().Element(c => ComposePdfSummary(c, ledger));
            col.Item().PaddingTop(10).Element(c => ComposePdfEntriesTable(c, ledger));
        });
    }

    private static void ComposePdfSummary(IContainer container, ProjectLedgerDto ledger)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
            });

            table.Cell().ColumnSpan(4).Element(SummaryHeaderCell)
                .Text("خلاصه مالی").FontSize(11).Bold().AlignCenter();

            WriteSummaryPair(table, "تعداد فاکتور", ledger.InvoiceCount.ToString("N0", CultureInfo.InvariantCulture));
            WriteSummaryPair(table, "جمع فاکتورها", ledger.InvoiceTotal.ToString("N0", CultureInfo.InvariantCulture));
            WriteSummaryPair(table, "تعداد واریزی", ledger.DepositCount.ToString("N0", CultureInfo.InvariantCulture));
            WriteSummaryPair(table, "جمع واریزی‌ها", ledger.DepositTotal.ToString("N0", CultureInfo.InvariantCulture));

            table.Cell().ColumnSpan(2).Element(SummaryBodyCell)
                .Text("مانده حساب").FontSize(10).Bold().AlignRight();
            table.Cell().ColumnSpan(2).Element(SummaryHighlightCell)
                .Text(ledger.OutstandingBalance.ToString("N0", CultureInfo.InvariantCulture))
                .FontSize(11).Bold().AlignCenter();
        });
    }

    private static void WriteSummaryPair(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(SummaryBodyCell).Text(label).FontSize(rowItemFontSize).Bold().AlignRight();
        table.Cell().Element(SummaryBodyCell).Text(value).FontSize(rowItemFontSize).AlignCenter();
    }

    private static void ComposePdfEntriesTable(IContainer container, ProjectLedgerDto ledger)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(72);   // تاریخ
                columns.ConstantColumn(48);   // نوع
                columns.RelativeColumn(3);    // شرح
                columns.RelativeColumn(1.2f); // بدهکار
                columns.RelativeColumn(1.2f); // بستانکار
                columns.RelativeColumn(1.2f); // مانده
            });

            table.Header(header =>
            {
                foreach (var title in new[] { "تاریخ", "نوع", "شرح", "بدهکار", "بستانکار", "مانده" })
                    header.Cell().Element(HeaderCell).Text(title).FontSize(9).Bold().AlignCenter();
            });

            if (ledger.Entries.Count == 0)
            {
                table.Cell().ColumnSpan(6).Element(BodyCell)
                    .Text("هنوز فاکتور یا واریزی‌ای ثبت نشده است.")
                    .FontSize(9)
                    .AlignCenter();
                return;
            }

            foreach (var entry in ledger.Entries)
            {
                WriteCentered(table, FormatPersianDate(entry.Date), fontSize: rowItemFontSize);
                WriteCentered(table, entry.EntryType == ProjectLedgerEntryType.Invoice ? "فاکتور" : "واریزی", fontSize: rowItemFontSize);
                table.Cell().Element(BodyCell)
                    .AlignRight()
                    .PaddingRight(4)
                    .Text(entry.Description)
                    .FontSize(rowItemFontSize);
                WriteCentered(table, entry.Debit > 0
                    ? entry.Debit.ToString("N0", CultureInfo.InvariantCulture)
                    : "—", fontSize: rowItemFontSize);
                WriteCentered(table, entry.Credit > 0
                    ? entry.Credit.ToString("N0", CultureInfo.InvariantCulture)
                    : "—", fontSize: rowItemFontSize);
                table.Cell().Element(BodyCell)
                    .Text(entry.RunningBalance.ToString("N0", CultureInfo.InvariantCulture))
                    .FontSize(rowItemFontSize)
                    .Bold()
                    .AlignCenter();
            }
        });
    }

    private static void ComposePdfFooter(IContainer container)
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

    private static IContainer HeaderCell(IContainer c) =>
        c.Border(0.5f)
            .Background(Colors.Grey.Lighten1)
            .PaddingVertical(4)
            .PaddingHorizontal(2)
            .AlignMiddle();

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.4f)
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .MinHeight(16)
            .AlignMiddle();

    private static IContainer SummaryHeaderCell(IContainer c) =>
        c.Border(0.5f)
            .Background(Colors.Grey.Lighten1)
            .PaddingVertical(4)
            .AlignMiddle();

    private static IContainer SummaryBodyCell(IContainer c) =>
        c.Border(0.4f)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .AlignMiddle();

    private static IContainer SummaryHighlightCell(IContainer c) =>
        c.Border(0.4f)
            .Background(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
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

            var assembly = typeof(ProjectFinancialReportService).Assembly;
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

    private static string FormatAddress(string? address) =>
        string.IsNullOrWhiteSpace(address)
            ? "آدرس پروژه : —"
            : $"آدرس پروژه : {address.Trim()}";

    private static string FormatPersianDate(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(local):0000}/{calendar.GetMonth(local):00}/{calendar.GetDayOfMonth(local):00}";
    }

    private static string BuildFileName(string projectName, string extension)
    {
        var safeName = SanitizeFileName(projectName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "Project";

        return $"Ledger-{safeName}-{DateTime.Now:yyyyMMdd}.{extension}";
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name.Trim())
        {
            if (invalid.Contains(ch) || ch is '/' or '\\' or ':')
                sb.Append('-');
            else
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Trim().Trim('.');
        return cleaned.Length > 80 ? cleaned[..80].Trim() : cleaned;
    }
    #endregion
}
