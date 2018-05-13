using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
namespace JoysOfEfficiency
{
    public class Config
    {
        public bool EasierFishing { get; set; } = true;
        public bool HowManyStonesLeft{ get; set; } = true;
        public SButton KeyShowStonesLeft { get; set; } = SButton.H;
        public bool AutoWaterNearbyCrops { get; set; } = true;
        public bool GiftInformation { get; set; } = true;
        public bool AutoPetNearbyAnimals { get; set; } = true;
        public bool AutoAnimalDoor { get; set; } = true;
    }
}
