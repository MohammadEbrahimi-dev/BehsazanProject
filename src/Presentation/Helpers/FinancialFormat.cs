namespace Behsazan.Presentation.Helpers;

public static class FinancialFormat
{
    public const string CurrencyUnit = "ریال";

    public static string Full(decimal value) => value.ToString("N0");

    public static string FullRial(decimal value) => $"{Full(value)} {CurrencyUnit}";

    public static string Compact(double value) => Compact((decimal)value);

    public static string Compact(decimal value)
    {
        var sign = value < 0 ? "-" : string.Empty;
        var abs = Math.Abs(value);

        if (abs >= 1_000_000_000m)
        {
            var scaled = abs / 1_000_000_000m;
            return $"{sign}{FormatScaled(scaled)} میلیارد";
        }

        if (abs >= 1_000_000m)
        {
            var scaled = abs / 1_000_000m;
            return scaled >= 100m
                ? $"{sign}{scaled:N0} میلیون"
                : $"{sign}{FormatScaled(scaled)} میلیون";
        }

        if (abs >= 1_000m)
            return $"{sign}{(abs / 1_000m):N0} هزار";

        return $"{sign}{abs:N0}";
    }

    private static string FormatScaled(decimal scaled)
    {
        if (scaled == Math.Truncate(scaled))
            return scaled.ToString("0");

        return scaled.ToString("0.#");
    }
}
