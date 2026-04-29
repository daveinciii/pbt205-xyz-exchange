using Newtonsoft.Json;
using TradingCore.Cli;


namespace TradingCore.Configuration
{
    // Loads and validates the list of stocks the trading system supports.
    //
    // The configuration file (tradingsystem.config.json) is searched for in the
    // current working directory and walked up to three levels of parent
    // directories. This keeps CLI apps (which run from their own bin folder)
    // and the GUI host (which runs from the project root) using the same file.
    //
    // If no config file is found, a sensible default of XYZ, ABC, DEF is used
    // and a warning is logged so the system still runs out-of-the-box.
    //
    // Satisfies A2 FR-01 (multi-stock support) and TR-07 (no hardcoded values).
    public static class StockConfig
    {
        private const string ConfigFileName = "tradingsystem.config.json";
        private static readonly string[] DefaultStocks = { "XYZ", "ABC", "DEF" };

        // Loaded once on first access — cheap to call repeatedly.
        private static IReadOnlyList<string>? _cached;

        // Returns the configured list of stock symbols, normalised to upper case.
        // Always non-empty; falls back to defaults if no config is found.
        public static IReadOnlyList<string> Stocks
        {
            get
            {
                if (_cached == null)
                {
                    _cached = LoadStocks();
                }
                return _cached;
            }
        }

        // True if the symbol matches a configured stock (case-insensitive).
        public static bool IsValid(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            return Stocks.Contains(symbol.Trim().ToUpperInvariant());
        }

        // Returns the canonical (upper-case, trimmed) form of a symbol.
        // Throws if the symbol is not in the configured list.
        public static string Normalise(string symbol)
        {
            var clean = symbol?.Trim().ToUpperInvariant() ?? "";
            if (!Stocks.Contains(clean))
            {
                throw new ArgumentException(
                    $"Unknown stock '{symbol}'. Configured stocks: {string.Join(", ", Stocks)}");
            }
            return clean;
        }

        // Forces a reload on next access — used by tests.
        public static void Reset() => _cached = null;

        // ---- internals ------------------------------------------------------

        private static IReadOnlyList<string> LoadStocks()
        {
            var path = FindConfigFile();
            if (path == null)
            {
                ConsoleUi.Box("StockConfig", $"{ConfigFileName} not found — using defaults: {string.Join(", ", DefaultStocks)}");
                return DefaultStocks;
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<ConfigFile>(json);
                var stocks = config?.Stocks;

                if (stocks == null || stocks.Count == 0)
                {
                    ConsoleUi.Box("StockConfig", $"No stocks defined in config — using defaults.");
                    return DefaultStocks;
                }

                // Normalise: trim, upper-case, drop empties, drop duplicates, preserve order.
                var clean = stocks
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                if (clean.Count == 0)
                {
                    ConsoleUi.Box("StockConfig", "All entries blank — using defaults.");
                    return DefaultStocks;
                }

                ConsoleUi.Box("StockConfig", $"Loaded {clean.Count} stocks: {string.Join(", ", clean)}");
                return clean;
            }
            catch (Exception ex)
            {
                ConsoleUi.Error($"StockConfig failed to read {path}: {ex.Message} — using defaults.");
                return DefaultStocks;
            }
        }

        // Walks up from the current directory looking for the config file.
        // Returns null if not found within 3 parent directories.
        private static string? FindConfigFile()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 4 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, ConfigFileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        private class ConfigFile
        {
            public List<string>? Stocks { get; set; }
        }
    }
}