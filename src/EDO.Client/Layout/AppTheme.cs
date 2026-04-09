using MudBlazor;

namespace EDO.Client.Layout;

/// <summary>
/// Единая тёмная тема приложения. Меняем только цвета/закругления/типографику —
/// структура и функционал страниц не затрагиваются.
/// </summary>
public static class AppTheme
{
    private static readonly string[] FontStack =
    {
        "Inter", "Montserrat", "Roboto", "Helvetica", "Arial", "sans-serif"
    };

    public static readonly MudTheme Dark = new()
    {
        PaletteDark = new PaletteDark
        {
            // Фоны — глубокий сине-чёрный
            Black = "#000000",
            Background = "#0B1120",
            Surface = "#151C2C",
            DrawerBackground = "#111827",
            DrawerText = "#FFFFFF",
            DrawerIcon = "#94A3B8",
            AppbarBackground = "#0B1120",
            AppbarText = "#FFFFFF",

            // Главный акцент — неоновый циан / teal
            Primary = "#06B6D4",
            PrimaryContrastText = "#0B1120",
            Secondary = "#94A3B8",
            SecondaryContrastText = "#0B1120",
            Tertiary = "#22D3EE",
            TertiaryContrastText = "#0B1120",

            // Смысловые цвета — с тёмным контрастным текстом, чтобы статусы читались
            Success = "#22C55E",
            SuccessContrastText = "#0B1120",
            Info = "#22D3EE",
            InfoContrastText = "#0B1120",
            Warning = "#F59E0B",
            WarningContrastText = "#0B1120",
            Error = "#EF4444",
            ErrorContrastText = "#FFFFFF",
            Dark = "#0B1120",
            DarkContrastText = "#FFFFFF",

            // Текст
            TextPrimary = "#FFFFFF",
            TextSecondary = "#94A3B8",
            TextDisabled = "#475569",
            ActionDefault = "#94A3B8",
            ActionDisabled = "#475569",
            ActionDisabledBackground = "#1E293B",

            // Линии и границы — тусклый сине-серый
            LinesDefault = "#1E293B",
            LinesInputs = "#1E293B",
            Divider = "#1E293B",
            DividerLight = "#151C2C",
            TableLines = "#1E293B",
            TableStriped = "#101728",
            TableHover = "#1B2436",

            // Overlays
            HoverOpacity = 0.08
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontStack,
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "normal"
            },
            H1 = new H1Typography { FontFamily = FontStack, FontWeight = "700" },
            H2 = new H2Typography { FontFamily = FontStack, FontWeight = "700" },
            H3 = new H3Typography { FontFamily = FontStack, FontWeight = "700" },
            H4 = new H4Typography { FontFamily = FontStack, FontWeight = "700" },
            H5 = new H5Typography { FontFamily = FontStack, FontWeight = "700" },
            H6 = new H6Typography { FontFamily = FontStack, FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontFamily = FontStack, FontWeight = "600" },
            Subtitle2 = new Subtitle2Typography { FontFamily = FontStack, FontWeight = "600" },
            Body1 = new Body1Typography { FontFamily = FontStack, FontWeight = "400" },
            Body2 = new Body2Typography { FontFamily = FontStack, FontWeight = "400" },
            Button = new ButtonTypography
            {
                FontFamily = FontStack,
                FontWeight = "600",
                TextTransform = "none"
            },
            Caption = new CaptionTypography { FontFamily = FontStack, FontWeight = "400" },
            Overline = new OverlineTypography { FontFamily = FontStack, FontWeight = "500" }
        }
    };
}
