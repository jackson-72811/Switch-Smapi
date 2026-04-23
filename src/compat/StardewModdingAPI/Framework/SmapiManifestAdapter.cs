using System.Collections.Generic;
using System.Linq;
using InternalManifest   = SwitchSMAPI.Framework.IManifest;
using InternalSemVer     = SwitchSMAPI.Framework.ISemanticVersion;
using InternalDependency = SwitchSMAPI.Framework.IManifestDependency;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// Wraps <see cref="InternalManifest"/> as <see cref="IManifest"/> so the compat layer
    /// can pass manifests loaded by the internal engine to PC-mod helpers without a type mismatch.
    /// </summary>
    internal sealed class SmapiManifestAdapter : IManifest {

        private readonly InternalManifest _inner;

        public SmapiManifestAdapter(InternalManifest inner) => _inner = inner;

        public string            Name              => _inner.Name;
        public string            Author            => _inner.Author;
        public ISemanticVersion  Version           => new SmapiSemanticVersionAdapter(_inner.Version);
        public string            Description       => _inner.Description;
        public string            UniqueID          => _inner.UniqueID;
        public string            EntryDll          => _inner.EntryDll;
        public ISemanticVersion? MinimumApiVersion =>
            _inner.MinimumApiVersion != null
                ? new SmapiSemanticVersionAdapter(_inner.MinimumApiVersion)
                : null;
        public IManifestDependency[] Dependencies  =>
            _inner.Dependencies.Select(d => (IManifestDependency)new DependencyAdapter(d)).ToArray();
        public IDictionary<string, object?> ExtraFields => _inner.ExtraFields;

        // ── Dependency adapter ────────────────────────────────────────────────

        private sealed class DependencyAdapter : IManifestDependency {
            private readonly InternalDependency _d;
            public DependencyAdapter(InternalDependency d) => _d = d;
            public string            UniqueID       => _d.UniqueID;
            public ISemanticVersion? MinimumVersion =>
                _d.MinimumVersion != null ? new SmapiSemanticVersionAdapter(_d.MinimumVersion) : null;
            public bool IsRequired => _d.IsRequired;
        }
    }

    /// <summary>
    /// Wraps <see cref="InternalSemVer"/> as <see cref="ISemanticVersion"/>
    /// for use by the compat manifest adapter.
    /// </summary>
    internal sealed class SmapiSemanticVersionAdapter : ISemanticVersion {

        private readonly InternalSemVer _inner;

        public SmapiSemanticVersionAdapter(InternalSemVer inner) => _inner = inner;

        public int     MajorVersion  => _inner.MajorVersion;
        public int     MinorVersion  => _inner.MinorVersion;
        public int     PatchVersion  => _inner.PatchVersion;
        public string? PrereleaseTag => _inner.PrereleaseTag;
        public string? BuildMetadata => _inner.BuildMetadata;

        public bool IsOlderThan(ISemanticVersion? other) {
            if (other == null) return false;
            return CompareTo(other) < 0;
        }

        public bool IsNewerThan(ISemanticVersion? other) {
            if (other == null) return true;
            return CompareTo(other) > 0;
        }

        public bool IsBetween(ISemanticVersion? min, ISemanticVersion? max)
            => (min == null || !IsOlderThan(min)) && (max == null || !IsNewerThan(max));

        public bool IsPrerelease() => !string.IsNullOrEmpty(PrereleaseTag);

        public int CompareTo(ISemanticVersion? other) {
            if (other == null) return 1;
            int c = MajorVersion.CompareTo(other.MajorVersion);
            if (c != 0) return c;
            c = MinorVersion.CompareTo(other.MinorVersion);
            if (c != 0) return c;
            c = PatchVersion.CompareTo(other.PatchVersion);
            if (c != 0) return c;
            bool thisPre  = IsPrerelease();
            bool otherPre = other.IsPrerelease();
            if (thisPre && !otherPre) return -1;
            if (!thisPre && otherPre) return 1;
            return string.Compare(PrereleaseTag, other.PrereleaseTag,
                System.StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => _inner.ToString() ?? $"{MajorVersion}.{MinorVersion}.{PatchVersion}";
    }
}
