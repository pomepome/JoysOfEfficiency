using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace JoysOfEfficiency.Core
{
    internal class InstanceHolder
    {
        public static IMod ModInstance { get; private set; }

        public static Config Config { get; private set; }
        public static IModHelper Helper => ModInstance.Helper;
        public static ITranslationHelper Translation => Helper.Translation;

        public static void Init(IMod modInstance, Config conf)
        {
            ModInstance = modInstance;
            Config = conf;
        }
    }
}
