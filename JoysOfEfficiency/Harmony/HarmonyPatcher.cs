using Harmony;

namespace JoysOfEfficiency.Harmony
{
    internal class HarmonyPatcher
    {
        private static readonly HarmonyInstance Harmony = HarmonyInstance.Create("com.pome.joe");

        public static void DoPatching()
        {
            Harmony.PatchAll();
        }

    }
}
