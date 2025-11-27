using MudBlazor;

namespace IgnisEducationSuite.Client.Layout
{
    public class Theme : MudTheme
    {
        public Theme()
        {

            Typography = new Typography()
            {
                Default = new Default()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".925rem",
                    FontWeight = 400,
                    LineHeight = 1.45,
                    LetterSpacing = ".010em"
                },

                /* ---- HEADINGS (Luxury Serif) ---- */
                H1 = new H1()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "4.5rem",
                    FontWeight = 300,
                    LineHeight = 1.1,
                    LetterSpacing = "-.020em"
                },
                H2 = new H2()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "3rem",
                    FontWeight = 300,
                    LineHeight = 1.15,
                    LetterSpacing = "-.010em"
                },
                H3 = new H3()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "2.25rem",
                    FontWeight = 400,
                    LineHeight = 1.2,
                    LetterSpacing = "0"
                },
                H4 = new H4()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "1.8rem",
                    FontWeight = 400,
                    LineHeight = 1.25
                },
                H5 = new H5()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "1.4rem",
                    FontWeight = 400,
                    LineHeight = 1.3
                },
                H6 = new H6()
                {
                    FontFamily = new[] { "Cormorant Garamond", "Georgia", "serif" },
                    FontSize = "1.15rem",
                    FontWeight = 500,
                    LineHeight = 1.4
                },

                /* ---- BUTTONS (UI Font) ---- */
                Button = new Button()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".90rem",
                    FontWeight = 600,
                    LetterSpacing = ".028em",
                    LineHeight = 1.7
                },

                /* ---- BODY TEXT ---- */
                Body1 = new Body1()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = "1rem",
                    FontWeight = 400,
                    LineHeight = 1.55
                },
                Body2 = new Body2()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".875rem",
                    FontWeight = 400,
                    LineHeight = 1.45
                },

                Caption = new Caption()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".75rem",
                    FontWeight = 400,
                    LineHeight = 1.6,
                    LetterSpacing = ".03em"
                },

                Subtitle2 = new Subtitle2()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = ".9rem",
                    FontWeight = 500,
                    LineHeight = 1.55
                }
            };

        }
    }
}
