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
            // Фоны — строгий монохромный графит, без синего оттенка
            Black = "#000000",
            Background = "#0A0A0A",
            Surface = "#1A1A1A",
            DrawerBackground = "#0A0A0A",
            DrawerText = "#FFFFFF",
            DrawerIcon = "#9E9E9E",
            AppbarBackground = "#0A0A0A",
            AppbarText = "#FFFFFF",

            // Главный акцент — неоновый циан / teal
            Primary = "#06B6D4",
            PrimaryContrastText = "#0A0A0A",
            Secondary = "#9E9E9E",
            SecondaryContrastText = "#0A0A0A",
            Tertiary = "#22D3EE",
            TertiaryContrastText = "#0A0A0A",

            // Смысловые цвета — с тёмным контрастным текстом, чтобы статусы читались
            Success = "#22C55E",
            SuccessContrastText = "#0A0A0A",
            Info = "#22D3EE",
            InfoContrastText = "#0A0A0A",
            Warning = "#F59E0B",
            WarningContrastText = "#0A0A0A",
            Error = "#EF4444",
            ErrorContrastText = "#FFFFFF",
            Dark = "#0A0A0A",
            DarkContrastText = "#FFFFFF",

            // Текст — нейтральный серый
            TextPrimary = "#FFFFFF",
            TextSecondary = "#9E9E9E",
            TextDisabled = "#5C5C5C",
            ActionDefault = "#9E9E9E",
            ActionDisabled = "#5C5C5C",
            ActionDisabledBackground = "#1F1F1F",

            // Линии и границы — нейтральный темно-серый
            LinesDefault = "#2A2A2A",
            LinesInputs = "#2A2A2A",
            Divider = "#2A2A2A",
            DividerLight = "#1A1A1A",
            TableLines = "#2A2A2A",
            TableStriped = "#141414",
            TableHover = "#222222",

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
