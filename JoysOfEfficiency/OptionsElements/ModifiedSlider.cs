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
    public class ModifiedSlider : OptionsElement
    {
        private readonly string Label;

        private readonly Action<int, int> SetValue;

        private readonly int MaxValue;
        private readonly int MinValue;

        private int Value;

        private readonly Func<bool> IsDisabled;

        private readonly Func<int, int, string> Format;

        public ModifiedSlider(string label, int which, int initialValue, int minValue, int maxValue, Action<int, int> setValue, Func<bool> disabled = null, Func<int, int, string> format = null, int width = 32)
            : base(label, -1, -1, width * Game1.pixelZoom, 6 * Game1.pixelZoom, 0)
        {
            whichOption = which;
            Label = label;
            Value = initialValue - minValue;
            MinValue = minValue;
            MaxValue = maxValue - minValue;
            SetValue = setValue ?? ((i, j) => { });
            IsDisabled = disabled ?? (() => false);
            Format = format ?? ((i, value) => value.ToString());
        }

        public override void leftClickHeld(int x, int y)
        {
            if (greyedOut)
                return;

            base.leftClickHeld(x, y);
            int oldValue = Value;
            Value = x >= bounds.X
                ? (x <= bounds.Right - 10 * Game1.pixelZoom ? (int)((x - bounds.X) / (this.bounds.Width - 10d * Game1.pixelZoom) * this.MaxValue) : this.MaxValue)
                : 0;
            if (Value != oldValue)
            {
                SetValue?.Invoke(whichOption, Value + MinValue);
            }

        }

        public override void receiveLeftClick(int x, int y)
        {
            if (greyedOut)
                return;
            base.receiveLeftClick(x, y);
            leftClickHeld(x, y);
        }

        public override void leftClickReleased(int x, int y)
        {
            SetValue?.Invoke(whichOption, Value + MinValue);
        }

        public override void draw(SpriteBatch spriteBatch, int slotX, int slotY)
        {
            label = $"{Label}: {Format(whichOption, Value + MinValue)}";
            greyedOut = IsDisabled();

            base.draw(spriteBatch, slotX, slotY);
            IClickableMenu.drawTextureBox(spriteBatch, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + bounds.X, slotY + bounds.Y, bounds.Width, bounds.Height, Color.White, Game1.pixelZoom, false);
            spriteBatch.Draw(Game1.mouseCursors, new Vector2(slotX + bounds.X + (bounds.Width - 10 * Game1.pixelZoom) * (Value / (float)MaxValue), slotY + bounds.Y), OptionsSlider.sliderButtonRect, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.9f);
        }
    }
}
