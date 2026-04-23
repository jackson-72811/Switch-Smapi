using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwitchSMAPI.Framework {

    /// <summary>Concrete implementation of <see cref="IManifest"/> parsed from manifest.json.</summary>
    public class ModManifest : IManifest {

        // ── JSON-bound fields ─────────────────────────────────────────────────

        [JsonProperty("Name",          Required = Required.Always)]
        public string Name { get; private set; } = string.Empty;

        [JsonProperty("Author",        Required = Required.Always)]
        public string Author { get; private set; } = string.Empty;

        [JsonProperty("Version",       Required = Required.Always)]
        public string VersionString { get; private set; } = string.Empty;

        [JsonProperty("Description",   Required = Required.Always)]
        public string Description { get; private set; } = string.Empty;

        [JsonProperty("UniqueID",      Required = Required.Always)]
        public string UniqueID { get; private set; } = string.Empty;

        [JsonProperty("EntryDll",      Required = Required.Always)]
        public string EntryDll { get; private set; } = string.Empty;

        [JsonProperty("MinimumApiVersion")]
        public string? MinimumApiVersionString { get; private set; }

        [JsonProperty("Dependencies")]
        public ManifestDependency[]? DependenciesRaw { get; private set; }

        // ── IManifest ─────────────────────────────────────────────────────────

        [JsonIgnore]
        public ISemanticVersion Version { get; private set; } = null!;

        [JsonIgnore]
        public ISemanticVersion? MinimumApiVersion { get; private set; }

        [JsonIgnore]
        public IManifestDependency[] Dependencies { get; private set; }
            = Array.Empty<IManifestDependency>();

        [JsonIgnore]
        public IDictionary<string, object?> ExtraFields { get; private set; }
            = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // ── Post-parse setup ──────────────────────────────────────────────────

        internal void Resolve() {
            Version = SemanticVersion.Parse(VersionString);

            if (!string.IsNullOrWhiteSpace(MinimumApiVersionString))
                MinimumApiVersion = SemanticVersion.Parse(MinimumApiVersionString);

            Dependencies = DependenciesRaw as IManifestDependency[]
                        ?? Array.Empty<IManifestDependency>();
        }

        // ── Factory ───────────────────────────────────────────────────────────

        public static ModManifest Load(string jsonPath) {
            string json = System.IO.File.ReadAllText(jsonPath);
            var manifest = JsonConvert.DeserializeObject<ModManifest>(json)
                ?? throw new InvalidOperationException($"manifest.json at {jsonPath} is empty.");

            // Capture extra/unknown fields
            var raw = JObject.Parse(json);
            string[] known = {
                "name","author","version","description","uniqueid",
                "entrydll","minimumapiversion","dependencies","updatekeys"
            };
            foreach (var prop in raw.Properties()) {
                if (Array.FindIndex(known, k => k.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)) < 0)
                    manifest.ExtraFields[prop.Name] = prop.Value.ToObject<object>();
            }

            manifest.Resolve();
            return manifest;
        }
    }

    /// <summary>A dependency entry parsed from manifest.json.</summary>
    public class ManifestDependency : IManifestDependency {
        [JsonProperty("UniqueID",       Required = Required.Always)]
        public string UniqueID { get; private set; } = string.Empty;

        [JsonProperty("MinimumVersion")]
        public string? MinimumVersionString { get; private set; }

        [JsonProperty("IsRequired")]
        public bool IsRequired { get; private set; } = true;

        [JsonIgnore]
        public ISemanticVersion? MinimumVersion { get; private set; }

        internal void Resolve() {
            if (!string.IsNullOrWhiteSpace(MinimumVersionString))
                MinimumVersion = SemanticVersion.Parse(MinimumVersionString);
        }

        [JsonConstructor]
        public ManifestDependency() { }

        // Called after JSON deserialization
        [System.Runtime.Serialization.OnDeserialized]
        internal void OnDeserialized(System.Runtime.Serialization.StreamingContext _) => Resolve();
    }
}
