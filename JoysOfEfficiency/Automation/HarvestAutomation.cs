using System.Collections.Generic;
using System.Linq;
using JoysOfEfficiency.Core;
using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using SVObject = StardewValley.Object;

namespace JoysOfEfficiency.Automation
{
    internal class HarvestAutomation
    {
        private static IMonitor Monitor => InstanceHolder.Monitor;
        private static Config Config => InstanceHolder.Config;

        private static readonly List<Vector2> FlowerLocationProducingNectar = new List<Vector2>();
        public static void UpdateNectarInfo()
        {
            FlowerLocationProducingNectar.Clear();
            foreach (KeyValuePair<Vector2, SVObject> kv in 
                Game1.currentLocation.Objects.Pairs.Where(
                    pair => pair.Value.Name == "Bee House")
                )
            {
                Vector2 houseLoc = kv.Key;
                foreach (Vector2 flowerLoc in GetAreaOfCollectingNectar(houseLoc))
                {
                    if ((int)flowerLoc.X >= 0 && (int)flowerLoc.Y >= 0 && !FlowerLocationProducingNectar.Contains(flowerLoc))
                    {
                        FlowerLocationProducingNectar.Add(flowerLoc);
                    }
                }
            }
        }

        public static void HarvestNearbyCrops(Farmer player)
        {
            GameLocation location = player.currentLocation;
            int radius = Config.AutoHarvestRadius;

            if (Config.ProtectNectarProducingFlower)
            {
                UpdateNectarInfo();
            }

            foreach (KeyValuePair<Vector2, HoeDirt> kv in Util.GetFeaturesWithin<HoeDirt>(radius))
            {
                Vector2 loc = kv.Key;
                HoeDirt dirt = kv.Value;
                if (dirt.crop == null)
                    continue;

                if (dirt.readyForHarvest())
                {
                    if (IsBlackListed(dirt.crop) || (InstanceHolder.Config.ProtectNectarProducingFlower && IsProducingNectar(loc)))
                        continue;
                    if (Util.Harvest((int)loc.X, (int)loc.Y, dirt))
                    {
                        if (dirt.crop.regrowAfterHarvest.Value == -1 || dirt.crop.forageCrop.Value)
                        {
                            //destroy crop if it does not regrow.
                            dirt.destroyCrop(loc, true, location);
                        }
                    }
                }
            }
            foreach (IndoorPot pot in Util.GetObjectsWithin<IndoorPot>(radius))
            {
                HoeDirt dirt = pot.hoeDirt.Value;
                if (dirt?.crop == null || !dirt.readyForHarvest())
                    continue;
                Vector2 tileLoc = Util.GetLocationOf(location, pot);
                if (dirt.crop.harvest((int)tileLoc.X, (int)tileLoc.Y, dirt))
                {
                    if (dirt.crop.regrowAfterHarvest.Value == -1 || dirt.crop.forageCrop.Value)
                    {
                        //destroy crop if it does not regrow.
                        dirt.destroyCrop(tileLoc, true, location);
                    }
                }
            }
        }

        public static void WaterNearbyCrops()
        {
            WateringCan can = Util.FindToolFromInventory<WateringCan>(Game1.player, InstanceHolder.Config.FindCanFromInventory);
            if (can == null)
                return;

            Util.GetMaxCan(can);
            bool watered = false;
            foreach (KeyValuePair<Vector2, HoeDirt> kv in Util.GetFeaturesWithin<HoeDirt>(InstanceHolder.Config.AutoWaterRadius))
            {
                HoeDirt dirt = kv.Value;
                float consume = 2 * (1.0f / (can.UpgradeLevel / 2.0f + 1));
                if (dirt.crop == null || dirt.crop.dead.Value || dirt.state.Value != 0 ||
                    !(Game1.player.Stamina >= consume)
                    || can.WaterLeft <= 0)
                {
                    continue;
                }

                dirt.state.Value = 1;
                Game1.player.Stamina -= consume;
                can.WaterLeft--;
                watered = true;
            }
            foreach (IndoorPot pot in Util.GetObjectsWithin<IndoorPot>(InstanceHolder.Config.AutoWaterRadius))
            {
                if (pot.hoeDirt.Value == null)
                    continue;

                HoeDirt dirt = pot.hoeDirt.Value;
                float consume = 2 * (1.0f / (can.UpgradeLevel / 2.0f + 1));
                if (dirt.crop != null && !dirt.crop.dead.Value && dirt.state.Value != 1 && Game1.player.Stamina >= consume && can.WaterLeft > 0)
                {
                    dirt.state.Value = 1;
                    pot.showNextIndex.Value = true;
                    Game1.player.Stamina -= consume;
                    can.WaterLeft--;
                    watered = true;
                }
            }
            if (watered)
            {
                Game1.playSound("slosh");
            }
        }

        public static void ToggleBlacklistUnderCursor()
        {
            GameLocation location = Game1.currentLocation;
            Vector2 tile = Game1.currentCursorTile;
            if (!location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrain))
                return;
            if (!(terrain is HoeDirt dirt))
                return;

            if (dirt.crop == null)
            {
                Util.ShowHudMessage("There is no crop under the cursor");
            }
            else
            {
                string name = dirt.crop.forageCrop.Value ? new SVObject(dirt.crop.whichForageCrop.Value, 1).Name : new SVObject(dirt.crop.indexOfHarvest.Value, 1).Name;
                if (name == "")
                {
                    return;
                }

                string text = ToggleBlackList(dirt.crop)
                    ? $"{name} has been added to AutoHarvest exception"
                    : $"{name} has been removed from AutoHarvest exception";
                Util.ShowHudMessage(text, 1000);
                Monitor.Log(text);
            }
        }
        /// <summary>
        /// Is the dirt's crop is a flower and producing nectar
        /// </summary>
        /// <param name="location">HoeDirt location to evaluate</param>
        /// <returns>Result</returns>
        private static bool IsProducingNectar(Vector2 location) => FlowerLocationProducingNectar.Contains(location);

        private static bool IsBlackListed(Crop crop)
        {
            int index = crop.forageCrop.Value ? crop.whichForageCrop.Value : crop.indexOfHarvest.Value;
            return InstanceHolder.Config.HarvestException.Contains(index);
        }

        private static bool ToggleBlackList(Crop crop)
        {
            int index = crop.forageCrop.Value ? crop.whichForageCrop.Value : crop.indexOfHarvest.Value;
            if (IsBlackListed(crop))
                InstanceHolder.Config.HarvestException.Remove(index);
            else
                InstanceHolder.Config.HarvestException.Add(index);

            InstanceHolder.ModInstance.WriteConfig();
            return IsBlackListed(crop);
        }

        private static IEnumerable<Vector2> GetAreaOfCollectingNectar(Vector2 homePoint)
        {
            List<Vector2> cropLocations = new List<Vector2>();
            Queue<Vector2> vector2Queue = new Queue<Vector2>();
            HashSet<Vector2> vector2Set = new HashSet<Vector2>();
            vector2Queue.Enqueue(homePoint);
            for (int index1 = 0; index1 <= 150 && vector2Queue.Count > 0; ++index1)
            {
                Vector2 index2 = vector2Queue.Dequeue();
                if (Game1.currentLocation.terrainFeatures.ContainsKey(index2) &&
                    Game1.currentLocation.terrainFeatures[index2] is HoeDirt dirt && dirt.crop != null &&
                    dirt.crop.programColored.Value && !dirt.crop.dead.Value && dirt.crop.currentPhase.Value >= dirt.crop.phaseDays.Count - 1)
                {
                    cropLocations.Add(index2);
                }
                foreach (Vector2 adjacentTileLocation in Utility.getAdjacentTileLocations(index2))
                {
                    if (!vector2Set.Contains(adjacentTileLocation))
                        vector2Queue.Enqueue(adjacentTileLocation);
                }
                vector2Set.Add(index2);
            }

            return cropLocations;
        }
    }
}
