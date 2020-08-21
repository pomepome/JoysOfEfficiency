using JoysOfEfficiency.Core;
using JoysOfEfficiency.Harmony;
using JoysOfEfficiency.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace JoysOfEfficiency.Automation
{
    internal class AutoFisher
    {
        private static Config Config => InstanceHolder.Config;

        private static readonly Logger Logger = new Logger("AFKFisher");

        public static bool AfkMode { get; private set; }

        private static bool CatchingTreasure { get; set; }
        private static int AutoFishingCounter { get; set; }
        private static int AfkCooltimeCounter { get; set; }

        private static IReflectionHelper Reflection => InstanceHolder.Reflection;

        public static void AfkFishing()
        {
            Farmer player = Game1.player;

            if(AfkMode && player.passedOut)
            {
                AfkMode = false;
                Util.ShowHudMessageTranslated("hud.afk.passedout");
                return;
            }

            if (!AfkMode || !(player.CurrentTool is FishingRod rod) || Game1.activeClickableMenu != null)
            {
                return;
            }

            if (!rod.inUse() && !rod.castedButBobberStillInAir)
            {

                if (player.Stamina <= (player.MaxStamina * Config.ThresholdStaminaPercentage) / 100.0f)
                {
                    AfkMode = false;
                    Util.ShowHudMessageTranslated("hud.afk.tired");
                    return;
                }
                AfkCooltimeCounter++;
                if(AfkCooltimeCounter < 10)
                {
                    return;
                }
                AfkCooltimeCounter = 0;
                rod.beginUsing(player.currentLocation, 0, 0, player);
            }
            if (rod.isTimingCast && Config.SafeMode)
            {
                rod.castingPower = Config.ThrowPower;
            }
            if (rod.fishCaught && !Config.SafeMode)
            {
                HarmonyPatcher.OverrideUseButton();
            }
        }

        public static void AutoReelRod()
        {
            Farmer player = Game1.player;
            if (!(player.CurrentTool is FishingRod rod) || Game1.activeClickableMenu != null)
            {
                return;
            }
            IReflectedField<int> whichFish = Reflection.GetField<int>(rod, "whichFish");

            if (!rod.isNibbling || !rod.isFishing || whichFish.GetValue() != -1 || rod.isReeling || rod.hit ||
                rod.isTimingCast || rod.pullingOutOfWater || rod.fishCaught || rod.castedButBobberStillInAir)
            {
                return;
            }

            rod.DoFunction(player.currentLocation, 1, 1, 1, player);
        }
        public static void AutoFishing(BobberBar bar)
        {
            AutoFishingCounter = (AutoFishingCounter + 1) % 3;
            if (AutoFishingCounter > 0)
            {
                return;
            }


            IReflectedField<float> bobberSpeed = Reflection.GetField<float>(bar, "bobberBarSpeed");

            float barPos = Reflection.GetField<float>(bar, "bobberBarPos").GetValue();
            int barHeight = Reflection.GetField<int>(bar, "bobberBarHeight").GetValue();
            float fishPos = Reflection.GetField<float>(bar, "bobberPosition").GetValue();
            float treasurePos = Reflection.GetField<float>(bar, "treasurePosition").GetValue();
            float distanceFromCatching = Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();
            bool treasureCaught = Reflection.GetField<bool>(bar, "treasureCaught").GetValue();
            bool treasure = Reflection.GetField<bool>(bar, "treasure").GetValue();
            float treasureAppearTimer = Reflection.GetField<float>(bar, "treasureAppearTimer").GetValue();
            float bobberBarSpeed = bobberSpeed.GetValue();

            float top = barPos;

            if (treasure && treasureAppearTimer <= 0 && !treasureCaught)
            {
                if (!CatchingTreasure && distanceFromCatching > 0.7f)
                {
                    CatchingTreasure = true;
                }
                if (CatchingTreasure && distanceFromCatching < 0.3f)
                {
                    CatchingTreasure = false;
                }
                if (CatchingTreasure)
                {
                    fishPos = treasurePos;
                }
            }

            if (fishPos > barPos + (barHeight / 2f))
            {
                return;
            }

            float strength = (fishPos - (barPos + barHeight / 2f)) / 16f;
            float distance = fishPos - top;

            float threshold = Util.Cap(InstanceHolder.Config.CpuThresholdFishing, 0, 0.5f);
            if (distance < threshold * barHeight || distance > (1 - threshold) * barHeight)
            {
                bobberBarSpeed = strength;
            }

            bobberSpeed.SetValue(bobberBarSpeed);
        }

        public static void ToggleAfkFishing()
        {
            AfkMode = !AfkMode;
            Util.ShowHudMessageTranslated(AfkMode ? "hud.afk.on" : "hud.afk.off");
            Logger.Log($"AFK Mode is {(AfkMode ? "enabled" : "disabled")}.");
        }
    }
}
