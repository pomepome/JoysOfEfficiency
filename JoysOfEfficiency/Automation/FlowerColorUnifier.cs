using System;
using System.Collections.Generic;
using System.Linq;

using JoysOfEfficiency.Core;

using Microsoft.Xna.Framework;

using StardewModdingAPI;

using StardewValley;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace JoysOfEfficiency.Automation
{
    using SVObject = Object;
    internal class FlowerColorUnifier
    {
        private static IMonitor Monitor => InstanceHolder.Monitor;
        private static Config Config => InstanceHolder.Config;
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
                        crop.tintColor.Value = Config.PoppyColor;
                        break;
                    case 591:
                        //Tulip
                        crop.tintColor.Value = Config.TulipColor;
                        break;
                    case 597:
                        //Blue Jazz
                        crop.tintColor.Value = Config.JazzColor;
                        break;
                    case 593:
                        //Summer Spangle
                        crop.tintColor.Value = Config.SummerSpangleColor;
                        break;
                    case 595:
                        //Fairy Rose
                        crop.tintColor.Value = Config.FairyRoseColor;
                        break;
                    default:
                        Color? color = GetCustomizedFlowerColor(crop.indexOfHarvest.Value);
                        if (color != null)
                        {
                            crop.tintColor.Value = color.Value;
                            break;
                        }
                        else
                        {
                            continue;   
                        }
                }

                if (oldColor.PackedValue == crop.tintColor.Value.PackedValue)
                {
                    continue;
                }

                SVObject obj = new SVObject(crop.indexOfHarvest.Value, 1);
                Monitor.Log($"changed {obj.DisplayName} @[{loc.X},{loc.Y}] to color(R:{crop.tintColor.R},G:{crop.tintColor.G},B:{crop.tintColor.B},A:{crop.tintColor.A})");
            }
        }

        private static Color? GetCustomizedFlowerColor(int indexOfHarvest)
        {
            return Config.CustomizedFlowerColors.TryGetValue(indexOfHarvest, out Color color) ? (Color?)color : null;
        }
    }
}