using System.Collections.Generic;

namespace StardewModdingAPI {
    public interface ITranslationHelper {
        string Locale { get; }
        Translation Get(string key);
        Translation Get(string key, object? tokens);
        IEnumerable<Translation> GetTranslations();
    }

    public class Translation {
        public string Key  { get; }
        private string _text;

        public Translation(string key, string text) { Key = key; _text = text; }

        public Translation Tokens(object? tokens) {
            if (tokens == null) return this;
            string result = _text;
            foreach (var prop in tokens.GetType().GetProperties()) {
                result = result.Replace("{{" + prop.Name + "}}", prop.GetValue(tokens)?.ToString() ?? "");
                result = result.Replace("{{" + prop.Name.ToLower() + "}}", prop.GetValue(tokens)?.ToString() ?? "");
            }
            return new Translation(Key, result);
        }

        public bool HasValue() => !string.IsNullOrEmpty(_text) && !_text.StartsWith("(no translation:");

        public override string ToString() => _text;
        public static implicit operator string(Translation t) => t.ToString();
        public static implicit operator bool(Translation t)   => t.HasValue();
    }
}
