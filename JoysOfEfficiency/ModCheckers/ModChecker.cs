using StardewModdingAPI;

namespace JoysOfEfficiency.ModCheckers
{
    public class ModChecker
    {
        public static bool IsCjbCheatsLoaded(IModHelper helper) => helper.ModRegistry.IsLoaded("CJBok.CheatsMenu");
        public static bool IsCoGLoaded(IModHelper helper) => helper.ModRegistry.IsLoaded("punyo.CasksOnGround");
    }
}
