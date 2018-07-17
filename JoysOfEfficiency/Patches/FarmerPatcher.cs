using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoysOfEfficiency.Utils;
using StardewValley;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace JoysOfEfficiency.Patches
{
    using Player = StardewValley.Farmer;
    internal class FarmerPatcher
    {
        private static bool Prefix(Player __instance, ref bool __result, ref int itemIndex, ref int quantity, ref int minPrice)
        {
            int count = 0;
            foreach (Item item in Util.GetNearbyItems(__instance))
            {
                if(item is Object obj && (!(obj is Furniture) && item.ParentSheetIndex == itemIndex || item.Category == itemIndex))
                    count += obj.Stack;
            }

            __result = count >= quantity;
            return false;
        }
    }
}
