using JoysOfEfficiency.Utils;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace JoysOfEfficiency.Core
{
    internal class MenuEvents
    {
        private static Config Conf => InstanceHolder.Config;
        public void OnMenuChanged(object sender, MenuChangedEventArgs args)
        {
            if (Conf.AutoLootTreasures && args.NewMenu is ItemGrabMenu menu)
            {
                //Opened ItemGrabMenu
                Util.LootAllAcceptableItems(menu);
            }

            if (Conf.CollectLetterAttachmentsAndQuests && args.NewMenu is LetterViewerMenu letter)
            {
                Util.CollectMailAttachmentsAndQuests(letter);
            }
        }
    }
}
