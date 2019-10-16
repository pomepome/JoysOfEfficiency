using StardewModdingAPI;

namespace JoysOfEfficiency.Core
{
    internal class InstanceHolder
    {
        public static ModEntry ModInstance { get; private set; }

        public static Config Config { get; private set; }
        public static IMonitor Monitor => ModInstance.Monitor;
        private static IModHelper Helper => ModInstance.Helper;
        public static ITranslationHelper Translation => Helper.Translation;
        public static IReflectionHelper Reflection => Helper.Reflection;
        public static void Init(ModEntry modInstance, Config conf)
        {
            ModInstance = modInstance;
            Config = conf;
        }
    }
}
