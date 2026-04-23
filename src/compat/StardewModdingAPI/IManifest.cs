using System.Collections.Generic;

namespace StardewModdingAPI {
    public interface IManifest {
        string            Name              { get; }
        string            Author            { get; }
        ISemanticVersion  Version           { get; }
        string            Description       { get; }
        string            UniqueID          { get; }
        string            EntryDll          { get; }
        ISemanticVersion? MinimumApiVersion { get; }
        IManifestDependency[] Dependencies  { get; }
        IDictionary<string, object?> ExtraFields { get; }
    }

    public interface IManifestDependency {
        string            UniqueID        { get; }
        ISemanticVersion? MinimumVersion  { get; }
        bool              IsRequired      { get; }
    }
}
