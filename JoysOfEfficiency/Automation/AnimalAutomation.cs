using System.Collections.Generic;
using System.Linq;
using JoysOfEfficiency.Utils;
using Netcode;
using StardewValley;
using StardewValley.Buildings;

namespace JoysOfEfficiency.Automation
{
    internal class AnimalAutomation
    {
        public static void LetAnimalsInHome()
        {
            Farm farm = Game1.getFarm();
            foreach (KeyValuePair<long, FarmAnimal> kv in farm.animals.Pairs.ToArray())
            {
                FarmAnimal animal = kv.Value;
                Util.Monitor.Log($"Warped {animal.displayName}({animal.shortDisplayType()}) to {animal.displayHouse}@[{animal.homeLocation.X}, {animal.homeLocation.Y}]");
                animal.warpHome(farm, animal);
            }
        }

        public static void AutoAnimalDoor()
        {
            Farm farm = Game1.getFarm();
            foreach (Building building in farm.buildings)
            {
                switch (building)
                {
                    case Coop coop:
                    {
                        if (coop.indoors.Value is AnimalHouse house)
                        {
                            if (house.animals.Any() && coop.animalDoorOpen.Value)
                            {
                                coop.animalDoorOpen.Value = false;
                                Util.Helper.Reflection.GetField<NetInt>(coop, "animalDoorMotion").SetValue(new NetInt(2));
                            }
                        }
                        break;
                    }
                    case Barn barn:
                    {
                        if (barn.indoors.Value is AnimalHouse house)
                        {
                            if (house.animals.Any() && barn.animalDoorOpen.Value)
                            {
                                barn.animalDoorOpen.Value = false;
                                Util.Helper.Reflection.GetField<NetInt>(barn, "animalDoorMotion").SetValue(new NetInt(2));
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
