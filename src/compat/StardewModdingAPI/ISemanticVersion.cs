using System;

namespace StardewModdingAPI {
    /// <summary>Desktop SMAPI-compatible semantic version interface.</summary>
    public interface ISemanticVersion : IComparable<ISemanticVersion> {
        int    MajorVersion  { get; }
        int    MinorVersion  { get; }
        int    PatchVersion  { get; }
        string? PrereleaseTag { get; }
        string? BuildMetadata { get; }

        bool IsOlderThan(ISemanticVersion? other);
        bool IsNewerThan(ISemanticVersion? other);
        bool IsBetween(ISemanticVersion? min, ISemanticVersion? max);
        bool IsPrerelease();
        string ToString();
    }
}
