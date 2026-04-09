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
            // Фоны
            Black = "#000000",
            Background = "#111112",
            Surface = "#1E1E1F",
            DrawerBackground = "#161617",
            DrawerText = "#FFFFFF",
            DrawerIcon = "#A1A1A1",
            AppbarBackground = "#161617",
            AppbarText = "#FFFFFF",

            // Главный акцент — неоновый циан
            Primary = "#00E5FF",
            PrimaryContrastText = "#0B0B0C",
            Secondary = "#A1A1A1",
            SecondaryContrastText = "#FFFFFF",

            // Сохраняем смысловые цвета (используются в существующих кнопках/чипах)
            Success = "#22C55E",
            Info = "#00E5FF",
            Warning = "#FACC15",
            Error = "#EF4444",

            // Текст
            TextPrimary = "#FFFFFF",
            TextSecondary = "#A1A1A1",
            TextDisabled = "#5C5C5E",
            ActionDefault = "#A1A1A1",
            ActionDisabled = "#5C5C5E",
            ActionDisabledBackground = "#2A2A2B",

            // Линии и границы
            LinesDefault = "#333333",
            LinesInputs = "#333333",
            Divider = "#262627",
            DividerLight = "#1E1E1F",
            TableLines = "#262627",
            TableStriped = "#1A1A1B",
            TableHover = "#252526",

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
