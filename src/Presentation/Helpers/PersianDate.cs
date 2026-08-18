using System.Globalization;

namespace Behsazan.Presentation.Helpers;

public static class PersianDate
{
    private static readonly PersianCalendar Calendar = new();

    public static CultureInfo Culture { get; } = CreateCulture();

    private static CultureInfo CreateCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("fa-IR").Clone();
        culture.DateTimeFormat.Calendar = new PersianCalendar();
        culture.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        culture.DateTimeFormat.LongDatePattern = "dddd، dd MMMM yyyy";
        culture.DateTimeFormat.FullDateTimePattern = "yyyy/MM/dd HH:mm";
        culture.DateTimeFormat.MonthDayPattern = "dd MMMM";
        culture.DateTimeFormat.YearMonthPattern = "MMMM yyyy";
        culture.NumberFormat.DigitSubstitution = DigitShapes.None;
        return CultureInfo.ReadOnly(culture);
    }

    public static string Format(DateTime value, bool includeTime = true)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

        var year = Calendar.GetYear(local);
        var month = Calendar.GetMonth(local);
        var day = Calendar.GetDayOfMonth(local);

        return includeTime
            ? $"{year:0000}/{month:00}/{day:00} - {local:HH:mm}"
            : $"{year:0000}/{month:00}/{day:00}";
    }

    public static string FormatOrDash(DateTime? value, bool includeTime = true) =>
        value.HasValue ? Format(value.Value, includeTime) : "—";

    private static readonly string[] JalaliMonthNames =
    [
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    ];

    private static readonly string[] GregorianMonthNamesFa =
    [
        "ژانویه", "فوریه", "مارس", "آوریل", "مه", "ژوئن",
        "ژوئیه", "اوت", "سپتامبر", "اکتبر", "نوامبر", "دسامبر"
    ];

    public static string FormatGregorianMonthYear(int gregorianYear, int gregorianMonth)
    {
        if (gregorianMonth is < 1 or > 12)
            return $"{gregorianYear}/{gregorianMonth:00}";

        return $"{GregorianMonthNamesFa[gregorianMonth - 1]} {gregorianYear}";
    }

    public static DateTime StartOfCurrentJalaliMonth(DateTime? today = null)
    {
        var local = (today ?? DateTime.Today).Date;
        var year = Calendar.GetYear(local);
        var month = Calendar.GetMonth(local);
        return Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
    }

    public static DateTime StartOfCurrentJalaliYear(DateTime? today = null)
    {
        var local = (today ?? DateTime.Today).Date;
        var year = Calendar.GetYear(local);
        return Calendar.ToDateTime(year, 1, 1, 0, 0, 0, 0);
    }

    public static string FormatMonthYear(int gregorianYear, int gregorianMonth)
    {
        if (gregorianMonth is < 1 or > 12)
            return $"{gregorianYear}/{gregorianMonth:00}";

        var local = new DateTime(gregorianYear, gregorianMonth, 1);
        var year = Calendar.GetYear(local);
        var month = Calendar.GetMonth(local);
        return $"{JalaliMonthNames[month - 1]} {year}";
    }

    public static string FormatMonthYearShort(int gregorianYear, int gregorianMonth)
    {
        if (gregorianMonth is < 1 or > 12)
            return $"{gregorianYear}/{gregorianMonth:00}";

        var local = new DateTime(gregorianYear, gregorianMonth, 1);
        var year = Calendar.GetYear(local);
        var month = Calendar.GetMonth(local);
        var name = JalaliMonthNames[month - 1];
        var shortName = name.Length <= 3 ? name : name[..3];
        return $"{shortName} {year}";
    }

    public static string FormatDayLabel(DateTime date)
    {
        var local = date.Kind == DateTimeKind.Utc ? date.ToLocalTime() : date;
        var month = Calendar.GetMonth(local);
        var day = Calendar.GetDayOfMonth(local);
        return $"{day} {JalaliMonthNames[month - 1]}";
    }

    public static string FormatWeekLabel(DateTime weekStart, DateTime? rangeEndInclusive = null)
    {
        var start = weekStart.Date;
        var end = start.AddDays(6);
        if (rangeEndInclusive.HasValue && end > rangeEndInclusive.Value.Date)
            end = rangeEndInclusive.Value.Date;

        return $"{FormatDayLabel(start)} – {FormatDayLabel(end)}";
    }

    public static DateTime StartOfJalaliMonthMonthsAgo(int monthsBack, DateTime? today = null)
    {
        var local = (today ?? DateTime.Today).Date;
        var year = Calendar.GetYear(local);
        var month = Calendar.GetMonth(local);
        var total = year * 12 + (month - 1) - monthsBack;
        var y = total / 12;
        var m = total % 12 + 1;
        return Calendar.ToDateTime(y, m, 1, 0, 0, 0, 0);
    }
}
