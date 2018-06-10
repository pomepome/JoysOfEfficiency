using StardewValley;
using StardewValley.Tools;

namespace JoysOfEfficiency.Common
{
    using Player = Farmer;

    internal static class FoxBalance
    {
        internal static int clickCooldown = 0;

        internal static bool MustBeWalking( Player player )
        {
            if ( !ModEntry.config.mustBeWalking )
                return false;
            return ( player.running || player.canOnlyWalk ); 
        }

        internal static int getRadius( int r )
        {
            if ( ModEntry.config.balancedMode )
                return 1;
            return r;
        }
    }
}
