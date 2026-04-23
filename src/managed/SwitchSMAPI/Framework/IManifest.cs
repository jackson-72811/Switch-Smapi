using System.Collections.Generic;

namespace SwitchSMAPI.Framework {

    /// <summary>A mod's parsed manifest.json.</summary>
    public interface IManifest {
        /// <summary>Human-readable mod name.</summary>
        string Name { get; }

        /// <summary>Mod author.</summary>
        string Author { get; }

        /// <summary>Mod version.</summary>
        ISemanticVersion Version { get; }

        /// <summary>Short description.</summary>
        string Description { get; }

        /// <summary>
        /// Globally unique identifier in the form "Author.ModName".
        /// Used for dependency resolution and per-mod data storage.
        /// </summary>
        string UniqueID { get; }

        /// <summary>Filename of the mod's entry DLL inside the mod folder.</summary>
        string EntryDll { get; }

        /// <summary>Minimum Switch-SMAPI version required to run this mod.</summary>
        ISemanticVersion? MinimumApiVersion { get; }

        /// <summary>Other mods this mod depends on.</summary>
        IManifestDependency[] Dependencies { get; }

        /// <summary>Extra custom fields not parsed by SMAPI.</summary>
        IDictionary<string, object?> ExtraFields { get; }
    }

    /// <summary>A dependency entry in a mod manifest.</summary>
    public interface IManifestDependency {
        /// <summary>The unique ID of the required mod.</summary>
        string UniqueID { get; }

        /// <summary>Minimum required version of the dependency, or null for any version.</summary>
        ISemanticVersion? MinimumVersion { get; }

        /// <summary>If false, SMAPI will log a warning but still load this mod if the dependency is absent.</summary>
        bool IsRequired { get; }
    }

    /// <summary>A semantic version conforming to SemVer 2.0.</summary>
    public interface ISemanticVersion : IComparable<ISemanticVersion> {
        int    MajorVersion { get; }
        int    MinorVersion { get; }
        int    PatchVersion { get; }
        string? PrereleaseTag { get; }
        string? BuildMetadata { get; }

        bool IsOlderThan(ISemanticVersion? other);
        bool IsNewerThan(ISemanticVersion? other);
        bool IsBetween(ISemanticVersion? min, ISemanticVersion? max);

        bool IsPrerelease();
        string ToString();
    }
}
