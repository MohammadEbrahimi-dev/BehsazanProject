using MudBlazor;

namespace Behsazan.Components.Layout;

public static class BehsazanTheme
{
    public static MudTheme Instance { get; } = Create();

    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0F766E",
            Secondary = "#334155",
            Tertiary = "#D97706",
            Info = "#2563EB",
            Success = "#16A34A",
            Warning = "#D97706",
            Error = "#DC2626",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#0F172A",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#334155",
            DrawerIcon = "#64748B",
            Background = "#F8FAFC",
            Surface = "#FFFFFF",
            BackgroundGray = "#F1F5F9",
            TextPrimary = "#0F172A",
            TextSecondary = "#64748B",
            ActionDefault = "#334155",
            LinesDefault = "#E2E8F0",
            TableLines = "#E2E8F0",
            Divider = "#E2E8F0",
            OverlayLight = "rgba(15,23,42,0.4)",
            HoverOpacity = 0.04,
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2DD4BF",
            Secondary = "#94A3B8",
            Tertiary = "#FBBF24",
            Info = "#60A5FA",
            Success = "#4ADE80",
            Warning = "#FBBF24",
            Error = "#F87171",
            AppbarBackground = "#0B1220",
            AppbarText = "#E2E8F0",
            DrawerBackground = "#0F172A",
            DrawerText = "#CBD5E1",
            DrawerIcon = "#94A3B8",
            Background = "#020617",
            Surface = "#0F172A",
            BackgroundGray = "#111827",
            TextPrimary = "#E2E8F0",
            TextSecondary = "#94A3B8",
            ActionDefault = "#CBD5E1",
            LinesDefault = "#1E293B",
            TableLines = "#1E293B",
            Divider = "#1E293B",
            OverlayDark = "rgba(2,6,23,0.7)",
            HoverOpacity = 0.08,
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.6",
            },
            H3 = new H3Typography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontWeight = "600",
                FontSize = "1.75rem",
                LineHeight = "1.35",
            },
            H4 = new H4Typography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontWeight = "600",
                FontSize = "1.4rem",
            },
            H5 = new H5Typography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontWeight = "600",
                FontSize = "1.15rem",
            },
            H6 = new H6Typography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontWeight = "600",
                FontSize = "1rem",
            },
            Button = new ButtonTypography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontWeight = "600",
                TextTransform = "none",
                FontSize = "0.875rem",
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontSize = "0.875rem",
            },
            Caption = new CaptionTypography
            {
                FontFamily = ["B Nazanin", "Tahoma", "Segoe UI", "sans-serif"],
                FontSize = "0.75rem",
            },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "232px",
            DrawerWidthRight = "232px",
            AppbarHeight = "56px",
        }
    };
}
