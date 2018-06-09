using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using JoysOfEfficiency.Common;

namespace JoysOfEfficiency.Automation
{
    using Game = Game1;
    using Player = Farmer;
    using Location = GameLocation;
    
    internal static class FoxWatering
    {
        internal static void WaterCrops( Player player )
        {
            WateringCan can = null;

            if ( !ModEntry.config.mustHoldCan )
                can = Utils.Util.FindToolFromInventory<WateringCan>( player );
            else {
                if ( !( player.CurrentTool is WateringCan c ) )
                    return;
                can = c;
            }

            if ( FoxBalance.MustBeWalking( player )) 
                return;

            if ( can != null ) {
                Location location = player.currentLocation;
                Vector2 xy = player.getTileLocation();
                int totalTilesWatered = 0;

                Func<int, int, bool> affect = ( x, y ) => {
                    Vector2 v;
                    v.X = x;
                    v.Y = y;
                    if ( FoxTileSelection.TileIsWaterableCrop( location, v ) is HoeDirt tile ) {
                        tile.state.Set( 1 );
                        totalTilesWatered += 1;
                        return true;
                    }
                    return false;
                };

                if ( FoxTileSelection.Spiral( xy, player.getTileLocation(), player.getDirection(), affect, can.UpgradeLevel ) ) {
                    Game.playSound( "slosh" );
                    int cost = 1;

                    if ( totalTilesWatered >= 2 ) cost++;
                    if ( totalTilesWatered >= 4 ) cost++;
                    if ( totalTilesWatered >= 8 ) cost++;
                    if ( totalTilesWatered >= 16 ) cost++;

                    player.Stamina -= ( 2 * cost ) - ( player.farmingLevel.Value / 10.0F );
                    can.WaterLeft -= cost;
                    FoxBalance.clickCooldown = 3 * ( cost + 1 );
                }
            }
        }

    }
}
