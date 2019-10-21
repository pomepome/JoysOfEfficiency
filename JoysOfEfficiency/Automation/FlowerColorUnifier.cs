using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoysOfEfficiency.Core;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using SVObject = StardewValley.Object;

namespace JoysOfEfficiency.Automation
{
    internal class FlowerColorUnifier
    {
        private static IMonitor Monitor => InstanceHolder.Monitor;
        public static void UnifyFlowerColors()
        {
            foreach (KeyValuePair<Vector2, TerrainFeature> featurePair in Game1.currentLocation.terrainFeatures.Pairs.Where(kv => kv.Value is HoeDirt))
            {
                Vector2 loc = featurePair.Key;
                HoeDirt dirt = (HoeDirt)featurePair.Value;
                Crop crop = dirt.crop;
                if (crop == null || dirt.crop.dead.Value || !dirt.crop.programColored.Value)
                {
                    continue;
                }
                Color oldColor = crop.tintColor.Value;
                switch (crop.indexOfHarvest.Value)
                {
                    case 376:
                        //Poppy
                        crop.tintColor.Value = InstanceHolder.Config.PoppyColor;
                        break;
                    case 591:
                        //Tulip
                        crop.tintColor.Value = InstanceHolder.Config.TulipColor;
                        break;
                    case 597:
                        //Blue Jazz
                        crop.tintColor.Value = InstanceHolder.Config.JazzColor;
                        break;
                    case 593:
                        //Summer Spangle
                        crop.tintColor.Value = InstanceHolder.Config.SummerSpangleColor;
                        break;
                    case 595:
                        //Fairy Rose
                        crop.tintColor.Value = InstanceHolder.Config.FairyRoseColor;
                        break;
                    default:
                        continue;
                }

                if (oldColor.PackedValue != crop.tintColor.Value.PackedValue)
                {
                    SVObject obj = new SVObject(crop.indexOfHarvest.Value, 1);
                    Monitor.Log($"changed {obj.DisplayName} @[{loc.X},{loc.Y}] to color(R:{crop.tintColor.R},G:{crop.tintColor.G},B:{crop.tintColor.B},A:{crop.tintColor.A})");
                }
            }
        }
    }
}
