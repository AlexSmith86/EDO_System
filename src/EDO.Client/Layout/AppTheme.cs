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
            // Фоны — глубокий тёплый графит в стиле Binance
            Black = "#000000",
            Background = "#181A20",
            Surface = "#262321",
            DrawerBackground = "#181A20",
            DrawerText = "#EAECEF",
            DrawerIcon = "#848E9C",
            AppbarBackground = "#181A20",
            AppbarText = "#EAECEF",

            // Главный акцент — фирменный жёлтый Binance
            Primary = "#FCD535",
            PrimaryContrastText = "#181A20",
            Secondary = "#848E9C",
            SecondaryContrastText = "#181A20",
            Tertiary = "#F0B90B",
            TertiaryContrastText = "#181A20",

            // Смысловые цвета — статусы с тёмным контрастным текстом
            Success = "#F0B90B",
            SuccessContrastText = "#181A20",
            Info = "#848E9C",
            InfoContrastText = "#181A20",
            Warning = "#FCD535",
            WarningContrastText = "#181A20",
            Error = "#F6465D",
            ErrorContrastText = "#FFFFFF",
            Dark = "#181A20",
            DarkContrastText = "#EAECEF",

            // Текст
            TextPrimary = "#EAECEF",
            TextSecondary = "#848E9C",
            TextDisabled = "#5E6673",
            ActionDefault = "#848E9C",
            ActionDisabled = "#5E6673",
            ActionDisabledBackground = "#2B3139",

            // Линии и границы — нейтральный графитовый
            LinesDefault = "#2A2A2A",
            LinesInputs = "#33302E",
            Divider = "#2A2A2A",
            DividerLight = "#262321",
            TableLines = "#2A2A2A",
            TableStriped = "#1E1C1A",
            TableHover = "#2B3139",

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
