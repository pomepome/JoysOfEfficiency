using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;

namespace JoysOfEfficiency.OptionsElements
{
    public abstract class OptionsElementWithLabel : OptionsElement
    {
        protected OptionsElementWithLabel(string label, int x, int y, int width, int height, int whichOption = -1)
            : base(label, x, y, width, height, whichOption) { }

        private int getOffsetLabel()
        {
            return Constants.TargetPlatform == GamePlatform.Android ? bounds.Width + 8 : 0;
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            base.draw(b, slotX + getOffsetLabel(), slotY);
        }
    }
}
