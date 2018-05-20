using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency
{
    public class ModifiedSlider : OptionsElement
    {
        private readonly string Label;
        
        private readonly Action<int,int> SetValue;
        
        private readonly int MaxValue;
        
        private int Value;
        
        private readonly Func<bool> IsDisabled;
        
        private readonly Func<int, int, string> Format;
        
        public ModifiedSlider(string label,int which, int initialValue, int maxValue, Action<int,int> setValue, Func<bool> disabled = null, Func<int, int, string> format = null, int width = 32)
            : base(label, -1, -1, width * Game1.pixelZoom, 6 * Game1.pixelZoom, 0)
        {
            this.whichOption = which;
            this.Label = label;
            this.Value = initialValue;
            this.MaxValue = maxValue;
            this.SetValue = setValue;
            this.IsDisabled = disabled ?? (() => false);
            this.Format = format ?? ((i, value) => value.ToString());
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.greyedOut)
                return;

            base.leftClickHeld(x, y);
            int oldValue = Value;
            this.Value = x >= this.bounds.X
                ? (x <= this.bounds.Right - 10 * Game1.pixelZoom ? (int)((x - this.bounds.X) / (this.bounds.Width - 10d * Game1.pixelZoom) * this.MaxValue) : this.MaxValue)
                : 0;
            if(Value != oldValue)
            {
                SetValue?.Invoke(whichOption, Value);
            }

        }

        public override void receiveLeftClick(int x, int y)
        {
            if (this.greyedOut)
                return;
            base.receiveLeftClick(x, y);
            this.leftClickHeld(x, y);
        }

        public override void leftClickReleased(int x, int y)
        {
            SetValue?.Invoke(whichOption, Value);
        }

        public override void draw(SpriteBatch spriteBatch, int slotX, int slotY)
        {
            this.label = $"{this.Label}: {this.Format(whichOption, this.Value)}";
            this.greyedOut = this.IsDisabled();

            base.draw(spriteBatch, slotX, slotY);
            IClickableMenu.drawTextureBox(spriteBatch, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X, slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, Game1.pixelZoom, false);
            spriteBatch.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X + (this.bounds.Width - 10 * Game1.pixelZoom) * (this.Value / (float)this.MaxValue), slotY + this.bounds.Y), OptionsSlider.sliderButtonRect, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.9f);
        }
    }
}
