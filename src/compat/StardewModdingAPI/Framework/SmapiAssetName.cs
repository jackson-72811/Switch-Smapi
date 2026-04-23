using System;

namespace StardewModdingAPI.Framework {

    /// <summary>Concrete <see cref="IAssetName"/> implementation used by the content helpers.</summary>
    internal sealed class SmapiAssetName : IAssetName {

        public string  Name       { get; }
        public string? LocaleCode { get; }

        public SmapiAssetName(string name, string? localeCode = null) {
            Name       = Normalise(name);
            LocaleCode = localeCode;
        }

        public bool IsEquivalentTo(string assetName, bool useBaseName = false) {
            string other = Normalise(assetName);
            return string.Equals(useBaseName ? StripLocale(Name) : Name,
                                 useBaseName ? StripLocale(other) : other,
                                 StringComparison.OrdinalIgnoreCase);
        }

        public bool StartsWith(string prefix, bool allowPartialWord = true, bool allowSubfolder = true) {
            string normPrefix = Normalise(prefix);
            if (!Name.StartsWith(normPrefix, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!allowPartialWord && Name.Length > normPrefix.Length) {
                char next = Name[normPrefix.Length];
                if (next != '/' && next != '\\' && next != '.')
                    return false;
            }
            return true;
        }

        public override string ToString() => Name;

        private static string Normalise(string name) =>
            name?.Replace('\\', '/').Trim('/') ?? string.Empty;

        private static string StripLocale(string name) {
            int dot = name.LastIndexOf('.');
            return dot > 0 ? name[..dot] : name;
        }
    }
}
