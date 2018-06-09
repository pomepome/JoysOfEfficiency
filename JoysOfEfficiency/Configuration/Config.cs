using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework.Input;
namespace JoysOfEfficiency.Configuration
{
    internal class Config
    {
        internal static void Init( Config config )
        {
            config.CPUThresholdFishing = Util.Cap( config.CPUThresholdFishing, 0, 0.5f );
            config.healthToEatRatio = Util.Cap( config.healthToEatRatio, 0.3f, 0.8f );
            config.staminaToEatRatio = Util.Cap( config.staminaToEatRatio, 0.3f, 0.8f );
            config.autoCollectRadius = (int)Util.Cap( config.autoCollectRadius, 1, 3 );
            config.autoHarvestRadius = (int)Util.Cap( config.autoHarvestRadius, 1, 3 );
            config.autoPetRadius = (int)Util.Cap( config.autoPetRadius, 1, 3 );
            config.autoWaterRadius = (int)Util.Cap( config.autoWaterRadius, 1, 3 );
            config.autoDigRadius = (int)Util.Cap( config.autoDigRadius, 1, 3 );
            config.autoShakeRadius = (int)Util.Cap( config.autoShakeRadius, 1, 3 );
            config.addedSpeedMultiplier = (int)Util.Cap( config.addedSpeedMultiplier, 1, 19 );
            config.machineRadius = (int)Util.Cap( config.machineRadius, 1, 3 );
            config.FPSlocation = (int)Util.Cap( config.FPSlocation, 0, 3 );
        }

        internal bool mineInfoGUI = true;
        internal bool autoWaterNearbyCrops = true;
        internal int autoWaterRadius = 1;
        internal bool mustHoldCan = true;
        internal bool mustHoldHoe = true;
        internal bool giftInformation = true;
        internal bool autoPetNearbyAnimals = true;
        internal int autoPetRadius = 1;
        internal bool autoAnimalDoor = true;
        internal bool autoFishing = false;
        internal bool autoReelRod = true;
        internal bool muchFasterBiting = false;
        internal bool fishingInfo = true;
        internal float CPUThresholdFishing = 0.2f;
        internal bool autoGate = true;
        internal bool autoEat = false;
        internal float staminaToEatRatio = 0.2f;
        internal float healthToEatRatio = 0.2f;
        internal bool autoHarvest = true;
        internal int autoHarvestRadius = 1;
        internal bool autoDestroyDeadCrops = true;
        internal bool autoRefillWateringCan = true;
        internal Keys keyShowMenu = Keys.R;
        internal bool autoCollectCollectibles = true;
        internal int autoCollectRadius = 1;
        internal bool autoShakeFruitedPlants = true;
        internal int autoShakeRadius = 1;
        internal bool autoDigArtifactSpot = true;
        internal int autoDigRadius = 1;
        internal bool fastToolUpgrade = false;
        internal bool fasterRunningSpeed = false;
        internal int addedSpeedMultiplier = 1;
        internal bool autoDepositIngredient = true;
        internal bool autoPullMachineResult = true;
        internal int machineRadius = 1;
        internal bool autoPetNearbyPets = true;
        internal bool protectNectarProducingFlower = true;
        internal bool FPSCounter = true;
        internal int FPSlocation = 1;


        internal bool balancedMode = true;
        internal bool mustBeWalking = false;
        internal bool balanceReadySound = false;
    }
}
