using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency.Options
{
    public class ModifiedInputListener : OptionsElement
    {
        private bool isListening = false;
        private bool conflicting = false;
        private Keys button;

        private readonly Action<int, ModifiedInputListener> OnStartListening;
        private readonly Action<int, Keys> OnButtonPressed;
        private readonly Func<int, bool> IsDisabled;
        private Rectangle ButtonRect;

        private static readonly SpriteFont font = Game1.dialogueFont;

        ITranslationHelper translation;
        IClickableMenu menu;

        public ModifiedInputListener(IClickableMenu parent ,string label, int which, Keys initial, ITranslationHelper translationHelper, Action<int, Keys> onButtonPressed, Action<int, ModifiedInputListener> onStartListening = null, Func<int, bool> isDisabled = null) : base(label, -1, -1, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, 0)
        {
            button = initial;
            OnButtonPressed = onButtonPressed;
            IsDisabled = isDisabled ?? ((i) => false);
            translation = translationHelper;
            OnStartListening = onStartListening ?? ((i,obj) => { });
            whichOption = which;
            menu = parent;
        }

        public override void receiveKeyPress(Keys key)
        {
            if(key == button)
            {
                return;
            }
            if(key == Keys.Escape)
            {
                conflicting = false;
                isListening = false;
                OnButtonPressed(whichOption, button);
                return;
            }
            base.receiveKeyPress(key);
            Config config = ModEntry.Conf;
            if(Game1.options.isKeyInUse(key) || (key == ModEntry.Conf.KeyShowMenu))
            {
                conflicting = true;
                return;
            }
            if(isListening)
            {
                button = key;
                conflicting = false;
                isListening = false;
                OnButtonPressed(whichOption, key);
                return;
            }
        }

        public override void draw(SpriteBatch b, int slotX, int slotY)
        {
            string text = $"{label}: {button.ToString()}";
            Vector2 size = Game1.dialogueFont.MeasureString(text);
            b.DrawString(Game1.dialogueFont, text, new Vector2(slotX , slotY + 8), Color.Black, 0, new Vector2(), 1f, SpriteEffects.None, 1.0f);

            int x = slotX + (int)size.X + 8;

            ButtonRect = new Rectangle(x, slotY, 90 , 45);
            bounds = new Rectangle(0, 0, (int)size.X + ButtonRect.Width, ButtonRect.Height);

            b.Draw(Game1.mouseCursors, ButtonRect, new Rectangle(294, 428, 21, 11), Color.White, 0, Vector2.Zero, SpriteEffects.None, 1.0f);
            //IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), slotX + bounds.Left, slotY + bounds.Top, bounds.Width, bounds.Height, Color.White);
        }

        public override void receiveLeftClick(int x, int y)
        {
            base.receiveLeftClick(x, y);
            x += menu.xPositionOnScreen;
            y += ButtonRect.Height / 2;
            if(ButtonRect != null && x >= ButtonRect.Left && x <= ButtonRect.Right)
            {
                OnStartListening(whichOption, this);
                isListening = true;
            }
        }

        public void DrawStrings(SpriteBatch batch, int x, int y)
        {
            x += 16;
            y += 16;
            {
                Vector2 size = font.MeasureString(translation.Get("button.awaiting"));
                batch.DrawString(font, translation.Get("button.awaiting"), new Vector2(x, y), Color.Black, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
                y += (int)size.Y + 8;

                size = font.MeasureString(translation.Get("button.esc"));
                batch.DrawString(font, translation.Get("button.esc"), new Vector2(x, y), Color.Black, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
                y += (int)size.Y + 8;

                if (conflicting)
                {
                    size = font.MeasureString(translation.Get("button.conflict"));
                    batch.DrawString(font, translation.Get("button.conflict"), new Vector2(x, y), Color.Black, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
                    y += (int)size.Y + 8;
                }
            }
        }

        public Point GetListeningMessageWindowSize()
        {
            int x = 32;
            int y = 16;

            {
                Vector2 size = font.MeasureString(translation.Get("button.awaiting"));
                x += (int)size.X;
                y += (int)size.Y;
            }
            {
                Vector2 size = font.MeasureString(translation.Get("button.esc"));
                if(size.X + 16 > x)
                {
                    x = (int)size.X + 16;
                }
                y += (int)size.Y + 8;
            }
            if(conflicting){
                Vector2 size = font.MeasureString(translation.Get("button.conflict"));
                if (size.X + 16 > x)
                {
                    x = (int)size.X + 16;
                }
                y += (int)size.Y + 8;
            }

            return new Point(x, y);
        }
    }
}
