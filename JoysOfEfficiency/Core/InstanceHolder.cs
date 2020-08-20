using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;

namespace JoysOfEfficiency.Core
{
    /// <summary>
    /// This class holds mod and config instance and exposes some useful methods.
    /// </summary>
    internal class InstanceHolder
    {
        private static ModEntry ModInstance { get; set; }

        public static Config Config { get; private set; }
        private static IModHelper Helper => ModInstance.Helper;
        public static ITranslationHelper Translation => Helper.Translation;
        public static IReflectionHelper Reflection => Helper.Reflection;
        public static Multiplayer Multiplayer => Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

        public static InputState Input => Reflection.GetField<InputState>(typeof(Game1), "input").GetValue();

        public static CustomAnimalConfigHolder CustomAnimalTool;

        /// <summary>
        /// Sets mod's entry　point and configuration instance. 
        /// </summary>
        /// <param name="modInstance">the mod instance</param>
        /// <param name="conf">the configuration instance</param>
        public static void Init(ModEntry modInstance, Config conf)
        {
            ModInstance = modInstance;
            Config = conf;
            CustomAnimalTool = new CustomAnimalConfigHolder(modInstance.GetFilePath("customAnimalTools.json"));
        }
        
        /// <summary>
        /// Writes settings to '(ModFolder)/config.json'.
        /// </summary>

        public static void WriteConfig()
        {
            Helper?.WriteConfig(Config);
        }

        public static Config LoadConfig()
        {
            return Config = Helper.ReadConfig<Config>();
        }
    }
}
