using System.Collections.Generic;
using System.Linq;
using JoysOfEfficiency.Core;
using JoysOfEfficiency.Utils;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace JoysOfEfficiency.Automation
{
    internal class AnimalAutomation
    {
        private static IMonitor Monitor => InstanceHolder.Monitor;

        public static void LetAnimalsInHome()
        {
            Farm farm = Game1.getFarm();
            foreach (KeyValuePair<long, FarmAnimal> kv in farm.animals.Pairs.ToArray())
            {
                FarmAnimal animal = kv.Value;
                InstanceHolder.Monitor.Log($"Warped {animal.displayName}({animal.shortDisplayType()}) to {animal.displayHouse}@[{animal.homeLocation.X}, {animal.homeLocation.Y}]");
                animal.warpHome(farm, animal);
            }
        }

        public static void AutoOpenAnimalDoor()
        {
            if (Game1.isRaining || Game1.isSnowing)
            {
                Monitor.Log("Don't open the animal door because of rainy/snowy weather.");
                return;
            }
            if (Game1.IsWinter)
            {
                Monitor.Log("Don't open the animal door because it's winter");
                return;
            }
            Farm farm = Game1.getFarm();
            foreach (Building building in farm.buildings)
            {
                switch (building)
                {
                    case Coop coop:
                    {
                        if (coop.indoors.Value is AnimalHouse house)
                        {
                            if (house.animals.Any() && !coop.animalDoorOpen.Value)
                            {
                                Monitor.Log($"Opening coop door @[{coop.animalDoor.X},{coop.animalDoor.Y}]");
                                coop.animalDoorOpen.Value = true;
                                InstanceHolder.Reflection.GetField<NetInt>(coop, "animalDoorMotion").SetValue(new NetInt(-2));
                            }
                        }
                        break;
                    }
                    case Barn barn:
                    {
                        if (barn.indoors.Value is AnimalHouse house)
                        {
                            if (house.animals.Any() && !barn.animalDoorOpen.Value)
                            {
                                Monitor.Log($"Opening barn door @[{barn.animalDoor.X},{barn.animalDoor.Y}]");
                                barn.animalDoorOpen.Value = true;
                                InstanceHolder.Reflection.GetField<NetInt>(barn, "animalDoorMotion").SetValue(new NetInt(-3));
                            }
                        }
                        break;
                    }
                }
            }
        }

        public static void AutoCloseAnimalDoor()
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
                                InstanceHolder.Reflection.GetField<NetInt>(coop, "animalDoorMotion").SetValue(new NetInt(2));
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
                                InstanceHolder.Reflection.GetField<NetInt>(barn, "animalDoorMotion").SetValue(new NetInt(2));
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
