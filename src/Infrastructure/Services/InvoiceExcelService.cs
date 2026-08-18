using System.Globalization;
using System.Text.RegularExpressions;
using Behsazan.Application.DTOs;
using Behsazan.Application.Interfaces;
using Behsazan.Application.Validation;
using Behsazan.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Behsazan.Infrastructure.Services;

public sealed class InvoiceExcelService : IInvoiceExcelService
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string FontName = "B Nazanin";

    private const string CompanyName = "بهسازان تیرچه";
    private const string CompanyTagline = "تولید کننده تیرچه استاندارد";
    private const string CompanySpecialty = "صنعتی نقطه جوش";
    private const string Greeting =
        "    احتراما، بدینوسیله فاکتور تیرچه سقف های شما حضورتان ارسال میگردد . با تشکر";
    private const string Terms1 =
        "    بعد از تحویل گرفتن محموله توسط خریدار یا نماینده ایشان ، بار فوق تا پایان تسویه حساب تیرچه ، فوم ( یونولیت) به طور امانت تحویل             گیرنده یا خریدار ( مالک ، صاحبکار یا پیمانکار ) می گردد .";
    private const string Terms2 =
        "    ایشان متعهد به پرداخت کل حساب و تسویه کامل حساب می باشد.";

    private static readonly string[] HeaderTokens =
    [
        "ردیف", "تعداد", "طول", "پایین", "بالا", "تقویت", "زیگزاگ", "قیمت", "طول کل", "قیمت کل"
    ];

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public InvoiceExcelService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
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
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.Title,
                i.TotalAmount,
                i.TotalPrice,
                i.ShippingCost,
                ProjectName = i.Project.Name,
                ProjectAddress = i.Project.Address,
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

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("فاکتور");
        ws.RightToLeft = true;
        ws.Style.Font.FontName = FontName;
        ws.Style.Font.FontSize = 12;

        SetColumnWidths(ws);
        BuildHeader(ws, invoice.CustomerFullName.Trim(), invoice.ProjectName,
            invoice.InvoiceNumber, invoice.InvoiceDate, invoice.Title);

        const int headerRow = 7;
        WriteColumnHeaders(ws, headerRow);

        var firstItemRow = headerRow + 1;
        var itemCount = invoice.Items.Count;
        if (itemCount == 0)
            itemCount = 1;

        for (var i = 0; i < invoice.Items.Count; i++)
            WriteItemRow(ws, firstItemRow + i, i + 1, invoice.Items[i]);

        var lastItemRow = firstItemRow + Math.Max(invoice.Items.Count, 1) - 1;
        var subtotalRow = lastItemRow + 1;
        WriteItemsSubtotalRow(ws, subtotalRow, firstItemRow, lastItemRow, invoice.Items.Count > 0);

        var cursor = subtotalRow + 1;
        var totalStartRow = subtotalRow;

        if (invoice.ShippingCost is > 0)
        {
            WriteShippingRow(ws, cursor, invoice.ShippingCost.Value);
            cursor++;
        }

        WriteGrandTotalRow(ws, cursor, totalStartRow, cursor - 1);
        cursor++;

        WriteAddressRow(ws, cursor, invoice.ProjectAddress);
        cursor += 2;

        WriteTermsAndSignatures(ws, cursor);

        ApplyPrintSettings(ws);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new FileDownloadDto
        {
            FileName = $"فاکتور-{invoice.InvoiceNumber}.xlsx",
            ContentType = ContentType,
            Content = stream.ToArray()
        };
    }

    public Task<InvoiceExcelParseResultDto> ParseImportAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();
            return Task.FromResult(ParseWorksheet(ws));
        }
        catch (Exception ex)
        {
            return Task.FromResult(InvoiceExcelParseResultDto.Fail(
                $"خواندن فایل اکسل ممکن نشد: {ex.Message}"));
        }
    }

    #region Export helpers
    private static void SetColumnWidths(IXLWorksheet ws)
    {
        ws.Column(1).Width = 7.5;
        ws.Column(2).Width = 6;
        ws.Column(3).Width = 8.5;
        ws.Column(4).Width = 7.5;
        ws.Column(5).Width = 6;
        ws.Column(6).Width = 6.5;
        ws.Column(7).Width = 7;
        ws.Column(8).Width = 9.5;
        ws.Column(9).Width = 15;
        ws.Column(10).Width = 11.5;
        ws.Column(11).Width = 15;
    }

    private static void BuildHeader(
        IXLWorksheet ws,
        string customerName,
        string projectName,
        int invoiceNumber,
        DateTime invoiceDate,
        string? title)
    {
        ws.Row(1).Height = 22.5;
        ws.Row(2).Height = 22;
        ws.Row(3).Height = 29;
        ws.Row(4).Height = 25;
        ws.Row(5).Height = 24;
        ws.Row(6).Height = 27;

        ws.Range(1, 5, 1, 9).Merge().Value = "به نام خدا";
        StyleTitle(ws.Range(1, 5, 1, 9), 16, bold: true);

        ws.Range(2, 1, 2, 4).Merge().Value = CompanyName;
        StyleTitle(ws.Range(2, 1, 2, 4), 14, bold: true);

        ws.Cell(2, 10).Value = "تاریخ:";
        StyleLabel(ws.Cell(2, 10));
        ws.Cell(2, 11).Value = FormatPersianDate(invoiceDate);
        StyleValue(ws.Cell(2, 11), bold: true);

        ws.Range(3, 1, 3, 4).Merge().Value = CompanyTagline;
        StyleTitle(ws.Range(3, 1, 3, 4), 12, bold: false);

        ws.Cell(3, 10).Value = "شماره :";
        StyleLabel(ws.Cell(3, 10));
        ws.Cell(3, 11).Value = invoiceNumber;
        StyleValue(ws.Cell(3, 11), bold: true);

        var displayName = string.IsNullOrWhiteSpace(customerName) ? "—" : customerName;
        ws.Range(4, 5, 5, 8).Merge().Value = displayName;
        StyleTitle(ws.Range(4, 5, 5, 8), 13, bold: true);

        ws.Range(4, 10, 5, 11).Merge().Value = projectName;
        StyleTitle(ws.Range(4, 10, 5, 11), 12, bold: true);

        ws.Range(5, 1, 5, 4).Merge().Value = CompanySpecialty;
        StyleTitle(ws.Range(5, 1, 5, 4), 11, bold: false);

        ws.Range(6, 1, 6, 9).Merge().Value = Greeting;
        ws.Range(6, 1, 6, 9).Style.Alignment.WrapText = true;
        ws.Range(6, 1, 6, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Range(6, 1, 6, 9).Style.Font.FontSize = 11;

        ws.Range(6, 10, 6, 11).Merge().Value = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        StyleTitle(ws.Range(6, 10, 6, 11), 12, bold: true);
        BorderBox(ws.Range(6, 10, 6, 11));
    }

    private static void WriteColumnHeaders(IXLWorksheet ws, int row)
    {
        ws.Row(row).Height = 33;
        string[] headers =
        [
            "ردیف",
            "تعداد",
            "طول تیرچه ( متر )",
            "میلگرد پایین",
            "میلگرد بالا",
            "میلگرد تقویت",
            "طول تقویت",
            "زیگزاگ",
            "قیمت هر متر   ( ريال )",
            "طول کل ( متر )",
            "قیمت کل (  ریال )"
        ];

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");
            BorderBox(cell);
        }
    }

    private static void WriteItemRow(IXLWorksheet ws, int row, int index, InvoiceItemPreviewDto item)
    {
        ws.Row(row).Height = 18;

        ws.Cell(row, 1).Value = index;
        ws.Cell(row, 2).Value = item.Count;
        ws.Cell(row, 3).Value = (double)item.Length;
        ws.Cell(row, 4).Value = InvoiceValidationRules.FormatTimesTwo(item.BottomRebar);
        ws.Cell(row, 5).Value = item.TopRebar;
        ws.Cell(row, 6).Value = item.ReinforcementBar?.ToString() ?? string.Empty;
        ws.Cell(row, 7).Value = item.ReinforcementPercent is null
            ? string.Empty
            : (double)(item.ReinforcementPercent.Value / 100m);
        ws.Cell(row, 8).Value = InvoiceValidationRules.FormatTimesTwo(item.Zigzag);
        ws.Cell(row, 9).Value = (double)item.UnitPrice;
        ws.Cell(row, 10).FormulaA1 = $"B{row}*C{row}";
        ws.Cell(row, 11).FormulaA1 = $"I{row}*J{row}";

        for (var c = 1; c <= 11; c++)
        {
            var cell = ws.Cell(row, c);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            BorderBox(cell);
        }

        ws.Cell(row, 3).Style.NumberFormat.Format = "0.####";
        ws.Cell(row, 7).Style.NumberFormat.Format = "0%";
        ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 10).Style.NumberFormat.Format = "0.###";
        ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0";
    }

    private static void WriteItemsSubtotalRow(
        IXLWorksheet ws,
        int row,
        int firstItemRow,
        int lastItemRow,
        bool hasItems)
    {
        ws.Row(row).Height = 18;

        if (hasItems)
        {
            ws.Cell(row, 2).FormulaA1 = $"SUM(B{firstItemRow}:B{lastItemRow})";
            ws.Cell(row, 10).FormulaA1 = $"SUM(J{firstItemRow}:J{lastItemRow})";
            ws.Cell(row, 11).FormulaA1 = $"SUM(K{firstItemRow}:K{lastItemRow})";
        }
        else
        {
            ws.Cell(row, 2).Value = 0;
            ws.Cell(row, 10).Value = 0;
            ws.Cell(row, 11).Value = 0;
        }

        for (var c = 1; c <= 11; c++)
        {
            var cell = ws.Cell(row, c);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            BorderBox(cell);
        }

        ws.Cell(row, 10).Style.NumberFormat.Format = "0.###";
        ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0";
    }

    private static void WriteShippingRow(IXLWorksheet ws, int row, decimal shippingCost)
    {
        ws.Row(row).Height = 18;
        ws.Range(row, 8, row, 10).Merge().Value = "کرایه حمل فوم به کارگاه ساختمانی";
        ws.Range(row, 8, row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(row, 11).Value = (double)shippingCost;
        ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 11).Style.Font.Bold = true;

        for (var c = 1; c <= 11; c++)
            BorderBox(ws.Cell(row, c));
    }

    private static void WriteGrandTotalRow(IXLWorksheet ws, int row, int fromRow, int toRow)
    {
        ws.Row(row).Height = 19;
        ws.Range(row, 8, row, 10).Merge().Value = "جمع کل";
        ws.Range(row, 8, row, 10).Style.Font.Bold = true;
        ws.Range(row, 8, row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (toRow >= fromRow)
            ws.Cell(row, 11).FormulaA1 = $"SUM(K{fromRow}:K{toRow})";
        else
            ws.Cell(row, 11).Value = 0;

        ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 11).Style.Font.Bold = true;
        ws.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");

        for (var c = 1; c <= 11; c++)
            BorderBox(ws.Cell(row, c));
    }

    private static void WriteAddressRow(IXLWorksheet ws, int row, string address)
    {
        ws.Row(row).Height = 28;
        var text = string.IsNullOrWhiteSpace(address)
            ? "آدرس پروژه : —"
            : $"آدرس پروژه : {address.Trim()}";
        ws.Range(row, 1, row, 11).Merge().Value = text;
        ws.Range(row, 1, row, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Range(row, 1, row, 11).Style.Font.FontSize = 11;
    }

    private static void WriteTermsAndSignatures(IXLWorksheet ws, int startRow)
    {
        var termsRow = startRow;
        ws.Row(termsRow).Height = 36;
        ws.Range(termsRow, 1, termsRow + 1, 11).Merge().Value = Terms1;
        ws.Range(termsRow, 1, termsRow + 1, 11).Style.Alignment.WrapText = true;
        ws.Range(termsRow, 1, termsRow + 1, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Range(termsRow, 1, termsRow + 1, 11).Style.Font.FontSize = 10;

        var terms2Row = termsRow + 3;
        ws.Range(terms2Row, 1, terms2Row, 9).Merge().Value = Terms2;
        ws.Range(terms2Row, 1, terms2Row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Range(terms2Row, 1, terms2Row, 9).Style.Font.FontSize = 10;

        var signRow = terms2Row + 1;
        ws.Range(signRow, 1, signRow, 3).Merge().Value = "امضاء فروشنده:";
        ws.Range(signRow, 7, signRow, 9).Merge().Value = "امضاء خریدار:";
        ws.Range(signRow, 1, signRow, 3).Style.Font.Bold = true;
        ws.Range(signRow, 7, signRow, 9).Style.Font.Bold = true;
    }

    private static void ApplyPrintSettings(IXLWorksheet ws)
    {
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 1);
        ws.PageSetup.Margins.Left = 0.25;
        ws.PageSetup.Margins.Right = 0.25;
        ws.PageSetup.Margins.Top = 0.4;
        ws.PageSetup.Margins.Bottom = 0.4;
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

    private static void StyleValue(IXLCell cell, bool bold)
    {
        cell.Style.Font.Bold = bold;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void BorderBox(IXLRange range) =>
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

    private static void BorderBox(IXLCell cell) =>
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

    private static string FormatPersianDate(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(local):0000}/{calendar.GetMonth(local):00}/{calendar.GetDayOfMonth(local):00}";
    }
    #endregion

    #region Import helpers
    private static InvoiceExcelParseResultDto ParseWorksheet(IXLWorksheet ws)
    {
        var used = ws.RangeUsed();
        if (used is null)
            return InvoiceExcelParseResultDto.Fail("فایل اکسل خالی است");

        var lastRow = used.LastRow().RowNumber();
        var lastCol = Math.Min(used.LastColumn().ColumnNumber(), 20);

        var headerRow = FindHeaderRow(ws, lastRow, lastCol);
        if (headerRow is null)
            return InvoiceExcelParseResultDto.Fail(
                "سطر عنوان جدول پیدا نشد. فایل باید شبیه قالب فاکتور بهسازان (Book704) باشد.");

        var columns = MapColumns(ws, headerRow.Value, lastCol);
        if (!columns.ContainsKey(Col.Count) || !columns.ContainsKey(Col.Length) || !columns.ContainsKey(Col.UnitPrice))
            return InvoiceExcelParseResultDto.Fail(
                "ستون‌های تعداد / طول / قیمت واحد در فایل شناسایی نشدند.");

        var warnings = new List<string>();
        var items = new List<InvoiceItemFormDto>();
        decimal? shipping = null;

        for (var r = headerRow.Value + 1; r <= lastRow; r++)
        {
            var rowText = ConcatRowText(ws, r, lastCol);

            if (string.IsNullOrWhiteSpace(rowText))
                continue;

            if (IsKeywordRow(rowText, "جمع کل") || IsKeywordRow(rowText, "امضاء") ||
                IsKeywordRow(rowText, "آدرس پروژه") || IsKeywordRow(rowText, "متعهد"))
                break;

            if (IsKeywordRow(rowText, "کرایه") ||
                (IsKeywordRow(rowText, "حمل") && !IsKeywordRow(rowText, "طول")))
            {
                shipping = TryReadMoney(ws, r, columns.GetValueOrDefault(Col.TotalPrice, 11), lastCol)
                           ?? shipping;
                continue;
            }

            if (IsKeywordRow(rowText, "فوم") || IsKeywordRow(rowText, "یونولیت"))
            {
                warnings.Add($"ردیف {r}: قلم فوم/یونولیت نادیده گرفته شد (در سیستم ثبت نمی‌شود).");
                continue;
            }

            if (LooksLikeSubtotalRow(ws, r, columns))
                continue;

            var item = TryReadItemRow(ws, r, columns);
            if (item is null)
                continue;

            var errors = InvoiceValidationRules.ValidateItem(item);
            if (errors.Count > 0)
            {
                warnings.Add($"ردیف {r}: {errors[0]} — نادیده گرفته شد.");
                continue;
            }

            item.Recalculate();
            items.Add(item);
        }

        if (items.Count == 0)
            return InvoiceExcelParseResultDto.Fail(
                "هیچ قلم تیرچه‌ای در فایل پیدا نشد." +
                (warnings.Count > 0 ? " " + string.Join(" ", warnings) : string.Empty));

        var invoiceDate = TryFindPersianDate(ws, headerRow.Value);
        var title = TryFindTitle(ws, headerRow.Value);

        return InvoiceExcelParseResultDto.Ok(items, invoiceDate, shipping, title, warnings);
    }

    private static string? TryFindTitle(IXLWorksheet ws, int beforeRow)
    {
        if (beforeRow > 6)
        {
            var boxed = GetCellText(ws.Cell(6, 10));
            if (string.IsNullOrWhiteSpace(boxed))
                boxed = GetCellText(ws.Cell(6, 11));

            var normalized = NormalizeTitleCandidate(boxed);
            if (normalized is not null)
                return normalized;
        }

        for (var r = 1; r < beforeRow; r++)
        {
            for (var c = 10; c <= 11; c++)
            {
                var candidate = NormalizeTitleCandidate(GetCellText(ws.Cell(r, c)));
                if (candidate is null)
                    continue;

                if (candidate.Contains("تاریخ", StringComparison.Ordinal) ||
                    candidate.Contains("شماره", StringComparison.Ordinal) ||
                    TryParsePersianDate(candidate) is not null ||
                    int.TryParse(NormalizeDigits(candidate), out _))
                    continue;

                return candidate;
            }
        }

        return null;
    }

    private static string? NormalizeTitleCandidate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        if (trimmed.Length > InvoiceValidationRules.MaxTitleLength)
            trimmed = trimmed[..InvoiceValidationRules.MaxTitleLength].Trim();

        if (IsLegacyJoistTypeLabel(trimmed))
            return null;

        return trimmed;
    }

    private static bool IsLegacyJoistTypeLabel(string text)
    {
        var normalized = text.Replace(" ", string.Empty);
        return normalized is "25بتنی" or "20فلزی" or "25فلزی"
            || text is "بتنی ۲۵" or "فلزی ۲۰" or "فلزی ۲۵"
            || text.Equals("بتنی 25", StringComparison.Ordinal)
            || text.Equals("فلزی 20", StringComparison.Ordinal)
            || text.Equals("فلزی 25", StringComparison.Ordinal);
    }

    private static int? FindHeaderRow(IXLWorksheet ws, int lastRow, int lastCol)
    {
        for (var r = 1; r <= Math.Min(lastRow, 40); r++)
        {
            var hits = 0;
            for (var c = 1; c <= lastCol; c++)
            {
                var text = GetCellText(ws.Cell(r, c));
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (HeaderTokens.Any(t => text.Contains(t, StringComparison.Ordinal)))
                    hits++;
            }

            if (hits >= 4)
                return r;
        }

        return null;
    }

    private static Dictionary<Col, int> MapColumns(IXLWorksheet ws, int headerRow, int lastCol)
    {
        var map = new Dictionary<Col, int>();

        for (var c = 1; c <= lastCol; c++)
        {
            var text = NormalizeHeader(GetCellText(ws.Cell(headerRow, c)));
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (text.Contains("ردیف") && !map.ContainsKey(Col.Row))
                map[Col.Row] = c;
            else if (text.Contains("تعداد") && !map.ContainsKey(Col.Count))
                map[Col.Count] = c;
            else if (text.Contains("طول کل") && !map.ContainsKey(Col.TotalAmount))
                map[Col.TotalAmount] = c;
            else if (text.Contains("طول") && text.Contains("تقویت") && !map.ContainsKey(Col.ReinforcementPercent))
                map[Col.ReinforcementPercent] = c;
            else if (text.Contains("طول") && !map.ContainsKey(Col.Length))
                map[Col.Length] = c;
            else if (text.Contains("پایین") && !map.ContainsKey(Col.BottomRebar))
                map[Col.BottomRebar] = c;
            else if (text.Contains("بالا") && !map.ContainsKey(Col.TopRebar))
                map[Col.TopRebar] = c;
            else if ((text.Contains("میلگرد تقویت") || (text.Contains("تقویت") && !text.Contains("طول")))
                     && !map.ContainsKey(Col.ReinforcementBar))
                map[Col.ReinforcementBar] = c;
            else if (text.Contains("زیگزاگ") && !map.ContainsKey(Col.Zigzag))
                map[Col.Zigzag] = c;
            else if ((text.Contains("قیمت هر") || (text.Contains("قیمت") && text.Contains("متر")))
                     && !map.ContainsKey(Col.UnitPrice))
                map[Col.UnitPrice] = c;
            else if (text.Contains("قیمت کل") && !map.ContainsKey(Col.TotalPrice))
                map[Col.TotalPrice] = c;
        }

        map.TryAdd(Col.Row, 1);
        map.TryAdd(Col.Count, 2);
        map.TryAdd(Col.Length, 3);
        map.TryAdd(Col.BottomRebar, 4);
        map.TryAdd(Col.TopRebar, 5);
        map.TryAdd(Col.ReinforcementBar, 6);
        map.TryAdd(Col.ReinforcementPercent, 7);
        map.TryAdd(Col.Zigzag, 8);
        map.TryAdd(Col.UnitPrice, 9);
        map.TryAdd(Col.TotalAmount, 10);
        map.TryAdd(Col.TotalPrice, 11);

        return map;
    }

    private static InvoiceItemFormDto? TryReadItemRow(
        IXLWorksheet ws,
        int row,
        IReadOnlyDictionary<Col, int> columns)
    {
        var count = TryReadInt(ws.Cell(row, columns[Col.Count]));
        var length = TryReadDecimal(ws.Cell(row, columns[Col.Length]));
        var unitPrice = TryReadDecimal(ws.Cell(row, columns[Col.UnitPrice]));

        if (count is null or <= 0 || length is null or <= 0)
            return null;

        var bottom = ParseRebar(GetCellText(ws.Cell(row, columns[Col.BottomRebar]))) ?? 0;
        var top = TryReadInt(ws.Cell(row, columns[Col.TopRebar])) ?? 0;
        var reinforcement = TryReadInt(ws.Cell(row, columns[Col.ReinforcementBar]));
        var percent = ParseReinforcementPercent(ws.Cell(row, columns[Col.ReinforcementPercent]));
        var zigzag = ParseRebar(GetCellText(ws.Cell(row, columns[Col.Zigzag]))) ?? 0;

        return new InvoiceItemFormDto
        {
            Count = count.Value,
            Length = length.Value,
            UnitPrice = unitPrice ?? 0m,
            BottomRebar = bottom,
            TopRebar = top,
            ReinforcementBar = reinforcement,
            ReinforcementPercent = percent,
            Zigzag = zigzag
        };
    }

    private static bool LooksLikeSubtotalRow(
        IXLWorksheet ws,
        int row,
        IReadOnlyDictionary<Col, int> columns)
    {
        var lengthText = GetCellText(ws.Cell(row, columns[Col.Length]));
        var rowIndexText = GetCellText(ws.Cell(row, columns[Col.Row]));
        var unitText = GetCellText(ws.Cell(row, columns[Col.UnitPrice]));

        if (!string.IsNullOrWhiteSpace(lengthText) || !string.IsNullOrWhiteSpace(unitText))
            return false;

        if (!string.IsNullOrWhiteSpace(rowIndexText) && int.TryParse(NormalizeDigits(rowIndexText), out _))
            return false;

        var count = TryReadInt(ws.Cell(row, columns[Col.Count]));
        var totalAmount = TryReadDecimal(ws.Cell(row, columns[Col.TotalAmount]));
        return count is not null || totalAmount is not null;
    }

    private static decimal? TryReadMoney(IXLWorksheet ws, int row, int preferredCol, int lastCol)
    {
        var preferred = TryReadDecimal(ws.Cell(row, preferredCol));
        if (preferred is > 0)
            return preferred;

        for (var c = lastCol; c >= 1; c--)
        {
            var value = TryReadDecimal(ws.Cell(row, c));
            if (value is > 0)
                return value;
        }

        return null;
    }

    private static DateTime? TryFindPersianDate(IXLWorksheet ws, int beforeRow)
    {
        for (var r = 1; r < beforeRow; r++)
        {
            for (var c = 1; c <= 15; c++)
            {
                var text = GetCellText(ws.Cell(r, c));
                var parsed = TryParsePersianDate(text);
                if (parsed is not null)
                    return parsed;
            }
        }

        return null;
    }

    private static DateTime? TryParsePersianDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = NormalizeDigits(text.Trim());
        var match = Regex.Match(normalized, @"(\d{3,4})[/\-.](\d{1,2})[/\-.](\d{1,2})");
        if (!match.Success)
            return null;

        if (!int.TryParse(match.Groups[1].Value, out var year) ||
            !int.TryParse(match.Groups[2].Value, out var month) ||
            !int.TryParse(match.Groups[3].Value, out var day))
            return null;

        if (year is >= 100 and < 1300)
            year += 1000;

        try
        {
            var calendar = new PersianCalendar();
            return calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    private static int? ParseRebar(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = NormalizeDigits(text)
            .Replace('×', 'x')
            .Replace('*', 'x')
            .Replace('Ｘ', 'x')
            .Trim();

        var match = Regex.Match(normalized, @"^(?:2\s*x\s*)?(\d+)$", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
            return value;

        return TryReadIntFromText(normalized);
    }

    private static int? ParseReinforcementPercent(IXLCell cell)
    {
        if (cell.TryGetValue(out double dbl))
        {
            if (dbl is > 0 and <= 1)
                return (int)Math.Round(dbl * 100, MidpointRounding.AwayFromZero);
            if (dbl is > 1 and <= 100)
                return (int)Math.Round(dbl, MidpointRounding.AwayFromZero);
        }

        var text = GetCellText(cell);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = NormalizeDigits(text).Replace("%", string.Empty).Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return null;

        if (value is > 0 and <= 1)
            return (int)Math.Round(value * 100, MidpointRounding.AwayFromZero);

        if (value is >= 0 and <= 100)
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);

        return null;
    }

    private static int? TryReadInt(IXLCell cell)
    {
        if (cell.TryGetValue(out double dbl))
            return (int)Math.Round(dbl, MidpointRounding.AwayFromZero);

        return TryReadIntFromText(GetCellText(cell));
    }

    private static int? TryReadIntFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = NormalizeDigits(text);
        return int.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static decimal? TryReadDecimal(IXLCell cell)
    {
        if (cell.TryGetValue(out double dbl))
            return Convert.ToDecimal(dbl);

        var text = GetCellText(cell);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = NormalizeDigits(text)
            .Replace(",", string.Empty)
            .Replace("ریال", string.Empty)
            .Trim();

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string GetCellText(IXLCell cell)
    {
        try
        {
            return cell.GetFormattedString()?.Trim() ?? string.Empty;
        }
        catch
        {
            return cell.GetString()?.Trim() ?? string.Empty;
        }
    }

    private static string ConcatRowText(IXLWorksheet ws, int row, int lastCol)
    {
        var parts = new List<string>();
        for (var c = 1; c <= lastCol; c++)
        {
            var text = GetCellText(ws.Cell(row, c));
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text);
        }

        return string.Join(" ", parts);
    }

    private static bool IsKeywordRow(string rowText, string keyword) =>
        rowText.Contains(keyword, StringComparison.Ordinal);

    private static string NormalizeHeader(string text) =>
        text.Replace('\u200c', ' ').Trim();

    private static string NormalizeDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var buffer = new char[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            buffer[i] = ch switch
            {
                >= '\u06F0' and <= '\u06F9' => (char)('0' + (ch - '\u06F0')),
                >= '\u0660' and <= '\u0669' => (char)('0' + (ch - '\u0660')),
                _ => ch
            };
        }

        return new string(buffer);
    }

    private enum Col
    {
        Row,
        Count,
        Length,
        BottomRebar,
        TopRebar,
        ReinforcementBar,
        ReinforcementPercent,
        Zigzag,
        UnitPrice,
        TotalAmount,
        TotalPrice
    }
    #endregion
}
