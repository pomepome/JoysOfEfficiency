using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency.Utils
{
    using Player = Farmer;
    using SVObject = StardewValley.Object;
    public class Util
    {
        public static IModHelper Helper;
        public static IMonitor Monitor;
        private static bool catchingTreasure;

        private static MineIcons icons = new MineIcons();
        private static List<Monster> lastMonsters = new List<Monster>();
        private static string lastKilledMonster;

        private static List<T> GetObjectsWithin<T>(int radius) where T : SVObject
        {
            GameLocation location = Game1.player.currentLocation;
            Vector2 ov = Game1.player.getTileLocation();
            List<T> list = new List<T>();
            for(int dx = -radius; dx <= radius; dx++)
            {
                for(int dy = -radius; dy <= radius; dy++)
                {
                    Vector2 loc = ov + new Vector2(dx, dy);
                    if(location.Objects.ContainsKey(loc) && location.Objects[loc] is T t)
                    {
                        list.Add(t);
                    }
                }
            }
            return list;
        }

        private static Dictionary<Vector2, T> GetFeaturesWithin<T>(int radius) where T : TerrainFeature
        {
            GameLocation location = Game1.player.currentLocation;
            Vector2 ov = Game1.player.getTileLocation();
            Dictionary<Vector2, T> list = new Dictionary<Vector2, T>();

            List<LargeTerrainFeature> lFeatures = Game1.currentLocation.largeTerrainFeatures.ToList();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2 loc = ov + new Vector2(dx, dy);
                    if (location.terrainFeatures.ContainsKey(loc) && location.terrainFeatures[loc] is T t)
                    {
                        list.Add(loc, t);
                        continue;
                    }
                    else if(typeof(LargeTerrainFeature).IsAssignableFrom(typeof(T)))
                    {
                        foreach(LargeTerrainFeature feature in lFeatures)
                        {
                            if(feature is T&& feature.tilePosition.X == loc.X && feature.tilePosition.Y == loc.Y)
                            {
                                list.Add(loc, feature as T);
                            }
                        }
                    }
                }
            }
            return list;
        }

        public static void ShakeNearbyFruitedBush()
        {
            foreach (KeyValuePair<Vector2, Bush> bushes in GetFeaturesWithin<Bush>(ModEntry.Conf.AutoHarvestRadius))
            {
                Vector2 loc = bushes.Key;
                Bush bush = bushes.Value;

                if(!bush.townBush.Value && bush.tileSheetOffset.Value == 1 && bush.inBloom(Game1.currentSeason, Game1.dayOfMonth))
                {
                    bush.performUseAction(loc, Game1.currentLocation);
                }
            }
        }

        public static void ShakeNearbyFruitedTree()
        {
            foreach (KeyValuePair<Vector2, TerrainFeature> kv in GetFeaturesWithin<TerrainFeature>(ModEntry.Conf.AutoShakeRadius))
            {
                Vector2 loc = kv.Key;
                TerrainFeature feature = kv.Value;
                if (feature is Tree tree)
                {
                    if (tree.hasSeed.Value && !tree.stump.Value)
                    {
                        Helper.Reflection.GetMethod(tree, "shake").Invoke(loc, false);
                    }
                }
                if (feature is FruitTree ftree)
                {
                    if (ftree.growthStage.Value >= 4 && ftree.fruitsOnTree.Value > 0 && !ftree.stump.Value)
                    {
                        ftree.shake(loc, false);
                    }
                }
            }
        }

        public static void DigNearbyArtifactSpots()
        {
            Player player = Game1.player;

            int radius = ModEntry.Conf.AutoDigRadius;
            Hoe hoe = FindToolFromInventory<Hoe>(player);
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
                {
                    Game1.playSound("hoeHit");
                }
            }
        }

        public static void CollectNearbyCollectibles(GameLocation location)
        {
            foreach(SVObject obj in GetObjectsWithin<SVObject>(ModEntry.Conf.AutoCollectRadius))
            {
                if(obj.IsSpawnedObject || obj.isAnimalProduct())
                {
                    CollectObj(location, obj);
                }
            }
        }

        public static void HarvestNearCrabPot(Player player)
        {
            int radius = ModEntry.Conf.AutoHarvestRadius;

            foreach (CrabPot pot in GetObjectsWithin<CrabPot>(radius))
            {
                if (pot.readyForHarvest.Value)
                {
                    HarvestCrubPot(player, pot);
                }
            }
        }

        public static bool HarvestCrubPot(Player who, CrabPot obj, bool justCheckingForActivity = false)
        {
            IReflectionHelper reflection = ModEntry.ModHelper.Reflection;
            IReflectedField<bool> lidFlapping = reflection.GetField<bool>(obj, "lidFlapping");
            IReflectedField<float> lidFlapTimer = reflection.GetField<float>(obj, "lidFlapTimer");
            IReflectedField<Vector2> shake = reflection.GetField<Vector2>(obj, "shake");
            IReflectedField<float> shakeTimer = reflection.GetField<float>(obj, "shakeTimer");
            if (obj.tileIndexToShow == 714)
            {
                if (justCheckingForActivity)
                {
                    return true;
                }
                SVObject item = obj.heldObject.Value;
                obj.heldObject.Value = null;
                if (who.IsLocalPlayer && !who.addItemToInventoryBool(item, false))
                {
                    obj.heldObject.Value = item;
                    Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"), Color.Red, 3500f));
                    return false;
                }
                Dictionary<int, string> data = Game1.content.Load<Dictionary<int, string>>("Data\\Fish");
                if (data.ContainsKey(item.ParentSheetIndex))
                {
                    string[] rawData = data[item.ParentSheetIndex].Split('/');
                    int minFishSize = (rawData.Length <= 5) ? 1 : Convert.ToInt32(rawData[5]);
                    int maxFishSize = (rawData.Length > 5) ? Convert.ToInt32(rawData[6]) : 10;
                    who.caughtFish(item.ParentSheetIndex, Game1.random.Next(minFishSize, maxFishSize + 1));
                }
                obj.readyForHarvest.Value = false;
                obj.tileIndexToShow = 710;
                lidFlapping.SetValue(true);
                lidFlapTimer.SetValue(60f);
                obj.bait.Value = null;
                who.animateOnce(279 + who.FacingDirection);
                who.currentLocation.playSound("fishingRodBend");
                DelayedAction.playSoundAfterDelay("coin", 500, null);
                who.gainExperience(1, 5);
                shake.SetValue(Vector2.Zero);
                shakeTimer.SetValue(0f);
                return true;
            }
            if (obj.bait.Value == null)
            {
                if (justCheckingForActivity)
                {
                    return true;
                }
                if (Game1.player.addItemToInventoryBool(obj.getOne(), false))
                {
                    Game1.playSound("coin");
                    Game1.currentLocation.objects.Remove(obj.TileLocation);
                    return true;
                }
            }
            return false;
        }

        public static Vector2 FindLadder(MineShaft shaft)
        {
            for (int i = 0; i < shaft.Map.GetLayer("Buildings").LayerWidth; i++)
            {
                for (int j = 0; j < shaft.Map.GetLayer("Buildings").LayerHeight; j++)
                {
                    int index = shaft.getTileIndexAt(new Point(i, j), "Buildings");
                    Vector2 loc = new Vector2(i, j);
                    if (!shaft.Objects.ContainsKey(loc) && !shaft.terrainFeatures.ContainsKey(loc))
                    {
                        if (index == 171 || index == 173 || index == 174)
                            return loc;
                    }
                }
            }
            return Vector2.Zero;
        }

        public static void DrawMineGui(SpriteBatch batch, SpriteFont font, Player player, MineShaft shaft)
        {
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;
            int stonesLeft = reflection.GetField<NetIntDelta>(shaft, "netStonesLeftOnThisLevel").GetValue();
            Vector2 ladderPos = FindLadder(shaft);
            bool ladder = ladderPos != null && ladderPos != Vector2.Zero;

            List<Monster> currentMonsters = shaft.characters.OfType<Monster>().ToList();
            foreach (Monster mon in lastMonsters)
            {
                if (!currentMonsters.Contains(mon) && mon.Name != "ignoreMe")
                {
                    lastKilledMonster = mon.Name;
                }
            }
            lastMonsters = currentMonsters.ToList();
            string tallyStr = null;
            string ladderStr = null;
            if (lastKilledMonster != null)
            {
                int kills = Game1.stats.getMonstersKilled(lastKilledMonster);
                tallyStr = string.Format(translation.Get("monsters.tally"), lastKilledMonster, kills);
            }

            string stonesStr = null;
            if (stonesLeft == 0)
            {
                stonesStr = translation.Get("stones.none");
            }
            else
            {
                bool single = stonesLeft == 1;
                if (single)
                {
                    stonesStr = translation.Get("stones.one");
                }
                else
                {
                    stonesStr = string.Format(translation.Get("stones.many"), stonesLeft);
                }
            }
            if (ladder)
            {
                ladderStr = translation.Get("ladder");
            }
            icons.Draw(stonesStr, tallyStr, ladderStr);
        }


        public static void DrawFishingInfoBox(SpriteBatch batch, BobberBar bar, SpriteFont font)
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
                if (size.X > width)
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
            fish.drawInMenu(batch, new Vector2(x + width / 2 - 32, y + 16), 1.0f, 1.0f, 0.9f, false);


            Vector2 stringSize = font.MeasureString("X");
            Vector2 addition = new Vector2(0, stringSize.Y);

            Vector2 vec2 = new Vector2(x + 32, y + 96);
            DrawString(batch, font, ref vec2, speciesText, Color.Black, scale, false);
            DrawString(batch, font, ref vec2, sizeText, Color.Black, scale, false);
            DrawString(batch, font, ref vec2, qualityText1, Color.Black, scale, true);
            DrawString(batch, font, ref vec2, qualityText2, GetColorForQuality(fishQuality), scale);
            vec2.X = x + 32;
            if (treasure)
            {
                if (!treasureCaught)
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

        public static bool CollectObj(GameLocation loc, SVObject obj)
        {
            Player who = Game1.player;
            IEnumerable<KeyValuePair<Vector2, SVObject>> pairs = loc.Objects.Pairs.Where(kv => kv.Value == obj);
            if(pairs.Count() == 0)
            {
                return false;
            }
            Vector2 vector = pairs.First().Key;

            int quality = obj.Quality;
            Random random = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + (int)vector.X + (int)vector.Y * 777);
            if (who.professions.Contains(16) && obj.isForage(loc))
            {
                obj.Quality = 4;
            }
            else if (obj.isForage(loc))
            {
                if (random.NextDouble() < who.ForagingLevel / 30f)
                {
                    obj.Quality = 2;
                }
                else if (random.NextDouble() < who.ForagingLevel / 15f)
                {
                    obj.Quality = 1;
                }
            }
            if (obj.questItem.Value && obj.questId.Value != 0 && !who.hasQuest(obj.questId.Value))
            {
                return false;
            }
            if (who.couldInventoryAcceptThisItem(obj))
            {

                Monitor.Log($"picked up {obj.DisplayName} at [{vector.X},{vector.Y}]");
                if (who.IsLocalPlayer)
                {
                    loc.localSound("pickUpItem");
                    DelayedAction.playSoundAfterDelay("coin", 300, null);
                }
                who.animateOnce(279 + who.FacingDirection);
                if (!loc.isFarmBuildingInterior())
                {
                    if (obj.isForage(loc))
                    {
                        who.gainExperience(2, 7);
                    }
                }
                else
                {
                    who.gainExperience(0, 5);
                }
                who.addItemToInventoryBool(obj.getOne(), false);
                Game1.stats.ItemsForaged++;
                if (who.professions.Contains(13) && random.NextDouble() < 0.2 && !obj.questItem.Value && who.couldInventoryAcceptThisItem(obj) && !loc.isFarmBuildingInterior())
                {
                    who.addItemToInventoryBool(obj.getOne(), false);
                    who.gainExperience(2, 7);
                }
                loc.Objects.Remove(vector);
                return true;
            }
            obj.Quality = quality;
            return false;
        }

        public static bool IsThereAnyWaterNear(GameLocation location, Vector2 tileLocation)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 toCheck = tileLocation + new Vector2(i, j);
                    int x = (int)toCheck.X, y = (int)toCheck.Y;
                    if (location.doesTileHaveProperty(x, y, "Water", "Back") != null || location.doesTileHaveProperty(x, y, "WaterSource", "Back") != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void DestroyNearDeadCrops(Player player)
        {
            GameLocation location = player.currentLocation;
            foreach (KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>(1))
            {
                Vector2 loc = kv.Key;
                HoeDirt dirt = kv.Value;
                if (dirt.crop != null && dirt.crop.dead.Value)
                {
                    string name = new SVObject(dirt.crop.indexOfHarvest.Value, 1).DisplayName;
                    dirt.destroyCrop(loc, true, location);
                }
            }
        }

        public static void HarvestNearCrops(Player player)
        {
            GameLocation location = player.currentLocation;
            int radius = ModEntry.Conf.AutoHarvestRadius;
            foreach (KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>(radius))
            {
                Vector2 loc = kv.Key;
                HoeDirt dirt = kv.Value;
                if (dirt.crop == null)
                {
                    continue;
                }
                if (dirt.readyForHarvest())
                {
                    if (Harvest((int)loc.X, (int)loc.Y, dirt))
                    {
                        if (dirt.crop.regrowAfterHarvest.Value == -1 || dirt.crop.forageCrop.Value)
                        {
                            //destroy crop if it does not reqrow.
                            dirt.destroyCrop(loc, true, location);
                        }
                    }
                }
            }
        }

        public static void WaterNearbyCrops()
        {
            Player player = Game1.player;
            WateringCan can = FindToolFromInventory<WateringCan>(player);
            if (can != null)
            {
                bool watered = false;
                foreach (KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>(ModEntry.Conf.AutoWaterRadius))
                {
                    Vector2 location = kv.Key;
                    HoeDirt dirt = kv.Value;
                    float consume = 2 * (1.0f / (can.UpgradeLevel / 2.0f + 1));
                    if (dirt.crop != null && !dirt.crop.dead.Value && dirt.state.Value == 0 && player.Stamina >= consume && can.WaterLeft > 0)
                    {
                        dirt.state.Value = 1;
                        player.Stamina -= consume;
                        can.WaterLeft--;
                        watered = true;
                    }
                }
                if (watered)
                {
                    Game1.playSound("slosh");
                }
            }
        }

        public static void TryToggleGate(Player player)
        {
            GameLocation location = player.currentLocation;

            foreach (SerializableDictionary<Vector2, SVObject> dic in location.Objects)
            {
                foreach (KeyValuePair<Vector2, SVObject> kv in dic)
                {
                    Vector2 loc = kv.Key;
                    if (!(kv.Value is Fence fence) || !fence.isGate.Value)
                    {
                        continue;
                    }

                    IReflectedField<NetInt> fieldPosition = Helper.Reflection.GetField<NetInt>(fence, "gatePosition");

                    RectangleE bb = ExpandE(fence.getBoundingBox(loc), 3 * Game1.tileSize);
                    if (!bb.IsInternalPoint(player.Position.X, player.Position.Y))
                    {
                        //It won't work if the player is far away.
                        continue;
                    }

                    bool? isUpDown = IsUpsideDown(location, fence);
                    if (isUpDown == null)
                    {
                        if (!fence.getBoundingBox(loc).Intersects(player.GetBoundingBox()))
                        {
                            fieldPosition.SetValue(new NetInt(0));
                        }
                        continue;
                    }

                    int gatePosition = fence.gatePosition.Value;
                    bool flag = IsPlayerInClose(player, fence.TileLocation, isUpDown);


                    if (flag && gatePosition == 0)
                    {
                        fieldPosition.SetValue(new NetInt(88));
                        Game1.playSound("doorClose");
                    }
                    if (!flag && gatePosition >= 88)
                    {
                        fieldPosition.SetValue(new NetInt(0));
                        Game1.playSound("doorClose");
                    }
                }
            }
        }

        public static T FindToolFromInventory<T>(Player player) where T : Tool
        {
            if(player.CurrentTool is T t)
            {
                return t;
            }
            return player.Items.OfType<T>().FirstOrDefault();
        }

        public static bool Harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null)
        {
            IReflectionHelper reflection = Helper.Reflection;

            Multiplayer multiplayer = reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            Crop crop = soil.crop;
            if (crop.dead.Value)
            {
                if (junimoHarvester != null)
                {
                    return true;
                }
                return false;
            }
            if (crop.forageCrop.Value)
            {
                SVObject o = null;
                int experience2 = 3;
                int num = crop.whichForageCrop.Value;
                if (num == 1)
                {
                    o = new SVObject(399, 1, false, -1, 0);
                }
                if (Game1.player.professions.Contains(16))
                {
                    o.Quality = 4;
                }
                else if (Game1.random.NextDouble() < Game1.player.ForagingLevel / 30f)
                {
                    o.Quality = 2;
                }
                else if (Game1.random.NextDouble() < Game1.player.ForagingLevel / 15f)
                {
                    o.Quality = 1;
                }
                Game1.stats.ItemsForaged += (uint)o.Stack;
                if (junimoHarvester != null)
                {
                    junimoHarvester.tryToAddItemToHut(o);
                    return true;
                }
                if (Game1.player.addItemToInventoryBool(o, false))
                {
                    Vector2 initialTile2 = new Vector2(xTile, yTile);
                    Game1.player.animateOnce(279 + Game1.player.FacingDirection);
                    Game1.player.canMove = false;
                    Game1.player.currentLocation.playSound("harvest");
                    DelayedAction.playSoundAfterDelay("coin", 260, null);
                    if (crop.regrowAfterHarvest.Value == -1)
                    {
                        multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(17, new Vector2(initialTile2.X * 64f, initialTile2.Y * 64f), Color.White, 7, Game1.random.NextDouble() < 0.5, 125f, 0, -1, -1f, -1, 0));
                        multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(14, new Vector2(initialTile2.X * 64f, initialTile2.Y * 64f), Color.White, 7, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0));
                    }
                    Game1.player.gainExperience(2, experience2);
                    return true;
                }
            }
            else if (crop.currentPhase.Value >= crop.phaseDays.Count - 1 && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0))
            {
                int numToHarvest = 1;
                int cropQuality = 0;
                int fertilizerQualityLevel = 0;
                if (crop.indexOfHarvest.Value == 0)
                {
                    return true;
                }
                Random r = new Random(xTile * 7 + yTile * 11 + (int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame);
                switch (soil.fertilizer.Value)
                {
                    case 368:
                        fertilizerQualityLevel = 1;
                        break;
                    case 369:
                        fertilizerQualityLevel = 2;
                        break;
                }
                double chanceForGoldQuality = 0.2 * (Game1.player.FarmingLevel / 10.0) + 0.2 * (double)fertilizerQualityLevel * (((double)Game1.player.FarmingLevel + 2.0) / 12.0) + 0.01;
                double chanceForSilverQuality = Math.Min(0.75, chanceForGoldQuality * 2.0);
                if (r.NextDouble() < chanceForGoldQuality)
                {
                    cropQuality = 2;
                }
                else if (r.NextDouble() < chanceForSilverQuality)
                {
                    cropQuality = 1;
                }
                if (crop.minHarvest.Value > 1 || crop.maxHarvest.Value > 1)
                {
                    numToHarvest = r.Next(crop.minHarvest.Value, Math.Min(crop.minHarvest.Value + 1, crop.maxHarvest.Value + 1 + Game1.player.FarmingLevel / crop.maxHarvestIncreasePerFarmingLevel.Value));
                }
                if (crop.chanceForExtraCrops.Value > 0.0)
                {
                    while (r.NextDouble() < Math.Min(0.9, crop.chanceForExtraCrops.Value))
                    {
                        numToHarvest++;
                    }
                }
                if ((int)crop.harvestMethod.Value == 1)
                {
                    for (int j = 0; j < numToHarvest; j++)
                    {
                        Game1.createObjectDebris(crop.indexOfHarvest.Value, xTile, yTile, -1, cropQuality, 1f, null);
                    }
                    if (crop.regrowAfterHarvest.Value == -1)
                    {
                        return true;
                    }
                    crop.dayOfCurrentPhase.Value = crop.regrowAfterHarvest.Value;
                    crop.fullyGrown.Value = true;
                }
                else if (Game1.player.addItemToInventoryBool((crop.programColored.Value) ? new ColoredObject(crop.indexOfHarvest.Value, 1, crop.tintColor.Value)
                {
                    Quality = cropQuality
                } : new SVObject(crop.indexOfHarvest.Value, 1, false, -1, cropQuality), false))
                {
                    Vector2 initialTile = new Vector2(xTile, yTile);
                    if (junimoHarvester == null)
                    {
                        Game1.player.animateOnce(279 + Game1.player.FacingDirection);
                        Game1.player.canMove = false;
                    }
                    else
                    {
                        junimoHarvester.tryToAddItemToHut((crop.programColored.Value) ? new ColoredObject(crop.indexOfHarvest.Value, 1, crop.tintColor.Value)
                        {
                            Quality = cropQuality
                        } : new SVObject(crop.indexOfHarvest.Value, 1, false, -1, cropQuality));
                    }
                    if (r.NextDouble() < (double)((float)Game1.player.LuckLevel / 1500f) + Game1.dailyLuck / 1200.0 + 9.9999997473787516E-05)
                    {
                        numToHarvest *= 2;
                        if (junimoHarvester == null)
                        {
                            Game1.player.currentLocation.playSound("dwoop");
                        }
                        else if (Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), 64, junimoHarvester.currentLocation))
                        {
                            junimoHarvester.currentLocation.playSound("dwoop");
                        }
                    }
                    else if (crop.harvestMethod.Value == 0)
                    {
                        if (junimoHarvester == null)
                        {
                            Game1.player.currentLocation.playSound("harvest");
                        }
                        if (junimoHarvester == null)
                        {
                            DelayedAction.playSoundAfterDelay("coin", 260, Game1.player.currentLocation);
                        }
                        else if (Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), 64, junimoHarvester.currentLocation))
                        {
                            DelayedAction.playSoundAfterDelay("coin", 260, junimoHarvester.currentLocation);
                        }
                        if (crop.regrowAfterHarvest.Value == -1)
                        {
                            multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(17, new Vector2(initialTile.X * 64f, initialTile.Y * 64f), Color.White, 7, Game1.random.NextDouble() < 0.5, 125f, 0, -1, -1f, -1, 0));
                            multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(14, new Vector2(initialTile.X * 64f, initialTile.Y * 64f), Color.White, 7, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0));
                        }
                    }
                    if (crop.indexOfHarvest.Value == 421)
                    {
                        crop.indexOfHarvest.Value = 431;
                        numToHarvest = r.Next(1, 4);
                    }
                    for (int i = 0; i < numToHarvest - 1; i++)
                    {
                        Game1.createObjectDebris(crop.indexOfHarvest.Value, xTile, yTile, -1, 0, 1f, null);
                    }
                    int price = Convert.ToInt32(Game1.objectInformation[crop.indexOfHarvest.Value].Split('/')[1]);
                    float experience = (float)(16.0 * Math.Log(0.018 * (double)price + 1.0, 2.7182818284590451));
                    if (junimoHarvester == null)
                    {
                        Game1.player.gainExperience(0, (int)Math.Round((double)experience));
                    }
                    if (crop.regrowAfterHarvest.Value == -1)
                    {
                        return true;
                    }
                    crop.dayOfCurrentPhase.Value = crop.regrowAfterHarvest.Value;
                    crop.fullyGrown.Value = true;
                }
                else
                {
                }
            }
            return false;
        }

        public static bool? IsUpsideDown(GameLocation location, Fence fence)
        {
            int num2 = 0;
            Vector2 tileLocation = fence.TileLocation;
            int whichType = fence.whichType.Value;
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

            if (fence.isGate.Value)
            {
                if (num2 == 110)
                {
                    return true;
                }
                else if (num2 == 1500)
                {
                    return false;
                }
            }

            return null;
        }

        public static bool IsPlayerInClose(Player player, Vector2 fenceLocation, bool? isUpDown)
        {
            if (isUpDown == null)
            {
                return false;
            }
            Vector2 playerTileLocation = player.getTileLocation();
            if (isUpDown == true)
            {
                return (playerTileLocation.X == fenceLocation.X) && (playerTileLocation.Y <= fenceLocation.Y + 1 && playerTileLocation.Y >= fenceLocation.Y - 1);
            }
            return (playerTileLocation.X >= fenceLocation.X - 1 && playerTileLocation.X <= fenceLocation.X + 1) && (playerTileLocation.Y == fenceLocation.Y);
        }

        public static void TryToEatIfNeeded(Player player)
        {
            if (player.isEating)
            {
                return;
            }
            if (player.Stamina <= player.MaxStamina * ModEntry.Conf.StaminaToEatRatio || player.health <= player.maxHealth * ModEntry.Conf.HealthToEatRatio)
            {
                SVObject itemToEat = null;
                foreach (SVObject item in player.Items.OfType<SVObject>())
                {
                    if (item.Edibility > 0)
                    {
                        //It's a edible item
                        if (itemToEat == null || (itemToEat.Edibility / itemToEat.salePrice() < item.Edibility / item.salePrice()))
                        {
                            //Found good edibility per price or just first food
                            itemToEat = item;
                        }
                    }
                }
                if (itemToEat != null)
                {
                    player.eatObject(itemToEat);
                    itemToEat.Stack--;
                    if (itemToEat.Stack == 0)
                    {
                        player.removeItemFromInventory(itemToEat);
                    }
                }
            }
        }

        public static T FindToolFromInventory<T>(bool fromEntireInventory) where T : Tool
        {
            Player player = Game1.player;
            T find = null;
            if (player.CurrentTool is T)
            {
                return player.CurrentTool as T;
            }
            if (fromEntireInventory)
            {
                foreach (Item item in player.Items)
                {
                    if (item is T t)
                    {
                        find = t;
                        break;
                    }
                }
            }
            return find;
        }
        public static void AutoFishing(BobberBar bar)
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

            float top = barPos, bottom = barPos + barHeight;

            if (treasure && treasureApeearTimer <= 0 && !treasureCaught)
            {
                if (!catchingTreasure && distanceFromCatching > 0.7f)
                {
                    catchingTreasure = true;
                }
                if (catchingTreasure && distanceFromCatching < 0.3f)
                {
                    catchingTreasure = false;
                }
                if (catchingTreasure)
                {
                    fishPos = treasurePos;
                }
            }

            float strength = (fishPos - (barPos + barHeight / 2)) / 16f;
            float distance = fishPos - top;

            float threshold = Cap(ModEntry.Conf.CPUThresholdFishing, 0, 0.5f);
            if (distance < threshold * barHeight || distance > (1 - threshold) * barHeight)
            {
                bobberBarSpeed = strength;
            }

            bobberSpeed.SetValue(bobberBarSpeed);
        }

        public static float Cap(float f, float min, float max)
        {
            return f < min ? min : (f > max ? max : f);
        }

        public static string Format(string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static List<FarmAnimal> GetAnimalsList(Player player)
        {
            List<FarmAnimal> list = new List<FarmAnimal>();
            if (player.currentLocation is Farm farm)
            {
                foreach (SerializableDictionary<long, FarmAnimal> animal in farm.animals)
                {
                    foreach (KeyValuePair<long, FarmAnimal> kv in animal)
                    {
                        list.Add(kv.Value);
                    }
                }
            }
            else if (player.currentLocation is AnimalHouse house)
            {
                foreach (SerializableDictionary<long, FarmAnimal> animal in house.animals)
                {
                    foreach (KeyValuePair<long, FarmAnimal> kv in animal)
                    {
                        list.Add(kv.Value);
                    }
                }
            }
            return list;
        }

        public static void LetAnimalsInHome()
        {
            Farm farm = Game1.getFarm();
            foreach (SerializableDictionary<long, FarmAnimal> dic in farm.animals.ToList())
            {
                foreach (KeyValuePair<long, FarmAnimal> kv in dic)
                {
                    FarmAnimal animal = kv.Value;
                    animal.warpHome(farm, animal);
                }
            }
        }

        public static void ShowHUDMessage(string message, int duration = 3500)
        {
            HUDMessage hudMessage = new HUDMessage(message, 3)
            {
                noIcon = true,
                timeLeft = duration
            };
            Game1.addHUDMessage(hudMessage);
        }

        public static Rectangle Expand(Rectangle rect, int radius)
        {
            return new Rectangle(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        public static RectangleE ExpandE(Rectangle rect, int radius)
        {
            return new RectangleE(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        public static void DrawSimpleTextbox(SpriteBatch batch, string text, SpriteFont font, Item item = null)
        {
            Vector2 stringSize = font.MeasureString(text);
            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + (int)(Game1.tileSize * 1.5f);

            if (x < 0)
            {
                x = 0;
            }
            if (y < 0)
            {
                y = 0;
            }
            int rightX = (int)stringSize.X + Game1.tileSize / 2 + 8;
            if (item != null)
            {
                rightX += Game1.tileSize;
            }
            if (x + rightX > Game1.viewport.Width)
            {
                x = Game1.viewport.Width - rightX;
            }
            int bottomY = (int)stringSize.Y + 32;
            if (item != null)
            {
                bottomY += (int)(Game1.tileSize * 1.2);
            }
            if (bottomY + y > Game1.viewport.Height)
            {
                y = Game1.viewport.Height - bottomY;
            }
            IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, rightX, bottomY, Color.White, 1f, true);
            if (!string.IsNullOrEmpty(text))
            {
                Vector2 vector2 = new Vector2(x + Game1.tileSize / 4, y + bottomY / 2 - 10);
                batch.DrawString(font, text, vector2 + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, text, vector2 + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, text, vector2 + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, text, vector2, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            }
            item?.drawInMenu(batch, new Vector2(x + (int)stringSize.X + 24, y + 16), 1.0f, 1.0f, 0.9f, false);
        }

        public static string GetKeyForQuality(int fishQuality)
        {
            switch (fishQuality)
            {
                case 1: return "quality.silver";
                case 2: return "quality.gold";
                case 3: return "quality.iridium";
            }
            return "quality.normal";
        }

        public static Color GetColorForQuality(int fishQuality)
        {
            switch (fishQuality)
            {
                case 1: return Color.AliceBlue;
                case 2: return Color.Tomato;
                case 3: return Color.Purple;
            }
            return Color.WhiteSmoke;
        }

        public static float Round(float val, int exponent)
        {
            return (float)Math.Round(val, exponent, MidpointRounding.AwayFromZero);
        }
        public static float Floor(float val, int exponent)
        {
            int e = 1;
            for (int i = 0; i < exponent; i++)
            {
                e *= 10;
            }
            return (float)Math.Floor(val * e) / e;
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

        public static int GetFinalSize(int inch)
        {
            return LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en ? inch : (int)Math.Round(inch * 2.54);
        }

        public static string TryFormat(string str, params object[] args)
        {
            try
            {
                string ret = string.Format(str, args);
                return ret;
            }
            catch
            {
            }
            return "";
        }
    }
}
