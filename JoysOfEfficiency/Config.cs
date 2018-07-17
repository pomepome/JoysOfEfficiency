using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace JoysOfEfficiency
{
    internal class Config
    {
        public bool MineInfoGui { get; set; } = true;
        public bool AutoWaterNearbyCrops { get; set; } = true;
        public int AutoWaterRadius { get; set; } = 1;
        public bool FindCanFromInventory { get; set; } = true;
        public bool FindHoeFromInventory { get; set; } = true;
        public bool GiftInformation { get; set; } = true;
        public bool AutoPetNearbyAnimals { get; set; } = false;
        public int AutoPetRadius { get; set; } = 1;
        public bool AutoAnimalDoor { get; set; } = true;
        public bool AutoFishing { get; set; } = false;
        public bool AutoReelRod { get; set; } = true;
        public bool MuchFasterBiting { get; set; } = false;
        public bool FishingInfo { get; set; } = true;
        public float CpuThresholdFishing { get; set; } = 0.2f;
        public bool AutoGate { get; set; } = true;
        public bool AutoEat { get; set; } = false;
        public float StaminaToEatRatio { get; set; } = 0.2f;
        public float HealthToEatRatio { get; set; } = 0.2f;
        public bool AutoHarvest { get; set; } = true;
        public int AutoHarvestRadius { get; set; } = 1;
        public bool AutoDestroyDeadCrops { get; set; } = true;
        public bool AutoRefillWateringCan { get; set; } = true;
        public Keys KeyShowMenu { get; set; } = Keys.R;
        public bool AutoCollectCollectibles { get; set; } = false;
        public int AutoCollectRadius { get; set; } = 1;
        public bool AutoShakeFruitedPlants { get; set; } = true;
        public int AutoShakeRadius { get; set; } = 1;
        public bool AutoDigArtifactSpot { get; set; } = false;
        public int AutoDigRadius { get; set; } = 1;
        public bool BalancedMode { get; set; } = true;
        public bool AutoDepositIngredient { get; set; } = false;
        public bool AutoPullMachineResult { get; set; } = true;
        public int MachineRadius { get; set; } = 1;
        public bool AutoPetNearbyPets { get; set; } = false;
        public bool ProtectNectarProducingFlower { get; set; } = true;
        public List<int> HarvestException { get; set; } = new List<int>();
        public Keys KeyToggleBlackList { get; set; } = Keys.F2;
        public bool FishingProbabilitiesInfo { get; set; } = false;
        public int ProbBoxX { get; set; } = 100;
        public int ProbBoxY { get; set; } = 500;
        public int RadiusCraftingFromChests { get; set; } = 3;
        public bool CraftingFromChests { get; set; } = false;
    }
}
