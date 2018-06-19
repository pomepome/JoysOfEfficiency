using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency.OptionsElements
{
    internal class LabelComponent : OptionsElement
    {
        public LabelComponent(string label) : base(label, -1, -1, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, 0) { }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            Utility.drawTextWithShadow(b, label, Game1.dialogueFont, new Vector2(slotX, slotY), Color.Black);
        }
    }
}
