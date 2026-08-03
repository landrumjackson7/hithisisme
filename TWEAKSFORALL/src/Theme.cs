using System.Drawing;

namespace TweaksForAll
{
    // Black + blue palette used across the whole app.
    internal static class Theme
    {
        public static readonly Color Bg = ColorTranslator.FromHtml("#07090E");
        public static readonly Color Sidebar = ColorTranslator.FromHtml("#0A0D14");
        public static readonly Color Panel = ColorTranslator.FromHtml("#0F131C");
        public static readonly Color PanelAlt = ColorTranslator.FromHtml("#141A26");
        public static readonly Color Card = ColorTranslator.FromHtml("#111725");
        public static readonly Color Border = ColorTranslator.FromHtml("#1B2434");

        public static readonly Color Accent = ColorTranslator.FromHtml("#2E7BFF");
        public static readonly Color AccentBright = ColorTranslator.FromHtml("#4C9BFF");
        public static readonly Color AccentDim = ColorTranslator.FromHtml("#173A78");
        public static readonly Color AccentGlow = ColorTranslator.FromHtml("#0E2244");

        public static readonly Color Text = ColorTranslator.FromHtml("#EAF1FF");
        public static readonly Color SubText = ColorTranslator.FromHtml("#7F8CA6");
        public static readonly Color Muted = ColorTranslator.FromHtml("#54607A");
        public static readonly Color Warn = ColorTranslator.FromHtml("#FFB24C");
        public static readonly Color Good = ColorTranslator.FromHtml("#39D98A");

        public static Font H1 = new Font("Segoe UI", 20f, FontStyle.Bold);
        public static Font H2 = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold);
        public static Font Body = new Font("Segoe UI", 9.75f, FontStyle.Regular);
        public static Font Small = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static Font Logo = new Font("Segoe UI", 15f, FontStyle.Bold);
        public static Font NavFont = new Font("Segoe UI Semibold", 10.5f, FontStyle.Regular);
    }
}
