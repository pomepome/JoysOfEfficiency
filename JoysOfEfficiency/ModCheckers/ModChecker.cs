using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency.ModCheckers
{
    public class ModChecker
    {
        public static bool IsCJBCheatsLoaded(IModHelper helper)
        {
            return helper.ModRegistry.IsLoaded("CJBok.CheatsMenu");
        }
        public static bool IsCasksAnywhereLoaded(IModHelper helper)
        {
            return helper.ModRegistry.IsLoaded("CasksAnywhere");
        }
    }
}
