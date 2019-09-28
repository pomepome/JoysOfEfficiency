using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using JoysOfEfficiency.Utils;
using StardewModdingAPI;
using StardewValley;

namespace JoysOfEfficiency.Patches
{
    using Player = Farmer;
    internal class HarmonyPatcher
    {
        public static bool Init()
        {
            IMonitor Mon = Util.Monitor;
            try
            {
                MethodInfo methodBase;
                MethodInfo methodPatcher;
                {
                    Mon.Log("Started patching Farmer");
                    methodBase = typeof(Player).GetMethod("hasItemInInventory", BindingFlags.Instance | BindingFlags.Public);
                    methodPatcher = typeof(FarmerPatcher).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                    Mon.Log("Trying to patch...");
                    if (!HarmonyHelper.Patch(methodBase, methodPatcher))
                    {
                        return false;
                    }
                }
                {
                    Mon.Log("Started patching CraftingRecipe");
                    methodBase = typeof(CraftingRecipe).GetMethod("consumeIngredients", BindingFlags.Instance | BindingFlags.Public);
                    methodPatcher = typeof(CraftingRecipePatcher).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                    Mon.Log("Trying to patch...");
                    if (!HarmonyHelper.Patch(methodBase, methodPatcher))
                    {
                        return false;
                    }
                }
            }
            catch(Exception e)
            {
                Util.Monitor.Log(e.ToString(), LogLevel.Error);
                return false;
            }
            return true;
        }
    }
}
