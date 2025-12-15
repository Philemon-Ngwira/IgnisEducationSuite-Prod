using ChartJs.Blazor.Common;
using MudBlazor;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IgnisEducationSuite.Client.Layout
{
    public class Theme : MudTheme
    {
        public Theme()
        {

            PaletteLight = new PaletteLight()
            {
                Primary = "#29104A",
                PrimaryLighten = "#7A4DBB",
                Background = "#F6F1FA",
                Surface = "#F0E6F6",
                TextPrimary = "#1B0F28",
                TextSecondary = "#4D3A5A",
                Success = "#4CAF50",
                Warning = "#F7B74A",
                Error = "#E53935",
                Info = " #5C6BC0",
            };
            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".925rem",
                    FontWeight = "400",
                    LineHeight = "1.45",
                    LetterSpacing = ".010em"
                },

                /* ---- HEADINGS (Luxury Serif) ---- */
                H1 = new H1Typography()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "4.5rem",
                    FontWeight = "300",
                    LineHeight = "1.1",
                    LetterSpacing = "-.020em"
                },
                H2 = new H2Typography()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "3rem",
                    FontWeight = "300",
                    LineHeight = "1.15",
                    LetterSpacing = "-.010em"
                },
                H3 = new H3Typography()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "2.25rem",
                    FontWeight = "400",
                    LineHeight = "1.2",
                    LetterSpacing = "0"
                },
                H4 = new H4Typography()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "1.8rem",
                    FontWeight = "400",
                    LineHeight = "1.25"
                },
                H5 = new H5Typography()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "1.4rem",
                    FontWeight = "400",
                    LineHeight = "1.3"
                },
                H6 = new H6Typography()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "1.15rem",
                    FontWeight = "500",
                    LineHeight = "1.4"
                },

                /* ---- BUTTONS (UI Font) ---- */
                Button = new ButtonTypography()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".90rem",
                    FontWeight = "600",
                    LetterSpacing = ".028em",
                    LineHeight = "1.7"
                },

                /* ---- BODY TEXT ---- */
                Body1 = new Body1Typography()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = "1rem",
                    FontWeight = "400",
                    LineHeight = "1.55"
                },
                Body2 = new Body2Typography()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".875rem",
                    FontWeight = "400",
                    LineHeight = "1.45"
                },

                Caption = new CaptionTypography()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".75rem",
                    FontWeight = "400",
                    LineHeight = "1.6",
                    LetterSpacing = ".03em"
                },

                Subtitle2 = new Subtitle2Typography()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".9rem",
                    FontWeight = "500",
                    LineHeight = "1.55"
                }
            };
        }
    }
}