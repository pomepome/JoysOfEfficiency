using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace JoysOfEfficiency.OptionsElements
{
    class ColorBox : OptionsElement
    {
        private Color _color;

        public ColorBox(string name, int which, Color color, int width = 128, int height = 128)
            : base(name, -1, -1, width, height, which)
        {
            _color = color;
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            slotX += 32;
            int x = slotX + 12;
            int y = slotY + 12;
            int width = bounds.Width - 24;
            int height = bounds.Height - 24;
            Util.DrawWindow(b, slotX, slotY, bounds.Width, bounds.Height);
                Util.DrawColoredBox(b, x, y, width, height, _color);
        }

        public void SetColor(Color c)
        {
            SetColor(c.R, c.G, c.B);
        }

        public void SetColor(int r, int g, int b)
        {
            _color.R = (byte) r;
            _color.G = (byte) g;
            _color.B = (byte) b;
        }
    }
}
