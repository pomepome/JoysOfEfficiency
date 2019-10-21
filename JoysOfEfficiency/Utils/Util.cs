using System;
using System.Collections.Generic;
using System.Linq;
using JoysOfEfficiency.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Layers;
using static System.String;
using static StardewValley.Game1;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace JoysOfEfficiency.Utils
{
    using Player = Farmer;
    using SVObject = Object;
    internal class Util
    {
        private static IMonitor Monitor => InstanceHolder.Monitor;
        private static IReflectionHelper Reflection => InstanceHolder.Reflection;
        private static ITranslationHelper Translation => InstanceHolder.Translation;


        private static int _lastItemIndex;

        #region Public EntryPoint

        public static void PetNearbyPets()
        {
            GameLocation location = currentLocation;
            Player player = Game1.player;

            Rectangle bb = Expand(player.GetBoundingBox(), InstanceHolder.Config.AutoPetRadius * tileSize);

            foreach (Pet pet in location.characters.OfType<Pet>().Where(pet => pet.GetBoundingBox().Intersects(bb)))
            {
                bool wasPet = Reflection.GetField<bool>(pet, "wasPetToday").GetValue();
                if (!wasPet)
                {
                    pet.checkAction(player, location); // Pet pet... lol
                }
            }
        }

        public static void ShakeNearbyFruitedBush()
        {
            int radius = InstanceHolder.Config.AutoShakeRadius;
            foreach (Bush bush in currentLocation.largeTerrainFeatures.OfType<Bush>())
            {
                Vector2 loc = bush.tilePosition.Value;
                Vector2 diff = loc - player.getTileLocation();
                if (Math.Abs(diff.X) > radius || Math.Abs(diff.Y) > radius)
                    continue;

                if (!bush.townBush.Value && bush.tileSheetOffset.Value == 1 && bush.inBloom(currentSeason, dayOfMonth))
                    bush.performUseAction(loc, currentLocation);
            }
        }

        public static void ShakeNearbyFruitedTree()
        {
            foreach (KeyValuePair<Vector2, TerrainFeature> kv in GetFeaturesWithin<TerrainFeature>(InstanceHolder.Config.AutoShakeRadius))
            {
                Vector2 loc = kv.Key;
                TerrainFeature feature = kv.Value;
                switch (feature)
                {
                    case Tree tree:
                        if (tree.hasSeed.Value && !tree.stump.Value)
                        {
                            if (!IsMultiplayer && player.ForagingLevel < 1)
                            {
                                break;
                            }
                            int num2;
                            switch (tree.treeType.Value)
                            {
                                case 3:
                                    num2 = 311;
                                    break;
                                case 1:
                                    num2 = 309;
                                    break;
                                case 2:
                                    num2 = 310;
                                    break;
                                case 6:
                                    num2 = 88;
                                    break;
                                default:
                                    num2 = -1;
                                    break;
                            }
                            if (currentSeason.Equals("fall") && tree.treeType.Value == 2 && dayOfMonth >= 14)
                            {
                                num2 = 408;
                            }
                            if (num2 != -1)
                                Reflection.GetMethod(tree, "shake").Invoke(loc, false);
                        }
                        break;
                    case FruitTree ftree:
                        if (ftree.growthStage.Value >= 4 && ftree.fruitsOnTree.Value > 0 && !ftree.stump.Value)
                        {
                            ftree.shake(loc, false);
                        }
                        break;
                }
            }
        }

        public static void DigNearbyArtifactSpots()
        {
            int radius = InstanceHolder.Config.AutoDigRadius;
            Hoe hoe = FindToolFromInventory<Hoe>(player, InstanceHolder.Config.FindHoeFromInventory);
            GameLocation location = player.currentLocation;
            if (hoe != null)
            {
                bool flag = false;
                for (int i = -radius; i <= radius; i++)
                {
                    for (int j = -radius; j <= radius; j++)
                    {
                        int x = player.getTileX() + i;
                        int y = player.getTileY() + j;
                        Vector2 loc = new Vector2(x, y);
                        if (location.Objects.ContainsKey(loc) && location.Objects[loc].ParentSheetIndex == 590 && !location.isTileHoeDirt(loc))
                        {
                            location.digUpArtifactSpot(x, y, player);
                            location.Objects.Remove(loc);
                            location.terrainFeatures.Add(loc, new HoeDirt());
                            flag = true;
                        }
                    }
                }
                if (flag)
                    playSound("hoeHit");
            }
        }

        public static void CollectNearbyCollectibles(GameLocation location)
        {
            foreach (SVObject obj in GetObjectsWithin<SVObject>(InstanceHolder.Config.AutoCollectRadius))
                if (obj.IsSpawnedObject || obj.isAnimalProduct())
                    CollectObj(location, obj);
        }

        

        
        
        

        

        public static void TryToggleGate(Player player)
        {
            foreach (Fence fence in GetObjectsWithin<Fence>(2).Where(f => f.isGate.Value))
            {
                Vector2 loc = fence.TileLocation;

                bool? isUpDown = IsUpsideDown(fence);
                if (isUpDown == null)
                {
                    if (!fence.getBoundingBox(loc).Intersects(player.GetBoundingBox()))
                    {
                        fence.gatePosition.Value = 0;
                    }
                    continue;
                }

                int gatePosition = fence.gatePosition.Value;
                bool flag = IsPlayerInClose(player, fence, fence.TileLocation, isUpDown);

                if (flag && gatePosition == 0)
                {
                    fence.gatePosition.Value = 88;
                    playSound("doorClose");
                }
                else if (!flag && gatePosition >= 88)
                {
                    fence.gatePosition.Value = 0;
                    playSound("doorClose");
                }
            }
        }

        #endregion

        #region Public Utility

        public static T FindToolFromInventory<T>(bool fromEntireInventory) where T : Tool
        {
            Player player = Game1.player;
            if (player.CurrentTool is T t)
            {
                return t;
            }
            if (!fromEntireInventory)
                return null;

            foreach (Item item in player.Items)
            {
                if (item is T t2)
                {
                    return t2;
                }
            }
            return null;
        }

        public static float Cap(float f, float min, float max)
        {
            return f < min ? min : (f > max ? max : f);
        }

        

        public static void ShowHudMessage(string message, int duration = 3500)
        {
            HUDMessage hudMessage = new HUDMessage(message, 3)
            {
                noIcon = true,
                timeLeft = duration
            };
            addHUDMessage(hudMessage);
        }

        public static IEnumerable<FarmAnimal> GetAnimalsList(Character player)
        {
            List<FarmAnimal> list = new List<FarmAnimal>();
            switch (player.currentLocation)
            {
                case Farm farm:
                {
                    list.AddRange(farm.animals.Values);
                    break;
                }

                case AnimalHouse house:
                {
                    list.AddRange(house.animals.Values);
                    break;
                }
            }
            return list;
        }

        public static Rectangle Expand(Rectangle rect, int radius)
        {
            return new Rectangle(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        public static void DrawSimpleTextbox(SpriteBatch batch, string text, int x, int y, SpriteFont font, object ctx, Item item = null)
        {
            Vector2 stringSize = text == null ? Vector2.Zero : font.MeasureString(text);
            if (x < 0)
            {
                x = 0;
            }
            if (y < 0)
            {
                y = 0;
            }

            if (ctx is OptionsElement)
            {
                y -= 64;
            }
            int rightX = (int)stringSize.X + tileSize / 2 + 8;
            if (item != null)
            {
                rightX += tileSize;
            }
            if (x + rightX > viewport.Width)
            {
                x = viewport.Width - rightX;
            }
            int bottomY = (int)stringSize.Y + 32;
            if (item != null)
            {
                bottomY = (int)(tileSize * 1.7);
            }
            if (bottomY + y > viewport.Height)
            {
                y = viewport.Height - bottomY;
            }
            DrawWindow(x, y, rightX, bottomY);
            if (!IsNullOrEmpty(text))
            {
                Vector2 vector2 = new Vector2(x + tileSize / 4, y + (bottomY - stringSize.Y) / 2 + 8f);
                Utility.drawTextWithShadow(batch, text, font, vector2, Color.Black);
            }
            item?.drawInMenu(batch, new Vector2(x + (int)stringSize.X + 24, y + 16), 1.0f, 1.0f, 0.9f, false);
        }

        public static void DrawSimpleTextbox(SpriteBatch batch, string text, SpriteFont font, object context, bool isIcon = false, Item item = null)
        {
            DrawSimpleTextbox(batch, text, getMouseX() + tileSize / 2, getMouseY() + (isIcon ? 24 : tileSize) + 24, font, context, item);
        }

        public static bool IsThereAnyWaterNear(GameLocation location, Vector2 tileLocation)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 toCheck = tileLocation + new Vector2(i, j);
                    int x = (int)toCheck.X, y = (int)toCheck.Y;
                    if (location.doesTileHaveProperty(x, y, "Water", "Back") != null || location.doesTileHaveProperty(x, y, "WaterSource", "Back") != null || location is BuildableGameLocation loc2 && loc2.buildings.Where(b => b.occupiesTile(toCheck)).Any(building => building.buildingType.Value == "Well"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static T FindToolFromInventory<T>(Player player, bool findFromInventory) where T : Tool
        {
            if (player.CurrentTool is T t)
            {
                return t;
            }
            return findFromInventory ? player.Items.OfType<T>().FirstOrDefault() : null;
        }

        public static List<T> GetObjectsWithin<T>(int radius) where T : SVObject
        {
            if (!Context.IsWorldReady || currentLocation?.Objects == null)
            {
                return new List<T>();
            }
            if (InstanceHolder.Config.BalancedMode)
            {
                radius = 1;
            }

            GameLocation location = player.currentLocation;
            Vector2 ov = player.getTileLocation();
            List<T> list = new List<T>();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2 loc = ov + new Vector2(dx, dy);
                    if (location.Objects.ContainsKey(loc) && location.Objects[loc] is T t)
                    {
                        list.Add(t);
                    }
                }
            }
            return list;
        }

        public static bool IsPlayerIdle()
        {
            if (paused || !shouldTimePass())
            {
                //When game is paused or time is stopped already. it's not idle.
                return false;
            }

            if (player.CurrentToolIndex != _lastItemIndex)
            {
                //When tool index changed, it's not idle.
                _lastItemIndex = player.CurrentToolIndex;
                return false;
            }

            if (player.isMoving() || player.UsingTool)
            {
                //When player is moving or is using tools, it's not idle of cause.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds an item into the player inventory.
        /// </summary>-0
        /// <param name="item">The item to push</param>
        /// <returns>Remaining stack number that couldn't be added</returns>
        public static int AddItemIntoInventory(Item item)
        {
            int oldStack = item.Stack;
            int remaining = oldStack;
            for (int i = 0; i < player.MaxItems; i++)
            {
                if (player.Items[i] == null || i >= player.Items.Count)
                {
                    remaining = 0;
                    break;
                }

                Item stack = player.Items[i];

                if (!stack.canStackWith(item))
                {
                    continue;
                }

                int toPut = Math.Min(remaining, stack.maximumStackSize() - stack.Stack);
                if (toPut > 0)
                {
                    remaining -= toPut;
                }

                if (remaining == 0)
                {
                    break;
                }
            }

            player.addItemToInventoryBool(item);
            if (activeClickableMenu is ItemGrabMenu && oldStack - remaining > 0)
            {
                // Draw item pickup hud because addItemToInventoryBool doesn't if ItemGrabMenu is opened.
                Item toShow = item.getOne();
                toShow.Stack = oldStack - remaining;
                DrawItemPickupHud(toShow);
            }

            return remaining;
        }

        public static Chest GetFridge()
        {
            if (!InstanceHolder.Config.CraftingFromChests)
            {
                return null;
            }
            int radius = InstanceHolder.Config.RadiusCraftingFromChests;
            if (InstanceHolder.Config.BalancedMode)
            {
                radius = 1;
            }

            if (!(currentLocation is FarmHouse house) || house.upgradeLevel < 1)
                return null;

            Layer layer = house.Map.GetLayer("Buildings");
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = player.getTileX() + dx;
                    int y = player.getTileY() + dy;
                    if (x >= 0 && y >= 0 && x < layer.TileWidth && y < layer.TileHeight && layer.Tiles[x, y]?.TileIndex == 173)
                    {
                        //It's the fridge sprite
                        return house.fridge.Value;
                    }
                }
            }
            return null;
        }

        public static List<Chest> GetNearbyChests(bool addFridge = true)
        {
            int radius = InstanceHolder.Config.BalancedMode ? 1 : InstanceHolder.Config.RadiusCraftingFromChests;
            List<Chest> chests = new List<Chest>();
            if (InstanceHolder.Config.CraftingFromChests)
            {
                foreach (Chest chest in GetObjectsWithin<Chest>(radius))
                {
                    chests.Add(chest);
                }
            }

            Chest fridge = GetFridge();
            if (addFridge && fridge != null)
            {
                chests.Add(fridge);
            }

            return chests;
        }

        public static List<Item> GetNearbyItems(Player player)
        {
            List<Item> items = new List<Item>(player.Items);
            foreach (Chest chest in GetNearbyChests())
            {
                if(chest != null)
                    items.AddRange(chest.items);
            }
            return items;
        }


        public static void DrawCursor()
        {
            if (!options.hardwareCursor)
                spriteBatch.Draw(mouseCursors, new Vector2(getOldMouseX(), getOldMouseY()),
                    getSourceRectForStandardTileSheet(mouseCursors, options.gamepadControls ? 44 : 0, 16, 16),
                    Color.White, 0f, Vector2.Zero, pixelZoom + dialogueButtonScale / 150f, SpriteEffects.None, 1f);
        }

        public static int GetMaxCan(WateringCan can)
        {
            if (can == null)
                return -1;
            switch (can.UpgradeLevel)
            {
                case 0:
                    can.waterCanMax = 40;
                    break;
                case 1:
                    can.waterCanMax = 55;
                    break;
                case 2:
                    can.waterCanMax = 70;
                    break;
                case 3:
                    can.waterCanMax = 85;
                    break;
                case 4:
                    can.waterCanMax = 100;
                    break;
                default:
                    return -1;
            }

            return can.waterCanMax;

        }

        public static void DrawShippingPrice(IClickableMenu menu, SpriteFont font)
        {
            if (!(menu is ItemGrabMenu grabMenu) || !(grabMenu.shippingBin || IsCaShippingBinMenu(grabMenu)))
            {
                return;
            }
            int shippingPrice = getFarm().shippingBin.Sum(item => GetTruePrice(item) / 2 * item.Stack);
            string title = Translation.Get("estimatedprice.title");
            string text = $" {shippingPrice}G";
            Vector2 sizeTitle = font.MeasureString(title) * 1.2f;
            Vector2 sizeText = font.MeasureString(text) * 1.2f;
            int width = Math.Max((int)sizeTitle.X, (int)sizeText.X) + 32;
            int height = 16 + (int)sizeTitle.Y + 8 + (int)sizeText.Y + 16;
            Vector2 basePos = new Vector2(menu.xPositionOnScreen - width, menu.yPositionOnScreen + menu.height / 4 - height);

            DrawWindow( (int)basePos.X, (int)basePos.Y, width, height);
            Utility.drawTextWithShadow(spriteBatch, title, font, basePos + new Vector2(16, 16), Color.Black, 1.2f);
            Utility.drawTextWithShadow(spriteBatch, text, font, basePos + new Vector2(16, 16 + (int)sizeTitle.Y + 8), Color.Black, 1.2f);
        }

        public static void DrawColoredBox(SpriteBatch batch, int x, int y, int width, int height, Color color)
        {
            batch.Draw(fadeToBlackRect, new Rectangle(x, y, width, height), color);
        }

        public static void DrawWindow(int x, int y, int width, int height)
        {
            IClickableMenu.drawTextureBox(spriteBatch, x, y, width, height, Color.White);
        }

        #endregion

        #region Private Utility

        public static Dictionary<Vector2, T> GetFeaturesWithin<T>(int radius) where T : TerrainFeature
        {
            if (!Context.IsWorldReady)
            {
                return new Dictionary<Vector2, T>();
            }
            GameLocation location = player.currentLocation;
            Vector2 ov = player.getTileLocation();
            Dictionary<Vector2, T> list = new Dictionary<Vector2, T>();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2 loc = ov + new Vector2(dx, dy);
                    if (location.terrainFeatures.ContainsKey(loc) && location.terrainFeatures[loc] is T t)
                    {
                        list.Add(loc, t);
                    }
                }
            }
            return list;
        }

        public static Vector2 GetLocationOf(GameLocation location, SVObject obj)
        {
            return location.Objects.Pairs.Any(kv => kv.Value == obj) ? location.Objects.Pairs.First(kv => kv.Value == obj).Key : new Vector2(-1, -1);
        }

        private static void CollectObj(GameLocation loc, SVObject obj)
        {
            Player who = player;

            Vector2 vector = GetLocationOf(loc, obj);

            if ((int)vector.X == -1 && (int)vector.Y == -1) return;
            if (obj.questItem.Value) return;

            int quality = obj.Quality;
            Random random = new Random((int)uniqueIDForThisGame / 2 + (int)stats.DaysPlayed + (int)vector.X + (int)vector.Y * 777);

            if (who.professions.Contains(16) && obj.isForage(loc))
                obj.Quality = 4;

            else if (obj.isForage(loc))
            {
                if (random.NextDouble() < who.ForagingLevel / 30f)
                    obj.Quality = 2;
                else if (random.NextDouble() < who.ForagingLevel / 15f)
                    obj.Quality = 1;
            }

            if (who.couldInventoryAcceptThisItem(obj))
            {
                Monitor.Log($"picked up {obj.DisplayName} at [{vector.X},{vector.Y}]");
                if (who.IsLocalPlayer)
                {
                    loc.localSound("pickUpItem");
                    DelayedAction.playSoundAfterDelay("coin", 300);
                }
                if (!who.isRidingHorse() && !who.ridingMineElevator)
                    who.animateOnce(279 + who.FacingDirection);

                if (!loc.isFarmBuildingInterior())
                {
                    if (obj.isForage(loc))
                        who.gainExperience(2, 7);
                }
                else
                    who.gainExperience(0, 5);

                who.addItemToInventoryBool(obj.getOne());
                stats.ItemsForaged++;
                if (who.professions.Contains(13) && random.NextDouble() < 0.2 && !obj.questItem.Value && who.couldInventoryAcceptThisItem(obj) && !loc.isFarmBuildingInterior())
                {
                    who.addItemToInventoryBool(obj.getOne());
                    who.gainExperience(2, 7);
                }
                loc.Objects.Remove(vector);
                return;
            }
            obj.Quality = quality;
        }

        public static bool IsCaShippingBinMenu(ItemGrabMenu menu)
        {
            return !menu.reverseGrab && menu.showReceivingMenu && menu.context is Farm;
        }

        
        

        private static void DrawItemPickupHud(Item item)
        {
            Color color = Color.WhiteSmoke;
            string text = item.DisplayName;

            if (item is Object obj2)
            {
                switch (obj2.Type)
                {
                    case "Arch":
                        color = Color.Tan;
                        text += content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1954");
                        break;
                    case "Fish":
                        color = Color.SkyBlue;
                        break;
                    case "Mineral":
                        color = Color.PaleVioletRed;
                        break;
                    case "Vegetable":
                        color = Color.PaleGreen;
                        break;
                    case "Fruit":
                        color = Color.Pink;
                        break;
                }
            }

            addHUDMessage(new HUDMessage(text, Math.Max(1, item.Stack), true, color, item));
        }

        private static int GetTruePrice(Item item)
        {
            return item is SVObject obj ? obj.sellToStorePrice() * 2 : item.salePrice();
        }

        /// <summary>
        /// Returns type of the gate
        /// </summary>
        /// <param name="fence">The fence</param>
        /// <returns>true for horizontal, false for vertical, null for invalid</returns>
        private static bool? IsUpsideDown(Fence fence)
        {
            int num2 = 0;
            Vector2 tileLocation = fence.TileLocation;
            int whichType = fence.whichType.Value;
            tileLocation.X += 1f;
            if (currentLocation.objects.ContainsKey(tileLocation) && currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 100;
            }
            tileLocation.X -= 2f;
            if (currentLocation.objects.ContainsKey(tileLocation) && currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 10;
            }
            tileLocation.X += 1f;
            tileLocation.Y += 1f;
            if (currentLocation.objects.ContainsKey(tileLocation) && currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 500;
            }
            tileLocation.Y -= 2f;
            if (currentLocation.objects.ContainsKey(tileLocation) && currentLocation.objects[tileLocation].GetType() == typeof(Fence) && ((Fence)currentLocation.objects[tileLocation]).countsForDrawing(whichType))
            {
                num2 += 1000;
            }

            if (fence.isGate.Value)
            {
                switch (num2)
                {
                    case 110:
                        return true;
                    case 1500:
                        return false;
                    default:
                        return null;
                }
            }
            return null;
        }

        private static bool IsPlayerInClose(Player player, Fence fence, Vector2 fenceLocation, bool? isUpDown)
        {
            if (isUpDown == null)
            {
                return fence.getBoundingBox(fence.TileLocation).Intersects(player.GetBoundingBox());
            }
            Vector2 playerTileLocation = player.getTileLocation();
            if (playerTileLocation == fenceLocation)
            {
                return true;
            }
            if (!IsPlayerFaceOrBackToFence(isUpDown == true, player))
            {
                return false;
            }
            return isUpDown.Value ? ExpandSpecific(fence.getBoundingBox(fenceLocation), 0, 16).Intersects(player.GetBoundingBox()) : ExpandSpecific(fence.getBoundingBox(fenceLocation), 16, 0).Intersects(player.GetBoundingBox());
        }

        private static Rectangle ExpandSpecific(Rectangle rect, int deltaX, int deltaY)
        {
            return new Rectangle(rect.X - deltaX, rect.Y - deltaY, rect.Width + deltaX * 2, rect.Height + deltaY * 2);
        }

        private static bool IsPlayerFaceOrBackToFence(bool isUpDown, Player player)
        {
            return isUpDown ? player.FacingDirection % 2 == 0 : player.FacingDirection % 2 == 1;
        }

        

        public static void DrawString(SpriteBatch batch, SpriteFont font, ref Vector2 location, string text, Color color, float scale, bool next = false)
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

        public static string TryFormat(string str, params object[] args)
        {
            try
            {
                string ret = Format(str, args);
                return ret;
            }
            catch
            {
                // ignored
            }

            return "";
        }


        #endregion
    }
}
