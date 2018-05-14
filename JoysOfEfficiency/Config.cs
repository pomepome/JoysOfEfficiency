using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
namespace JoysOfEfficiency
{
    public class Config
    {
        public bool HowManyStonesLeft{ get; set; } = true;
        public SButton KeyShowStonesLeft { get; set; } = SButton.H;
        public bool AutoWaterNearbyCrops { get; set; } = true;
        public bool GiftInformation { get; set; } = true;
        public bool AutoPetNearbyAnimals { get; set; } = true;
        public bool AutoAnimalDoor { get; set; } = true;
        public bool AutoFishing { get; set; } = false;
        public bool AutoReelRod { get; set; } = true;
        public bool MuchFasterBiting { get; set; } = false;
        public bool FishingInfo { get; set; } = true;
    }
}
