using System;
using System.Text.RegularExpressions;

namespace SwitchSMAPI.Framework {

    /// <summary>A parsed semantic version (SemVer 2.0).</summary>
    public class SemanticVersion : ISemanticVersion {

        // ── Parsing ───────────────────────────────────────────────────────────

        private static readonly Regex Pattern = new Regex(
            @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)" +
            @"(?:-(?<pre>[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?" +
            @"(?:\+(?<build>[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // ── Properties ────────────────────────────────────────────────────────

        public int     MajorVersion  { get; }
        public int     MinorVersion  { get; }
        public int     PatchVersion  { get; }
        public string? PrereleaseTag { get; }
        public string? BuildMetadata { get; }

        // ── Construction ──────────────────────────────────────────────────────

        public SemanticVersion(int major, int minor, int patch,
                               string? prerelease = null, string? build = null) {
            MajorVersion  = major;
            MinorVersion  = minor;
            PatchVersion  = patch;
            PrereleaseTag = prerelease;
            BuildMetadata = build;
        }

        public static SemanticVersion Parse(string version) {
            if (version == null) throw new ArgumentNullException(nameof(version));
            var m = Pattern.Match(version.Trim());
            if (!m.Success)
                throw new FormatException($"Invalid semantic version: '{version}'");

            return new SemanticVersion(
                int.Parse(m.Groups["major"].Value),
                int.Parse(m.Groups["minor"].Value),
                int.Parse(m.Groups["patch"].Value),
                m.Groups["pre"].Success   ? m.Groups["pre"].Value   : null,
                m.Groups["build"].Success ? m.Groups["build"].Value : null);
        }

        public static bool TryParse(string? version, out SemanticVersion? result) {
            result = null;
            if (version == null) return false;
            try { result = Parse(version); return true; }
            catch { return false; }
        }

        // ── ISemanticVersion ──────────────────────────────────────────────────

        public bool IsPrerelease() => PrereleaseTag != null;

        public bool IsOlderThan(ISemanticVersion? other) =>
            other != null && CompareTo(other) < 0;

        public bool IsNewerThan(ISemanticVersion? other) =>
            other != null && CompareTo(other) > 0;

        public bool IsBetween(ISemanticVersion? min, ISemanticVersion? max) =>
            (min == null || !IsOlderThan(min)) &&
            (max == null || !IsNewerThan(max));

        public int CompareTo(ISemanticVersion? other) {
            if (other == null) return 1;

            int cmp = MajorVersion.CompareTo(other.MajorVersion);
            if (cmp != 0) return cmp;
            cmp = MinorVersion.CompareTo(other.MinorVersion);
            if (cmp != 0) return cmp;
            cmp = PatchVersion.CompareTo(other.PatchVersion);
            if (cmp != 0) return cmp;

            // No prerelease > prerelease (stable > rc)
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

        public override bool Equals(object? obj) =>
            obj is ISemanticVersion other && CompareTo(other) == 0;

        public override int GetHashCode() =>
            HashCode.Combine(MajorVersion, MinorVersion, PatchVersion, PrereleaseTag);
    }
}
