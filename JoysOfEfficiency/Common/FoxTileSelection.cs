using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace JoysOfEfficiency.Common
{
    using Location = GameLocation;

    internal static class FoxTileSelection
    {
        internal static bool Spiral( Vector2 xy, Vector2 playerPos, int dir, Func<int, int, bool> affect, int tier = 0, int radius = 1 )
        {
            int x = (int)xy.X;
            int y = (int)xy.Y;
            if ( AreaOfEffect( affect, x, y, playerPos, dir, tier ) ) return true;

            Func<bool>[] f = new Func<bool>[4];
            f[0] = () => {
                if ( AreaOfEffect( affect, x, --y, playerPos, dir, tier ) )
                    return true;
                return false;
            };
            f[1] = () => {
                if ( AreaOfEffect( affect, ++x, y, playerPos, dir, tier ) )
                    return true;
                return false;
            };
            f[2] = () => {
                if ( AreaOfEffect( affect, x, ++y, playerPos, dir, tier ) )
                    return true;
                return false;
            };
            f[3] = () => {
                if ( AreaOfEffect( affect, --x, y, playerPos, dir, tier ) )
                    return true;
                return false;
            };

            int d = dir;
            if ( d < 0 )
                d = 0;

            for ( int r = 1 ; r <= radius ; r++ ) {
                if ( f[d % 4].Invoke() ) return true;

                d++;
                for ( int i = 1 ; i <= 2 * r - 1 ; i++ )
                    if ( f[d % 4].Invoke() ) return true;

                d++;
                for ( int i = 1 ; i <= 2 * r ; i++ )
                    if ( f[d % 4].Invoke() ) return true;

                d++;
                for ( int i = 1 ; i <= 2 * r ; i++ )
                    if ( f[d % 4].Invoke() ) return true;

                d++;
                for ( int i = 1 ; i <= 2 * r ; i++ )
                    if ( f[d % 4].Invoke() ) return true;

            }
            return false;
        }

        internal static bool AreaOfEffect( Func<int, int, bool> affect, int x, int y, Vector2 playerPos, int dir, int tier = 0 )
        {
            if ( playerPos.X > x ) dir = 3;
            if ( playerPos.X < x ) dir = 1;
            if ( playerPos.Y > y ) dir = 0;
            if ( playerPos.Y < y ) dir = 2;

            bool ret = false;

            switch ( tier ) {
                case 0:
                    ret |= affect( x, y );
                    break;
                case 1:
                    ret |= affect( x, y );
                    if ( ret ) {
                        switch ( dir ) {
                            case 0:
                                ret |= affect( x, y - 1 );
                                ret |= affect( x, y - 2 );
                                break;

                            case 1:
                                ret |= affect( x + 1, y );
                                ret |= affect( x + 2, y );
                                break;

                            case 2:
                                ret |= affect( x, y + 1 );
                                ret |= affect( x, y + 2 );
                                break;

                            case 3:
                                ret |= affect( x - 1, y );
                                ret |= affect( x - 2, y );
                                break;
                        }
                    }
                    break;
                case 2:
                    ret |= affect( x, y );
                    if ( ret ) {
                        switch ( dir ) {
                            case 0:
                                ret |= affect( x, y - 1 );
                                ret |= affect( x, y - 2 );
                                ret |= affect( x, y - 3 );
                                ret |= affect( x, y - 4 );
                                break;

                            case 1:
                                ret |= affect( x + 1, y );
                                ret |= affect( x + 2, y );
                                ret |= affect( x + 3, y );
                                ret |= affect( x + 4, y );
                                break;

                            case 2:
                                ret |= affect( x, y + 1 );
                                ret |= affect( x, y + 2 );
                                ret |= affect( x, y + 3 );
                                ret |= affect( x, y + 4 );
                                break;

                            case 3:
                                ret |= affect( x - 1, y );
                                ret |= affect( x - 2, y );
                                ret |= affect( x - 3, y );
                                ret |= affect( x - 4, y );
                                break;
                        }
                    }
                    break;
                case 3:
                    ret |= affect( x, y );
                    if ( ret ) {
                        switch ( dir ) {
                            case 0:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x + i, y );
                                    ret |= affect( x + i, y - 1 );
                                    ret |= affect( x + i, y - 2 );
                                }
                                break;

                            case 1:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x, y + i );
                                    ret |= affect( x + 1, y + i );
                                    ret |= affect( x + 2, y + i );
                                }
                                break;

                            case 2:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x + i, y );
                                    ret |= affect( x + i, y + 1 );
                                    ret |= affect( x + i, y + 2 );
                                }
                                break;

                            case 3:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x, y + i );
                                    ret |= affect( x - 1, y + i );
                                    ret |= affect( x - 2, y + i );
                                }
                                break;
                        }
                    }
                    break;
                case 4:
                    ret |= affect( x, y );
                    if ( ret ) {
                        switch ( dir ) {
                            case 0:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x + i, y );
                                    ret |= affect( x + i, y - 1 );
                                    ret |= affect( x + i, y - 2 );
                                    ret |= affect( x + i, y - 3 );
                                    ret |= affect( x + i, y - 4 );
                                    ret |= affect( x + i, y - 5 );
                                }
                                break;

                            case 1:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x, y + i );
                                    ret |= affect( x + 1, y + i );
                                    ret |= affect( x + 2, y + i );
                                    ret |= affect( x + 3, y + i );
                                    ret |= affect( x + 4, y + i );
                                    ret |= affect( x + 5, y + i );
                                }
                                break;

                            case 2:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x + i, y );
                                    ret |= affect( x + i, y + 1 );
                                    ret |= affect( x + i, y + 2 );
                                    ret |= affect( x + i, y + 3 );
                                    ret |= affect( x + i, y + 4 );
                                    ret |= affect( x + i, y + 5 );
                                }
                                break;

                            case 3:
                                for ( int i = -1 ; i <= 1 ; i++ ) {
                                    ret |= affect( x, y + i );
                                    ret |= affect( x - 1, y + i );
                                    ret |= affect( x - 2, y + i );
                                    ret |= affect( x - 3, y + i );
                                    ret |= affect( x - 4, y + i );
                                    ret |= affect( x - 5, y + i );
                                }
                                break;
                        }
                    }
                    break;
            }
            return ret;
        }

        internal static TerrainFeature GetTileAtLocation( Location location, Vector2 xy )
        {
            if ( location.terrainFeatures.ContainsKey( xy ) )
                return location.terrainFeatures[xy];
            return null;
        }

        internal static HoeDirt TileIsWaterableCrop( Location location, Vector2 xy )
        {
            if ( ( GetTileAtLocation( location, xy ) is HoeDirt tile ) && ( tile.crop != null && !tile.crop.dead.Value && tile.state.Value == 0 ) )
                return tile;
            return null;
        }

        internal static HoeDirt TileIsHarvestableCrop( Location location, Vector2 xy )
        {
            if ( ( GetTileAtLocation( location, xy ) is HoeDirt tile ) && ( tile.crop != null && tile.readyForHarvest() ) )
                return tile;
            return null;
        }

    }
}
