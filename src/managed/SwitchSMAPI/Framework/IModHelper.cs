using SwitchSMAPI.Framework.Content;
using SwitchSMAPI.Framework.Data;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Input;
using SwitchSMAPI.Framework.Multiplayer;
using SwitchSMAPI.Framework.Reflection;
using SwitchSMAPI.Framework.Translation;

namespace SwitchSMAPI.Framework {

    /// <summary>
    /// Provides access to all Switch-SMAPI APIs for a specific mod.
    /// Obtained from <c>Mod.Helper</c> or the <c>Entry(IModHelper helper)</c> parameter.
    /// </summary>
    public interface IModHelper {

        /// <summary>Absolute filesystem path to this mod's folder on the SD card.</summary>
        string DirectoryPath { get; }

        /// <summary>The mod's parsed manifest.</summary>
        IManifest ModManifest { get; }

        /// <summary>Provides access to all event categories.</summary>
        IModEvents Events { get; }

        /// <summary>Loads and edits game content assets.</summary>
        IContentHelper Content { get; }

        /// <summary>Reads and writes per-mod data files on the SD card.</summary>
        IDataHelper Data { get; }

        /// <summary>Provides access to private game fields, properties, and methods.</summary>
        IReflectionHelper Reflection { get; }

        /// <summary>Provides localised translation strings.</summary>
        ITranslationHelper Translation { get; }

        /// <summary>Provides the state of player input (buttons, cursor).</summary>
        IInputHelper Input { get; }

        /// <summary>Facilitates sending and receiving messages in a multiplayer session.</summary>
        IMultiplayerHelper Multiplayer { get; }

        /// <summary>Read a JSON file from this mod's folder.</summary>
        TModel? ReadConfig<TModel>() where TModel : class, new();

        /// <summary>Write a JSON file to this mod's folder.</summary>
        void WriteConfig<TModel>(TModel config) where TModel : class, new();
    }
}
