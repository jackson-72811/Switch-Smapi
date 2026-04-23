using System.Collections.Generic;
using System.Linq;
using InternalHelper = SwitchSMAPI.Framework.Translation.TranslationHelper;

namespace StardewModdingAPI.Framework {

    /// <summary>Bridges <see cref="ITranslationHelper"/> to the internal translation system.</summary>
    internal sealed class SmapiTranslationHelper : ITranslationHelper {

        private readonly InternalHelper _inner;

        public string Locale => _inner.Locale;

        public SmapiTranslationHelper(InternalHelper inner) => _inner = inner;

        public Translation Get(string key) {
            var t = _inner.Get(key);
            return new Translation(key, t.ToString());
        }

        public Translation Get(string key, object? tokens) {
            var t = _inner.Get(key, tokens);
            return new Translation(key, t.ToString());
        }

        public IEnumerable<Translation> GetTranslations()
            => _inner.GetTranslations().Select(k => Get(k));
    }
}
