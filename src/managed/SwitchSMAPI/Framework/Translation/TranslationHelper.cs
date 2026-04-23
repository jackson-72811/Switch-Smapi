using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SwitchSMAPI.Framework.Logging;

namespace SwitchSMAPI.Framework.Translation {

    /// <inheritdoc cref="ITranslationHelper"/>
    public class TranslationHelper : ITranslationHelper {

        private readonly string   _modDirectory;
        private readonly IMonitor _monitor;
        private Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string Locale { get; private set; } = "en";

        public TranslationHelper(string modDirectory, IMonitor monitor) {
            _modDirectory = modDirectory;
            _monitor      = monitor;
            Reload(GetGameLocale());
        }

        public Translation Get(string key)              => Get(key, null);
        public Translation Get(string key, object? tokens) {
            _strings.TryGetValue(key, out string? raw);
            raw ??= $"(no translation:{key})";
            return new TranslationImpl(key, raw, tokens);
        }

        public IEnumerable<string> GetTranslations() => _strings.Keys;

        // ── Locale loading ────────────────────────────────────────────────────

        public void Reload(string locale) {
            Locale  = locale;
            _strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string i18nDir = Path.Combine(_modDirectory, "i18n");
            if (!Directory.Exists(i18nDir)) return;

            // Load default.json first, then overlay locale-specific
            LoadFile(Path.Combine(i18nDir, "default.json"));
            if (!locale.Equals("en", StringComparison.OrdinalIgnoreCase)) {
                LoadFile(Path.Combine(i18nDir, $"{locale}.json"));

                // Try language-only fallback (e.g. "zh" for "zh-CN")
                int dash = locale.IndexOf('-');
                if (dash > 0) {
                    LoadFile(Path.Combine(i18nDir, $"{locale[..dash]}.json"));
                }
            }

            _monitor.Log($"Loaded {_strings.Count} translation strings for locale '{locale}'", LogLevel.Debug);
        }

        private void LoadFile(string path) {
            if (!File.Exists(path)) return;
            try {
                var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
                if (d == null) return;
                foreach (var kvp in d)
                    _strings[kvp.Key] = kvp.Value;
            } catch (Exception ex) {
                _monitor.Log($"Failed to parse i18n file '{path}': {ex.Message}", LogLevel.Error);
            }
        }

        private static string GetGameLocale() {
            // Read from Game1.content.RootDirectory or Localizer — reflect to avoid hard dep
            try {
                var t = Type.GetType("StardewValley.LocalizedContentManager, Stardew Valley");
                if (t != null) {
                    var p = t.GetProperty("CurrentLanguageCode",
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public);
                    if (p != null) {
                        var code = p.GetValue(null)?.ToString();
                        if (!string.IsNullOrEmpty(code)) return code!;
                    }
                }
            } catch { /* fallback */ }
            return "en";
        }

        // ── Translation implementation ─────────────────────────────────────────

        private sealed class TranslationImpl : Translation {
            private static readonly Regex TokenPattern = new Regex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

            public string Key { get; }
            private readonly string  _raw;
            private readonly object? _tokens;

            internal TranslationImpl(string key, string raw, object? tokens) {
                Key     = key;
                _raw    = raw;
                _tokens = tokens;
            }

            public Translation Tokens(object? tokens) => new TranslationImpl(Key, _raw, tokens);

            public override string ToString() {
                if (_tokens == null) return _raw;
                return TokenPattern.Replace(_raw, m => {
                    string name  = m.Groups[1].Value.Trim();
                    var    props = _tokens.GetType().GetProperties();
                    foreach (var p in props) {
                        if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                            return p.GetValue(_tokens)?.ToString() ?? string.Empty;
                    }
                    return m.Value; // token not found — leave as-is
                });
            }

            public static implicit operator string(TranslationImpl t) => t.ToString();
        }
    }
}
