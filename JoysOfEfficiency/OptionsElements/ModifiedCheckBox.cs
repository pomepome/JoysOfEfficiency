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
    internal class ModifiedCheckBox : OptionsElement
    {

        private bool IsChecked { get; set; }
        Action<int, bool> _valueChanged;
        Func<int, bool> _isDisabled;
        public ModifiedCheckBox(string label, int which, bool initial, Action<int, bool> callback = null, Func<int,bool> isDisabled = null) : base(label, -1, -1, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, 0)
        {
            this.label = ModEntry.ModHelper.Translation.Get($"options.{label}");
            IsChecked = initial;
            _valueChanged = callback;
            whichOption = which;
            _isDisabled = isDisabled ?? (i =>  false);
        }

        public override void receiveLeftClick(int x, int y)
        {
            if(greyedOut)
            {
                return;
            }
            Game1.playSound("drumkit6");
            base.receiveLeftClick(x, y);
            IsChecked = !IsChecked;
            _valueChanged?.Invoke(whichOption, IsChecked);
        }

        public override void draw(SpriteBatch spriteBatch, int slotX, int slotY)
        {
            greyedOut = _isDisabled(whichOption);
            spriteBatch.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y), this.IsChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked, Color.White * (greyedOut ? 0.33f : 1f), 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.4f);
            base.draw(spriteBatch, slotX, slotY);
        }
    }
}
