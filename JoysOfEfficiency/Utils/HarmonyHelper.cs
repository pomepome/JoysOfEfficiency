using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using JoysOfEfficiency.Core;
using StardewModdingAPI;

namespace JoysOfEfficiency.Utils
{
    public class HarmonyHelper
    {
        private const string uniqueId = "punyo.JOE";
        private  static readonly HarmonyInstance harmony = HarmonyInstance.Create(uniqueId);

        public static bool Patch(MethodInfo methodObjective, MethodInfo methodPatcher)
        {
            try
            {
                if (methodObjective == null)
                {
                    Util.Monitor.Log("Object method is null.", LogLevel.Error);
                    return false;
                }

                if (methodPatcher == null)
                {
                    Util.Monitor.Log("Patcher method is null.", LogLevel.Error);
                    return false;
                }

                harmony.Patch(methodObjective, new HarmonyMethod(methodPatcher), null);
                Util.Monitor.Log($"Method:{GetMethodString(methodObjective)} has been patched by {GetMethodString(methodPatcher)}");

                return true;
            }
            catch (Exception e)
            {
                Util.Monitor.Log($"An Exception Occured: {e}", LogLevel.Error);
                return false;
            }
        }

        private static string GetMethodString(MethodBase method)
        {
            return $"{method.DeclaringType?.FullName}::{method.Name}";
        }
    }
}
