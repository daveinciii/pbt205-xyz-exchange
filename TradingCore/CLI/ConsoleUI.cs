namespace TradingCore.Cli
{
    // Centralises console output formatting for all CLI applications.
    //
    // Replaces the inline box-drawing scattered through SendOrderApp, ExchangeApp
    // and RabbitMQService. The previous approach had alignment bugs (rows missing
    // their right border, headers mis-aligned with body lines) and broken format
    // strings — for example "${order.Price:F2,-10}" silently ignores the
    // alignment because it sits inside the format specifier rather than before it.
    //
    // All boxes share a fixed inner width so output stays clean across apps.
    public static class ConsoleUi
    {
        // Inner width of every box (the space between the borders).
        private const int InnerWidth = 56;

        private static readonly string Bottom = "└─" + new string('─', InnerWidth) + "─┘";

        // Banner with a centred title — used at startup.
        public static void Banner(string title)
        {
            var top    = "╔" + new string('═', InnerWidth + 2) + "╗";
            var bottom = "╚" + new string('═', InnerWidth + 2) + "╝";
            Console.WriteLine(top);
            Console.WriteLine(Centre(title));
            Console.WriteLine(bottom);
        }

        // Single-line box — title at the top, one body line.
        public static void Box(string title, string line)
        {
            WriteHeader(title);
            WriteBodyLine(line);
            Console.WriteLine(Bottom);
        }

        // Multi-line box — title at the top, several body lines.
        public static void Box(string title, params string[] lines)
        {
            WriteHeader(title);
            foreach (var line in lines)
            {
                WriteBodyLine(line);
            }
            Console.WriteLine(Bottom);
        }

        // Error box — distinct double-bar style so failures stand out in the demo.
        public static void Error(string message)
        {
            var top    = "╓─ ERROR " + new string('─', InnerWidth - 6) + "─╖";
            var bottom = "╙─" + new string('─', InnerWidth) + "─╜";
            Console.WriteLine(top);
            WriteBodyLine(message);
            Console.WriteLine(bottom);
        }

        // ---- internals ------------------------------------------------------

        private static void WriteHeader(string title)
        {
            // "┌─ TITLE ──...─┐" right-padded to fixed width.
            var labelled = $"─ {title.ToUpperInvariant()} ";
            var fillLen = InnerWidth + 2 - labelled.Length;
            if (fillLen < 0) fillLen = 0;
            Console.WriteLine("┌" + labelled + new string('─', fillLen) + "┐");
        }

        private static void WriteBodyLine(string line)
        {
            // Trim or pad the line to exactly InnerWidth visible characters
            // so every row lines up against the right border.
            if (line.Length > InnerWidth)
            {
                line = line.Substring(0, InnerWidth);
            }
            Console.WriteLine("│ " + line.PadRight(InnerWidth) + " │");
        }

        private static string Centre(string text)
        {
            if (text.Length >= InnerWidth) text = text.Substring(0, InnerWidth);
            int pad = (InnerWidth - text.Length) / 2;
            var padded = new string(' ', pad) + text + new string(' ', InnerWidth - text.Length - pad);
            return "║ " + padded + " ║";
        }
    }
}