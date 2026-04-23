namespace SwitchSMAPI.Framework.Translation {

    /// <summary>A translated string that can have tokens substituted into it.</summary>
    public interface Translation {
        /// <summary>The translation key.</summary>
        string Key { get; }

        /// <summary>
        /// Substitute anonymous-object tokens into the string.
        /// e.g. <c>translation.Tokens(new { name = "Alex" })</c>
        /// replaces <c>{{name}}</c> with <c>"Alex"</c>.
        /// </summary>
        Translation Tokens(object? tokens);

        /// <summary>The translated text with all tokens substituted.</summary>
        string ToString();

        /// <summary>Implicit conversion so you can use a Translation directly as a string.</summary>
        static implicit operator string(Translation t) => t.ToString();
    }

    /// <summary>Provides localised strings for a mod.</summary>
    public interface ITranslationHelper {
        /// <summary>The locale currently active in the game (e.g. "en", "de", "zh-CN").</summary>
        string Locale { get; }

        /// <summary>Get a translation by key.</summary>
        Translation Get(string key);

        /// <summary>Get a translation by key with tokens.</summary>
        Translation Get(string key, object? tokens);

        /// <summary>Get all translation keys available for the current locale.</summary>
        System.Collections.Generic.IEnumerable<string> GetTranslations();
    }
}
