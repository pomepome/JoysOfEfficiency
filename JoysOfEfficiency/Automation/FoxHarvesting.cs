using System;
using StardewValley;
using JoysOfEfficiency.Common;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using JoysOfEfficiency.Utils;

namespace JoysOfEfficiency.Automation
{
    using Game = Game1;
    using Player = Farmer;
    using Location = GameLocation;

    internal static class FoxHarvesting
    {
        internal static void HarvestCrops( Player player )
        {
            if ( FoxBalance.MustBeWalking( player ) )
                return;

            Location location = player.currentLocation;
            Vector2 xy = player.getTileLocation();

            Func<int, int, bool> affect = ( x, y ) => {
                Vector2 v;
                v.X = x;
                v.Y = y;
                if ( FoxTileSelection.TileIsHarvestableCrop( location, v ) is HoeDirt tile ) {
                    /// TODO: Insert Flower check
                    return Util.Harvest( x, y, tile );
                }
                return false;
            };

            if ( FoxTileSelection.Spiral( xy, player.getTileLocation(), player.getDirection(), affect ) ) {
                FoxBalance.clickCooldown = 2;
            }
        }
    }
}
