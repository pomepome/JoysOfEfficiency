using System;
using System.Collections.Generic;
using JoysOfEfficiency.Utils;
using StardewValley;
using StardewValley.Objects;

namespace JoysOfEfficiency.Patches
{
    internal class CraftingRecipePatcher
    {
        internal static bool Prefix(ref CraftingRecipe __instance)
        {
            Dictionary<int, int> recipeList = Util.Helper.Reflection.GetField<Dictionary<int, int>>(__instance, "recipeList")
                .GetValue();
            foreach (KeyValuePair<int, int> kv in recipeList)
            {
                int index = kv.Key;
                int count = kv.Value;
                int toConsume;
                foreach (Item playerItem in new List<Item>(Game1.player.Items))
                {
                    if (playerItem != null && (playerItem.parentSheetIndex == index || playerItem.category == index))
                    {
                        toConsume = Math.Min(playerItem.Stack, count);
                        playerItem.Stack -= toConsume;
                        count -= toConsume;
                        if (playerItem.Stack == 0)
                        {
                            Game1.player.removeItemFromInventory(playerItem);
                        }
                    }
                }

                if (ModEntry.Conf.CraftingFromChests)
                {
                    List<Chest> chests = Util.GetObjectsWithin<Chest>(ModEntry.Conf.RadiusCraftingFromChests);
                    Chest fridge = Util.GetFridge();
                    if (fridge != null)
                    {
                        chests.Add(fridge);
                    }

                    foreach (Chest chest in chests)
                    {
                        foreach (Item chestItem in new List<Item>(chest.items))
                        {
                            if (chestItem != null &&
                                (chestItem.parentSheetIndex == index || chestItem.category == index))
                            {
                                toConsume = Math.Min(chestItem.Stack, count);
                                chestItem.Stack -= toConsume;
                                count -= toConsume;
                                if (chestItem.Stack == 0)
                                {
                                    chest.items.Remove(chestItem);
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
