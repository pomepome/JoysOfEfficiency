using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace JoysOfEfficiency
{

    using Player = StardewValley.Farmer;
    using SVObject = StardewValley.Object;
    public class ModEntry : Mod
    {

        private Config config = null;
        private string hoverText;
        private bool catchingTreasure = false;

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<Config>();
            GameEvents.UpdateTick += OnGameUpdate;

            InputEvents.ButtonPressed += OnButtonPressed;

            SaveEvents.BeforeSave += OnBeforeSave;
            TimeEvents.AfterDayStarted += OnPostSave;

            GraphicsEvents.OnPostRenderHudEvent += OnPostRenderHUD;
        }

        #region EventHandlers

        private void OnGameUpdate(object sender, EventArgs args)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            Player player = Game1.player;
            IReflectionHelper reflection = Helper.Reflection;
            if (config.AutoWaterNearbyCrops && player.currentLocation.IsFarm)
            {
                RectangleE bb = Expand(player.GetBoundingBox(), 3 * Game1.tileSize);
                WateringCan can = null;
                foreach (Item item in player.Items)
                {
                    //Search Watering Can To Use
                    can = item as WateringCan;
                    if (can != null)
                    {
                        break;
                    }
                }
                if (can == null)
                {
                    return;
                }
                bool watered = false;
                foreach (KeyValuePair<Vector2, TerrainFeature> kv in player.currentLocation.terrainFeatures)
                {
                    Vector2 location = kv.Key;
                    TerrainFeature tf = kv.Value;
                    Point centre = tf.getBoundingBox(location).Center;
                    if (bb.IsInternalPoint(centre.X, centre.Y) && tf is HoeDirt dirt)
                    {
                        if (dirt.crop != null && dirt.state == 0 && player.Stamina >= 2 && can.WaterLeft > 0)
                        {
                            dirt.state = 1;
                            player.Stamina -= 2;
                            can.WaterLeft--;
                            watered = true;
                        }
                    }
                }
                if (watered)
                {
                    Game1.playSound("slosh");
                }
            }
            if (config.GiftInformation)
            {
                hoverText = null;
                if (player.CurrentItem == null || !player.CurrentItem.canBeGivenAsGift())
                {
                }
                else
                {
                    List<NPC> npcList = player.currentLocation.characters.Where(a => a.isVillager()).ToList();
                    foreach (NPC npc in npcList)
                    {
                        RectangleE npcRect = new RectangleE(npc.position.X, npc.position.Y - npc.sprite.getHeight() - Game1.tileSize / 1.5f, npc.sprite.getWidth() * 3 + npc.sprite.getWidth() / 1.5f, (npc.sprite.getHeight() * 3.5f));

                        if (npcRect.IsInternalPoint(Game1.getMouseX() + Game1.viewport.X, Game1.getMouseY() + Game1.viewport.Y))
                        {
                            //Mouse hovered on the NPC
                            StringBuilder key = new StringBuilder("taste.");
                            switch (npc.getGiftTasteForThisItem(player.CurrentItem))
                            {
                                case 0: key.Append("love."); break;
                                case 2: key.Append("like."); break;
                                case 4: key.Append("dislike."); break;
                                case 6: key.Append("hate."); break;
                                default: key.Append("neutral."); break;
                            }
                            switch (npc.gender)
                            {
                                case 0: key.Append("male"); break;
                                default: key.Append("female"); break;
                            }
                            Translation translation = Helper.Translation.Get(key.ToString());
                            hoverText = translation?.ToString();
                        }
                    }
                }
            }
            if (config.AutoPetNearbyAnimals)
            {
                int radius = 3 * Game1.tileSize;
                RectangleE bb = new RectangleE(player.position.X - radius, player.position.Y - radius, radius * 2, radius * 2);
                List<FarmAnimal> animalList = GetAnimalsList(player);
                foreach (FarmAnimal animal in animalList)
                {
                    if (bb.IsInternalPoint(animal.position.X, animal.position.Y) && !animal.wasPet)
                    {
                        animal.pet(player);
                    }
                }
            }
            if (player.CurrentTool is FishingRod rod)
            {
                IReflectedField<int> whichFish = reflection.GetField<int>(rod, "whichFish");
                if (rod.isNibbling && !rod.isReeling && !rod.hit && !rod.pullingOutOfWater && !rod.fishCaught)
                {
                    if (config.AutoFishing)
                    {
                        rod.DoFunction(player.currentLocation, 1, 1, 1, player);
                        rod.hit = true;
                    }
                }
                if (config.MuchFasterBiting)
                {
                    rod.timeUntilFishingBite -= 1000;
                }
            }
            if(config.AutoGate)
            {
                TryToggleGate(player);
            }
            if(config.AutoEat)
            {
                TryToEatIfNeeded(player);
            }
        }

        private void OnButtonPressed(object sender, EventArgsInput args)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;
            if (config.HowManyStonesLeft && args.Button == config.KeyShowStonesLeft)
            {
                Player player = Game1.player;
                if (player.currentLocation is MineShaft mine)
                {
                    int stonesLeft = reflection.GetField<int>(mine, "stonesLeftOnThisLevel").GetValue();
                    if (stonesLeft == 0)
                    {
                        ShowHUDMessage(translation.Get("stones.none"));
                    }
                    else
                    {
                        bool single = stonesLeft == 1;
                        if(single)
                        {
                            ShowHUDMessage(translation.Get("stones.one"));
                        }
                        else
                        {
                            ShowHUDMessage(Format(translation.Get("stones.many"), stonesLeft));
                        }
                    }
                }
            }
        }

        private void OnPostRenderHUD(object sender, EventArgs args)
        {
            if(Context.IsPlayerFree && !string.IsNullOrEmpty(hoverText) && Game1.player.CurrentItem != null)
            {
                DrawSimpleTextbox(Game1.spriteBatch, hoverText, Game1.smallFont, Game1.player.CurrentItem);
            }
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is BobberBar bar)
            {
                if (config.FishingInfo)
                {
                    DrawFishingInfoBox(Game1.spriteBatch, bar, Game1.dialogueFont);
                }

                if (config.AutoFishing)
                {
                    AutoFishing(bar);
                }
            }
        }

        private void OnBeforeSave(object sender, EventArgs args)
        {
            if(!Context.IsWorldReady || !Context.IsPlayerFree || !config.AutoAnimalDoor)
            {
                return;
            }
            Log("BeforeSave");
            LetAnimalsInHome();

            Farm farm = Game1.getFarm();
            foreach (Building building in farm.buildings)
            {
                if (building is Coop coop)
                {
                    if (coop.indoors is AnimalHouse house)
                    {
                        if (house.animals.Any() && coop.animalDoorOpen)
                        {
                            coop.animalDoorOpen = false;
                            Helper.Reflection.GetField<int>(coop, "animalDoorMotion").SetValue(2);
                        }
                    }
                }
                else if (building is Barn barn)
                {
                    if (barn.indoors is AnimalHouse house)
                    {
                        if (house.animals.Any() && barn.animalDoorOpen)
                        {
                            barn.animalDoorOpen = false;
                            Helper.Reflection.GetField<int>(barn, "animalDoorMotion").SetValue(2);
                        }
                    }
                }
            }
        }

        private void OnPostSave(object sender, EventArgs args)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree || !config.AutoAnimalDoor)
            {
                return;
            }
            Log("PostSave");
            if(Game1.isRaining || Game1.isSnowing)
            {
                Log("Don't open because of rainy/snowy weather.");
                return;
            }
            Farm farm = Game1.getFarm();
            foreach (Building building in farm.buildings)
            {
                if (building is Coop coop)
                {
                    if (coop.indoors is AnimalHouse house)
                    {
                        if (house.animals.Any() && !coop.animalDoorOpen)
                        {
                            coop.animalDoorOpen = true;
                            Helper.Reflection.GetField<int>(coop, "animalDoorMotion").SetValue(-2);
                        }
                    }
                }
                else if(building is Barn barn)
                {
                    if (barn.indoors is AnimalHouse house)
                    {
                        if (house.animals.Any() && !barn.animalDoorOpen)
                        {
                            barn.animalDoorOpen = true;
                            Helper.Reflection.GetField<int>(barn, "animalDoorMotion").SetValue(-3);
                        }
                    }
                }
            }
        }

        #endregion

        #region Utilities

        private void TryToEatIfNeeded(Player player)
        {
            if(Game1.isEating)
            {
                return;
            }
            if(player.Stamina <= player.MaxStamina * config.StaminaToEatRatio || player.health <= player.maxHealth * config.HealthToEatRatio)
            {
                SVObject itemToEat = null;
                foreach(SVObject item in player.items.OfType<SVObject>())
                {
                    if(item.Edibility > 0)
                    {
                        //It's a edible item
                        if(itemToEat == null || (itemToEat.Edibility / itemToEat.salePrice() < item.Edibility / item.salePrice()))
                        {
                            //Found good edibility per price or just first food
                            itemToEat = item;
                        }
                    }
                }
                if(itemToEat != null)
                {
                    Log("You ate {0}.", itemToEat.DisplayName);
                    Game1.playerEatObject(itemToEat);
                    itemToEat.Stack--;
                    if(itemToEat.Stack == 0)
                    {
                        player.removeItemFromInventory(itemToEat);
                    }
                }
            }
        }

        public void TryToggleGate(Player player)
        {
            GameLocation location = player.currentLocation;

            foreach (KeyValuePair<Vector2, SVObject> kv in location.Objects)
            {
                Vector2 loc = kv.Key;
                if (!(kv.Value is Fence fence) || !fence.isGate)
                {
                    continue;
                }

                RectangleE bb = Expand(fence.getBoundingBox(loc), 2 * Game1.tileSize);
                if (!bb.IsInternalPoint(player.Position))
                {
                    //It won't work if player is far away.
                    continue;
                }

                bool? isUpDown = IsUpsideDown(location, fence);
                if (isUpDown == null)
                {
                    continue;
                }

                int gatePosition = fence.gatePosition;
                bool flag = IsPlayerInClose(player, fence.TileLocation, isUpDown);


                if(flag && gatePosition == 0)
                {
                    fence.gatePosition = 88;
                    Game1.playSound("doorClose");
                }
                if(!flag && gatePosition >= 88)
                {
                    fence.gatePosition = 0;
                    Game1.playSound("doorClose");
                }
            }
        }

        private bool? IsUpsideDown(GameLocation location, Fence fence)
        {
            int num2 = 0;
            Vector2 tileLocation = fence.TileLocation;
            int whichType = fence.whichType;
            tileLocation.X += 1f;
            if (Game1.currentLocation.objects.ContainsKey(tileLocation) && Game1.currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)Game1.currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 100;
            }
            tileLocation.X -= 2f;
            if (Game1.currentLocation.objects.ContainsKey(tileLocation) && Game1.currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)Game1.currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 10;
            }
            tileLocation.X += 1f;
            tileLocation.Y += 1f;
            if (Game1.currentLocation.objects.ContainsKey(tileLocation) && Game1.currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)Game1.currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 500;
            }
            tileLocation.Y -= 2f;
            if (Game1.currentLocation.objects.ContainsKey(tileLocation) && Game1.currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)Game1.currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 1000;
            }

            if(fence.isGate)
            {
                if(num2 == 110)
                {
                    return true;
                }
                else if(num2 == 1500)
                {
                    return false;
                }
            }

            return null;
        }

        private bool IsPlayerInClose(Player player, Vector2 fenceLocation, bool? isUpDown)
        {
            if(isUpDown == null)
            {
                return false;
            }
            Vector2 playerTileLocation = player.getTileLocation();
            if(isUpDown == true)
            {
                return (playerTileLocation.X == fenceLocation.X) && (playerTileLocation.Y <= fenceLocation.Y + 1 && playerTileLocation.Y >= fenceLocation.Y - 1);
            }
            return (playerTileLocation.X >= fenceLocation.X - 1 && playerTileLocation.X <= fenceLocation.X + 1) && (playerTileLocation.Y == fenceLocation.Y);
        }

        public void AutoFishing(BobberBar bar)
        {
            IReflectionHelper reflection = Helper.Reflection;

            IReflectedField<float> bobberSpeed = reflection.GetField<float>(bar, "bobberBarSpeed");

            float barPos = reflection.GetField<float>(bar, "bobberBarPos").GetValue();
            int barHeight = reflection.GetField<int>(bar, "bobberBarHeight").GetValue();
            float fishPos = reflection.GetField<float>(bar, "bobberPosition").GetValue();
            float treasurePos = reflection.GetField<float>(bar, "treasurePosition").GetValue();
            float distanceFromCatching = reflection.GetField<float>(bar, "distanceFromCatching").GetValue();
            bool treasureCaught = reflection.GetField<bool>(bar, "treasureCaught").GetValue();
            bool treasure = reflection.GetField<bool>(bar, "treasure").GetValue();
            float treasureApeearTimer = reflection.GetField<float>(bar, "treasureAppearTimer").GetValue();
            float bobberBarSpeed = bobberSpeed.GetValue();

            float up = barPos, down = barPos + barHeight;

            if(treasure && treasureApeearTimer <= 0 && !treasureCaught)
            {
                if(!catchingTreasure && distanceFromCatching > 0.7f)
                {
                    catchingTreasure = true;
                }
                if(catchingTreasure && distanceFromCatching < 0.3f)
                {
                    catchingTreasure = false;
                }
                if (catchingTreasure)
                {
                    fishPos = treasurePos;
                }
            }

            float strength = (fishPos - (barPos + barHeight / 2)) / 16f;
            float distance = fishPos - up;

            float threshold = Cap(config.CPUThresholdFishing, 0, 0.5f);
            if (distance < threshold * barHeight || distance > (1 - threshold) * barHeight)
            {
                bobberBarSpeed = strength;
            }

            bobberSpeed.SetValue(bobberBarSpeed);
        }

        private float Cap(float f, float min, float max)
        {
            return f < min ? min : (f > max ? max : f);
        }

        private void Log(string format, params object[] args)
        {
            Monitor.Log(Format(format, args));
        }

        private string Format(string format, params object[] args)
        {
            return string.Format(format, args);
        }

        private List<FarmAnimal> GetAnimalsList(Player player)
        {
            List<FarmAnimal> list = new List<FarmAnimal>();
            if(player.currentLocation is Farm farm)
            {
                foreach(KeyValuePair<long,FarmAnimal> animal in farm.animals)
                {
                    list.Add(animal.Value);
                }
            }
            else if(player.currentLocation is AnimalHouse house)
            {
                foreach (KeyValuePair<long, FarmAnimal> animal in house.animals)
                {
                    list.Add(animal.Value);
                }
            }
            return list;
        }

        private void LetAnimalsInHome()
        {
            Farm farm = Game1.getFarm();
            foreach (KeyValuePair<long, FarmAnimal> kv in farm.animals.ToList())
            {
                FarmAnimal animal = kv.Value;
                animal.warpHome(farm, animal);
            }
        }

        private void ShowHUDMessage(string message, int duration = 3500)
        {
            HUDMessage hudMessage = new HUDMessage(message, 3)
            {
                noIcon = true,
                timeLeft = duration
            };
            Game1.addHUDMessage(hudMessage);
        }

        private RectangleE Expand(Rectangle rect, int radius)
        {
            return new RectangleE(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        private void DrawSimpleTextbox(SpriteBatch batch, string text, SpriteFont font, Item item)
        {
            Vector2 stringSize = font.MeasureString(text);
            int x = Game1.getMouseX() - (int)(stringSize.X)/2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;

            if(x < 0)
            {
                x = 0;
            }
            if(y < 0)
            {
                y = 0;
            }
            int rightX = (int)stringSize.X + Game1.tileSize / 2 + Game1.tileSize + 8;
            if(x + rightX > Game1.viewport.Width)
            {
                x = Game1.viewport.Width - rightX;
            }
            int bottomY = Math.Max(60, (int)(stringSize.Y + Game1.tileSize * 1.2));
            if(bottomY + y > Game1.viewport.Height)
            {
                y = Game1.viewport.Height - bottomY;
            }
            IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, rightX, bottomY, Color.White, 1f, true);
            if(!string.IsNullOrEmpty(text))
            {
                Vector2 vector2 = new Vector2(x + Game1.tileSize / 4, y + bottomY / 2 - 10);
                batch.DrawString(font, hoverText, vector2 + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, hoverText, vector2 + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, hoverText, vector2 + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, hoverText, vector2, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            }
            item.drawInMenu(batch, new Vector2(x + (int)stringSize.X + 24, y + 16), 1.0f,1.0f,0.9f,false);
        }

        private void DrawFishingInfoBox(SpriteBatch batch, BobberBar bar, SpriteFont font)
        {
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;

            int width = 0, height = 120;


            float scale = 1.0f;


            int whitchFish = reflection.GetField<int>(bar, "whichFish").GetValue();
            int fishSize = reflection.GetField<int>(bar, "fishSize").GetValue();
            int fishQuality = reflection.GetField<int>(bar, "fishQuality").GetValue();
            bool treasure = reflection.GetField<bool>(bar, "treasure").GetValue();
            bool treasureCaught = reflection.GetField<bool>(bar, "treasureCaught").GetValue();
            float treasureAppearTimer = reflection.GetField<float>(bar, "treasureAppearTimer").GetValue() / 1000;
            
            SVObject fish = new SVObject(whitchFish, 1);

            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en)
            {
                scale = 0.7f;
            }

            string speciesText = TryFormat(translation.Get("fishinfo.species").ToString(), fish.DisplayName);
            string sizeText = TryFormat(translation.Get("fishinfo.size").ToString(), GetFinalSize(fishSize));
            string qualityText1 = translation.Get("fishinfo.quality").ToString();
            string qualityText2 = translation.Get(GetKeyForQuality(fishQuality)).ToString();
            string incomingText = TryFormat(translation.Get("fishinfo.treasure.incoming").ToString(), treasureAppearTimer);
            string appearedText = translation.Get("fishinfo.treasure.appear").ToString();
            string caughtText = translation.Get("fishinfo.treasure.caught").ToString();

            {
                Vector2 size = font.MeasureString(speciesText) * scale;
                if (size.X > width)
                {
                    width = (int)size.X;
                }
                height += (int)size.Y;
                size = font.MeasureString(sizeText) * scale;
                if (size.X > width)
                {
                    width = (int)size.X;
                }
                height += (int)size.Y;
                Vector2 temp = font.MeasureString(qualityText1);
                Vector2 temp2 = font.MeasureString(qualityText2);
                size = new Vector2(temp.X + temp2.X, Math.Max(temp.Y, temp2.Y));
                if(size.X > width)
                {
                    width = (int)size.X;
                }
                height += (int)size.Y;
            }

            if (treasure)
            {
                if (treasureAppearTimer > 0)
                {
                    Vector2 size = font.MeasureString(incomingText) * scale;
                    if (size.X > width)
                    {
                        width = (int)size.X;
                    }
                    height += (int)size.Y;
                }
                else
                {
                    if (!treasureCaught)
                    {
                        Vector2 size = font.MeasureString(appearedText) * scale;
                        if (size.X > width)
                        {
                            width = (int)size.X;
                        }
                        height += (int)size.Y;
                    }
                    else
                    {
                        Vector2 size = font.MeasureString(caughtText) * scale;
                        if (size.X > width)
                        {
                            width = (int)size.X;
                        }
                        height += (int)size.Y;
                    }
                }
            }

            width += 64;

            int x = bar.xPositionOnScreen + bar.width + 96;
            if (x + width > Game1.viewport.Width)
            {
                x = bar.xPositionOnScreen - width - 96;
            }
            int y = (int)Cap(bar.yPositionOnScreen, 0, Game1.viewport.Height - height);

            IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
            fish.drawInMenu(batch, new Vector2( x + width / 2 - 32, y + 16), 1.0f, 1.0f, 0.9f, false);


            Vector2 stringSize = font.MeasureString("X");
            Vector2 addition = new Vector2(0, stringSize.Y);
            
            Vector2 vec2 = new Vector2(x + 32, y + 80 + 16);
            DrawString(batch, font, ref vec2, speciesText, Color.Black, scale, false);
            DrawString(batch, font, ref vec2, sizeText, Color.Black, scale, false);
            DrawString(batch, font, ref vec2, qualityText1, Color.Black, scale, true);
            DrawString(batch, font, ref vec2, qualityText2, GetColorForQuality(fishQuality), scale);
            vec2.X = x + 32;
            if(treasure)
            {
                if(!treasureCaught)
                {
                    if (treasureAppearTimer > 0f)
                    {
                        DrawString(batch, font, ref vec2, incomingText, Color.Red, scale, false);
                    }
                    else
                    {
                        DrawString(batch, font, ref vec2, appearedText, Color.LightGoldenrodYellow, scale, false);
                    }
                }
                else
                {
                    DrawString(batch, font, ref vec2, caughtText, Color.ForestGreen, scale, false);
                }
            }
        }

        private string GetKeyForQuality(int fishQuality)
        {
            switch (fishQuality)
            {
                case 1: return "quality.silver";
                case 2: return "quality.gold";
                case 3: return "quality.iridium";
            }
            return "quality.normal";
        }

        private Color GetColorForQuality(int fishQuality)
        {
            switch (fishQuality)
            {
                case 1: return Color.AliceBlue;
                case 2: return Color.Tomato;
                case 3: return Color.Purple;
            }
            return Color.WhiteSmoke;
        }
        
        private float Round(float val, int exponent)
        {
            return (float)Math.Round(val, exponent, MidpointRounding.AwayFromZero);
        }
        private float Floor(float val, int exponent)
        {
            int e = 1;
            for(int i = 0;i < exponent;i++)
            {
                e *= 10;
            }
            return (float)Math.Floor(val * e) / e;
        }

        private void DrawString(SpriteBatch batch, SpriteFont font, ref Vector2 location, string text, Color color, float scale, bool next = false)
        {
            Vector2 stringSize = font.MeasureString(text) * scale;
            batch.DrawString(font, text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            if (next)
            {
                location += new Vector2(stringSize.X, 0);
            }
            else
            {
                location += new Vector2(0, stringSize.Y + 4);
            }
        }
        
        private int GetFinalSize(int inch)
        {
            return LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en ? inch : (int)Math.Round(inch * 2.54);
        }

        private string TryFormat(string str, params object[] args)
        {
            try
            {
                string ret = string.Format(str, args);
                return ret;
            }
            catch(Exception e)
            {
            }
            return "";
        }
        #endregion
    }
}
