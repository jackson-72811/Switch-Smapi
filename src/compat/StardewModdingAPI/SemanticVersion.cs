using System;
using System.Text.RegularExpressions;

namespace StardewModdingAPI {
    /// <summary>Desktop SMAPI-compatible SemanticVersion.</summary>
    public class SemanticVersion : ISemanticVersion {

        private static readonly Regex _pattern = new Regex(
            @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)" +
            @"(?:-(?<pre>[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?(?:\+(?<build>[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public int     MajorVersion  { get; }
        public int     MinorVersion  { get; }
        public int     PatchVersion  { get; }
        public string? PrereleaseTag { get; }
        public string? BuildMetadata { get; }

        public SemanticVersion(string version) {
            var m = _pattern.Match(version?.Trim() ?? "");
            if (!m.Success) throw new FormatException($"Invalid semantic version: '{version}'");
            MajorVersion  = int.Parse(m.Groups["major"].Value);
            MinorVersion  = int.Parse(m.Groups["minor"].Value);
            PatchVersion  = int.Parse(m.Groups["patch"].Value);
            PrereleaseTag = m.Groups["pre"].Success   ? m.Groups["pre"].Value   : null;
            BuildMetadata = m.Groups["build"].Success ? m.Groups["build"].Value : null;
        }

        public SemanticVersion(int major, int minor, int patch,
                               string? prerelease = null, string? build = null) {
            MajorVersion  = major;
            MinorVersion  = minor;
            PatchVersion  = patch;
            PrereleaseTag = prerelease;
            BuildMetadata = build;
        }

        public bool IsPrerelease() => PrereleaseTag != null;

        public bool IsOlderThan(ISemanticVersion? other) => other != null && CompareTo(other) < 0;
        public bool IsNewerThan(ISemanticVersion? other) => other != null && CompareTo(other) > 0;
        public bool IsBetween(ISemanticVersion? min, ISemanticVersion? max) =>
            (min == null || !IsOlderThan(min)) && (max == null || !IsNewerThan(max));

        public int CompareTo(ISemanticVersion? other) {
            if (other == null) return 1;
            int c = MajorVersion.CompareTo(other.MajorVersion); if (c != 0) return c;
            c = MinorVersion.CompareTo(other.MinorVersion);     if (c != 0) return c;
            c = PatchVersion.CompareTo(other.PatchVersion);     if (c != 0) return c;
            if (PrereleaseTag == null && other.PrereleaseTag != null) return 1;
            if (PrereleaseTag != null && other.PrereleaseTag == null) return -1;
            if (PrereleaseTag != null && other.PrereleaseTag != null)
                return string.CompareOrdinal(PrereleaseTag, other.PrereleaseTag);
            return 0;
        }

        public override string ToString() {
            string v = $"{MajorVersion}.{MinorVersion}.{PatchVersion}";
            if (PrereleaseTag != null) v += $"-{PrereleaseTag}";
            if (BuildMetadata != null) v += $"+{BuildMetadata}";
            return v;
        }

        public override bool Equals(object? obj) => obj is ISemanticVersion o && CompareTo(o) == 0;
        public override int  GetHashCode() => HashCode.Combine(MajorVersion, MinorVersion, PatchVersion, PrereleaseTag);

        public static bool TryParse(string? text, out SemanticVersion? result) {
            result = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            try { result = new SemanticVersion(text!); return true; }
            catch { return false; }
        }
    }
}
