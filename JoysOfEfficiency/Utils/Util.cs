using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace JoysOfEfficiency.Utils
{
    using Player = StardewValley.Farmer;
    using SVObject = StardewValley.Object;
    public class Util
    {
        internal static IModHelper Helper;
        internal static IMonitor Monitor;
        internal static ModEntry ModInstance;
        
        private static bool catchingTreasure;

        private static readonly MineIcons icons = new MineIcons();

        private static List<Monster> lastMonsters = new List<Monster>();
        private static readonly List<Vector2> flowerLocationProducingNectar = new List<Vector2>();
        private static string lastKilledMonster;

        public static void PetNearbyPets()
        {
            GameLocation location = Game1.currentLocation;
            Player player = Game1.player;

            Rectangle bb = Expand(player.GetBoundingBox(), ModEntry.Conf.AutoPetRadius * Game1.tileSize);

            foreach (Pet pet in location.characters.OfType<Pet>().Where(pet=>pet.GetBoundingBox().Intersects(bb)))
            {
                bool wasPet = Helper.Reflection.GetField<bool>(pet, "wasPetToday").GetValue();
                if (!wasPet)
                {
                    pet.checkAction(player, location); // Pet pet... lol
                }
            }
        }

        public static void TryToggleGate(Player player)
        {
            foreach (Fence fence in GetObjectsWithin<Fence>(2).Where(f => f.isGate))
            {
                Vector2 loc = fence.TileLocation;

                bool? isUpDown = IsUpsideDown(fence);
                if (isUpDown == null)
                {
                    if (!player.GetBoundingBox().Intersects(fence.getBoundingBox(loc)))
                    {
                        fence.gatePosition = 0;
                    }
                    continue;
                }

                int gatePosition = fence.gatePosition;
                bool flag = IsPlayerInClose(player, fence, fence.TileLocation, isUpDown);


                if (flag && gatePosition == 0)
                {
                    fence.gatePosition = 88;
                    Game1.playSound("doorClose");
                }
                if (!flag && gatePosition >= 88)
                {
                    fence.gatePosition = 0;
                    Game1.playSound("doorClose");
                }
            }
        }

        public static void DepositIngredientsToMachines()
        {
            Player player = Game1.player;
            if (player.CurrentItem == null || !(player.CurrentItem is SVObject item))
            {
                return;
            }
            foreach (SVObject obj in GetObjectsWithin<SVObject>(ModEntry.Conf.MachineRadius).Where(IsObjectMachine))
            {
                Vector2 loc = GetLocationOf(Game1.currentLocation, obj);
                if (obj.heldObject == null)
                {
                    bool flag = false;
                    bool accepted = obj.Name == "Furnace" ? CanFurnaceAcceptItem(item, player) : Utility.isThereAnObjectHereWhichAcceptsThisItem(Game1.currentLocation, item, (int)loc.X * Game1.tileSize, (int)loc.Y * Game1.tileSize);
                    if (obj is Cask cask)
                    {
                        if (ModEntry.IsCasksAnywhereOn)
                        {
                            if (CanCaskAcceptThisItem(cask, item))
                            {
                                flag = true;
                            }
                        }
                        else if (Game1.currentLocation is Cellar && accepted)
                        {
                            flag = true;
                        }
                    }
                    else if (accepted)
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        obj.performObjectDropInAction(item, false, player);
                        if (obj.Name != "Furnace" || item.getStack() == 0)
                        {
                            player.reduceActiveItemByOne();
                        }
                    }
                }
            }
        }

        public static void PullMachineResult()
        {
            Player player = Game1.player;
            foreach (SVObject obj in GetObjectsWithin<SVObject>(ModEntry.Conf.MachineRadius).Where(IsObjectMachine))
            {
                if (obj.readyForHarvest && obj.heldObject != null)
                {
                    Item item = obj.heldObject;
                    if (player.couldInventoryAcceptThisItem(item))
                        obj.checkForAction(player);
                }
            }
        }

        public static void ShakeNearbyFruitedBush()
        {
            foreach (KeyValuePair<Vector2, Bush> bushes in GetFeaturesWithin<Bush>(ModEntry.Conf.AutoHarvestRadius))
            {
                Vector2 loc = bushes.Key;
                Bush bush = bushes.Value;

                if (!bush.townBush && bush.tileSheetOffset == 1 && bush.inBloom(Game1.currentSeason, Game1.dayOfMonth))
                {
                    bush.performUseAction(loc);
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
                    if (tree.hasSeed && !tree.stump)
                    {
                        Helper.Reflection.GetMethod(tree, "shake").Invoke(loc, false);
                    }
                }
                if (feature is FruitTree ftree)
                {
                    if (ftree.growthStage >= 4 && ftree.fruitsOnTree > 0 && !ftree.stump)
                    {
                        ftree.shake(loc, false);
                    }
                }
            }
        }

        public static void CollectNearbyCollectibles()
        {
            foreach (SVObject obj in GetObjectsWithin<SVObject>(ModEntry.Conf.AutoCollectRadius).Where(obj=>obj.IsSpawnedObject))
            {
                CollectObj(Game1.currentLocation, obj);
            }
        }

        public static void WaterNearbyCrops()
        {
            Player player = Game1.player;
            WateringCan can = FindToolFromInventory<WateringCan>(ModEntry.Conf.FindCanFromInventory);
            if (can != null)
            {
                GetMaxCan(can);
                bool watered = false;
                foreach (KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>(ModEntry.Conf.AutoWaterRadius))
                {
                    HoeDirt dirt = kv.Value;
                    float consume = 2 * (1.0f / (can.UpgradeLevel / 2.0f + 1));
                    if (dirt.crop != null && !dirt.crop.dead && dirt.state == 0 && player.Stamina >= consume && can.WaterLeft > 0)
                    {
                        dirt.state = 1;
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


        public static void DestroyNearDeadCrops(Player player)
        {
            foreach (KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>(1))
            {
                Vector2 loc = kv.Key;
                HoeDirt dirt = kv.Value;
                if (dirt.crop != null && dirt.crop.dead)
                {
                    dirt.destroyCrop(loc);
                }
            }
        }

        public static void HarvestNearCrops(Player player)
        {
            UpdateNectarInfo();
            foreach (KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>(ModEntry.Conf.AutoHarvestRadius))
            {
                Vector2 loc = kv.Key;
                HoeDirt dirt = kv.Value;

                if (dirt.crop == null || IsBlackListed(dirt.crop))
                {
                    continue;
                }
                if (ModEntry.Conf.ProtectNectarProducingFlower && IsProducingNectar(loc))
                {
                    continue;
                }
                if (dirt.readyForHarvest())
                {
                    if (Harvest((int)loc.X, (int)loc.Y, dirt))
                    {
                        if (dirt.crop.regrowAfterHarvest == -1 || dirt.crop.forageCrop)
                        {
                            //destroy crop if it does not reqrow.
                            dirt.destroyCrop(loc);
                        }
                    }
                }
            }
        }

        public static void UpdateNectarInfo()
        {
            flowerLocationProducingNectar.Clear();
            foreach (KeyValuePair<Vector2, SVObject> kv in Game1.currentLocation.Objects.Where(pair => pair.Value.Name == "Bee House"))
            {
                Vector2 houseLoc = kv.Key;
                Vector2 flowerLoc = GetCropLocation(Utility.findCloseFlower(houseLoc));
                if ((int)flowerLoc.X != -1 && (int)flowerLoc.Y != -1 && !flowerLocationProducingNectar.Contains(flowerLoc))
                {
                    flowerLocationProducingNectar.Add(flowerLoc);
                }
            }
        }

        public static void DrawCursor()
        {
            if (!Game1.options.hardwareCursor)
            {
                Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.options.gamepadControls ? 44 : 0, 16, 16), Color.White, 0f, Vector2.Zero, Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
            }
        }

        public static void DrawMineGui(SpriteBatch batch, SpriteFont font, Player player, MineShaft shaft)
        {
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;
            int stonesLeft = reflection.GetField<int>(shaft, "stonesLeftOnThisLevel").GetValue();
            Vector2? ladderPos = FindLadder(shaft);
            bool ladder = ladderPos != null;

            List<Monster> currentMonsters = shaft.characters.OfType<Monster>().ToList();
            foreach (Monster mon in lastMonsters)
            {
                if (!currentMonsters.Contains(mon) && mon.name != "ignoreMe")
                {
                    lastKilledMonster = mon.name;
                    Log($"LastMonster set {mon.name}");
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
                stonesStr = single ? translation.Get("stones.one") : string.Format(translation.Get("stones.many"), stonesLeft);
            }
            if (ladder)
            {
                ladderStr = translation.Get("ladder");
            }
            icons.Draw(stonesStr, tallyStr, ladderStr);
        }

        public static int GetMaxCan(WateringCan can)
        {
            if(can == null)
            {
                return -1;
            }
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
            }
            return can.waterCanMax;
        }

        private static Vector2 GetCropLocation(Crop crop)
        {
            foreach (KeyValuePair<Vector2, TerrainFeature> kv in Game1.currentLocation.terrainFeatures)
            {
                if (kv.Value is HoeDirt dirt)
                {
                    if (dirt.crop != null && !dirt.crop.dead && dirt.crop == crop)
                    {
                        return kv.Key;
                    }
                }
            }
            return new Vector2(-1, -1);
        }


        private static bool CanCaskAcceptThisItem(Cask cask, Item dropIn)
        {
            if (cask.quality >= 4 || cask.heldObject != null)
            {
                return false;
            }
            if (dropIn is SVObject obj && obj.quality == 4)
            {
                return false;
            }
            switch (dropIn.parentSheetIndex)
            {
                case 426:
                case 424:
                case 348:
                case 459:
                case 303:
                case 346:
                    return true;
            }
            return false;
        }

        public static Vector2 GetLocationOf(GameLocation location, SVObject obj)
        {
            List<KeyValuePair<Vector2, SVObject>> pairs = location.Objects.Where(kv => kv.Value == obj).ToList();
            return !pairs.Any() ? new Vector2(-1, -1) : pairs.First().Key;
        }

        /// <summary>
        /// Is the dirt's crop is a flower and producing nectar
        /// </summary>
        /// <param name="dirt">tileLocation to evaluate</param>
        /// <returns></returns>
        private static bool IsProducingNectar(Vector2 dirt)
        {
            return flowerLocationProducingNectar.Contains(dirt);
        }

        public static Vector2? FindLadder(MineShaft shaft)
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
            return null;
        }

        public static void PrintFishingInfo(FishingRod rod)
        {
            GameLocation location = Game1.currentLocation;
            bool flag = false;
            if (location.fishSplashPoint != null)
            {
                Vector2 bobber = Helper.Reflection.GetField<Vector2>(rod, "bobber").GetValue();
                Rectangle rectangle = new Rectangle(location.fishSplashPoint.X * 64, location.fishSplashPoint.Y * 64, 64, 64);
                Rectangle value = new Rectangle((int)bobber.X - 80, (int)bobber.Y - 80, 64, 64);
                flag = rectangle.Intersects(value);
            }
            int clearWaterDistance = Helper.Reflection.GetField<int>(rod, "clearWaterDistance").GetValue();
            Dictionary<int, double> dict = GetFishes(Game1.currentLocation, rod.attachments[0]?.ParentSheetIndex ?? -1, clearWaterDistance + (flag ? 1 : 0), Game1.player);
            DrawProbBox(dict);
        }

        private static void DrawProbBox(Dictionary<int, double> probs)
        {
            SpriteBatch b = Game1.spriteBatch;
            Size size = GetProbBoxSize(probs);
            IClickableMenu.drawTextureBox(Game1.spriteBatch, ModEntry.Conf.ProbBoxX, ModEntry.Conf.ProbBoxY, size.Width, size.Height, Color.White);
            int square = (int)(Game1.tileSize / 1.5);
            int x = ModEntry.Conf.ProbBoxX + 8;
            int y = ModEntry.Conf.ProbBoxY + 16;
            SpriteFont font = Game1.dialogueFont;
            {
                foreach (KeyValuePair<int, double> kv in probs)
                {
                    string text = $"{kv.Value * 100:f1}%";
                    SVObject fish = new SVObject(kv.Key, 1);

                    fish.drawInMenu(b, new Vector2(x + 8, y), 1.0f);
                    Utility.drawTextWithShadow(b, text, font, new Vector2(x + 32 + square, y + 16), Color.Black);

                    y += square + 16;
                }
            }
        }

        private static Size GetProbBoxSize(Dictionary<int, double> probs)
        {
            int width = 16, height = 48;
            int square = (int)(Game1.tileSize / 1.5);
            SpriteFont font = Game1.dialogueFont;
            {
                foreach (KeyValuePair<int, double> kv in probs)
                {
                    string text = $"{kv.Value * 100:f1}%";
                    Vector2 textSize = font.MeasureString(text);
                    int w = square + (int)textSize.X + 64;
                    if (w > width)
                    {
                        width = w;
                    }
                    height += square + 16;
                }
            }
            return new Size(width, height);
        }

        private static Dictionary<int, double> GetFinalProbabilities(Dictionary<int, double> dict)
        {
            Dictionary<int, double> result = new Dictionary<int, double>();
            double ratio = 1.0;
            foreach (KeyValuePair<int, double> kv in dict)
            {
                double d = kv.Value * ratio;
                result.Add(kv.Key, d);
                ratio = ratio * (1 - kv.Value);
            }

            return result;
        }

        private static Dictionary<int, double> MagnifyProbabilities(Dictionary<int, double> dict, double ratio)
        {
            Dictionary<int, double> result = new Dictionary<int, double>();
            foreach (KeyValuePair<int, double> kv in dict)
                result.Add(kv.Key, kv.Value * ratio);

            return result;
        }

        private static Dictionary<K, V> ConcatDictionary<K, V>(Dictionary<K, V> a, Dictionary<K, V> b)
        {
            return a.Concat(b).ToDictionary(x => x.Key, x => x.Value);
        }

        private static Dictionary<int, double> GetFishes(GameLocation location, int bait, int waterDepth, Player who)
        {
            double sum = 0;
            Dictionary<int, double> dict;
            switch (location)
            {
                case Farm _:
                    dict = GetFishesFarm(waterDepth, who);
                    break;
                case MineShaft shaft:
                    dict = GetFishesMine(shaft, bait, waterDepth, who);
                    sum = dict.Sum(kv => kv.Value);
                    if (1 - sum >= 0.001f)
                    {
                        dict.Add(168, 1 - sum);
                    }
                    return dict;
                default:
                    dict = GetFishes(waterDepth, who);
                    break;
            }

            KeyValuePair<int, double>[] array = dict.ToArray();
            Array.Sort(array, new CustomSorter());

            Dictionary<int, double> dict2 =
                GetFinalProbabilities(array.ToDictionary(x => x.Key, x => x.Value)).OrderByDescending(kv => kv.Value)
                    .Where(kv => !IsGarbage(kv.Key)).ToDictionary(x => x.Key, x => x.Value);
            sum = dict2.Sum(kv => kv.Value);
            if (1 - sum >= 0.001f)
            {
                dict2.Add(168, 1 - sum);
            }
            return dict2;
        }

        private static bool IsGarbage(int index)
        {
            if (index >= 167 && index <= 172)
            {
                return true;
            }
            switch (index)
            {
                case 152:
                case 153:
                case 157: return true;
            }
            return false;
        }

        private static Dictionary<int, double> GetFishes(int waterDepth, Player who, string locationName = null)
        {
            Dictionary<int, double> dict = new Dictionary<int, double>();

            Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\Locations");
            string key = locationName ?? Game1.currentLocation.Name;
            if (key.Equals("WitchSwamp") && !who.mailReceived.Contains("henchmanGone") && !who.hasItemInInventory(308, 1))
            {
                return new Dictionary<int, double>
                {
                    {308,0.25}
                };
            }
            if (dictionary.ContainsKey(key))
            {
                string[] array = dictionary[key].Split('/')[4 + Utility.getSeasonNumber(Game1.currentSeason)].Split(' ');
                Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                if (array.Length > 1)
                {
                    for (int i = 0; i < array.Length; i += 2)
                    {
                        dictionary2.Add(array[i], array[i + 1]);
                    }
                }
                string[] array2 = dictionary2.Keys.ToArray();
                Dictionary<int, string> dictionary3 = Game1.content.Load<Dictionary<int, string>>("Data\\Fish");
                Utility.Shuffle(Game1.random, array2);
                foreach (string t in array2)
                {
                    bool flag2 = true;
                    string[] array3 = dictionary3[Convert.ToInt32(t)].Split('/');
                    string[] array4 = array3[5].Split(' ');
                    int num2 = Convert.ToInt32(dictionary2[t]);
                    if (num2 == -1 || Game1.currentLocation.getFishingLocation(who.getTileLocation()) == num2)
                    {
                        int num3 = 0;
                        while (num3 < array4.Length)
                        {
                            if (Game1.timeOfDay < Convert.ToInt32(array4[num3]) || Game1.timeOfDay >= Convert.ToInt32(array4[num3 + 1]))
                            {
                                num3 += 2;
                                continue;
                            }
                            flag2 = false;
                            break;
                        }
                    }
                    if (!array3[7].Equals("both"))
                    {
                        if (array3[7].Equals("rainy") && !Game1.isRaining)
                        {
                            flag2 = true;
                        }
                        else if (array3[7].Equals("sunny") && Game1.isRaining)
                        {
                            flag2 = true;
                        }
                    }
                    if (who.FishingLevel < Convert.ToInt32(array3[12]))
                    {
                        flag2 = true;
                    }

                    if (flag2)
                        continue;

                    double num4 = Convert.ToDouble(array3[10]);
                    double num5 = Convert.ToDouble(array3[11]) * num4;
                    num4 -= Math.Max(0, Convert.ToInt32(array3[9]) - waterDepth) * num5;
                    num4 += who.FishingLevel / 50f;
                    num4 = Math.Min(num4, 0.89999997615814209);
                    int num = Convert.ToInt32(t);

                    dict.Add(num, num4);
                }
            }
            return dict;
        }

        private static Dictionary<int, double> GetFishesMine(MineShaft shaft, int bait, int waterDepth, Player who)
        {
            Dictionary<int, double> dict = new Dictionary<int, double>();
            double num2 = 1.0;
            num2 += 0.4 * who.FishingLevel;
            num2 += waterDepth * 0.1;
            double p = 0;
            int level = shaft.getMineArea();
            switch (level)
            {
                case 0:
                case 10:
                    num2 += (bait == 689) ? 3 : 0;
                    p = 0.02 + 0.01 * num2;
                    dict.Add(158, p);
                    break;
                case 40:
                    num2 += (bait == 682) ? 3 : 0;
                    p = 0.015 + 0.009 * num2;
                    dict.Add(161, p);
                    break;
                case 80:
                    num2 += (bait == 684) ? 3 : 0;
                    p = 0.01 + 0.008 * num2;
                    dict.Add(162, p);
                    break;
            }

            if (level == 10 || level == 40)
            {
                return ConcatDictionary(dict,
                    MagnifyProbabilities(
                        GetFishes(waterDepth, who, "UndergroundMine")
                            .Where(kv => !IsGarbage(kv.Key)).ToDictionary(x => x.Key, x => x.Value),
                        1 - p));
            }

            return dict;
        }

        private static Dictionary<int, double> GetFishesFarm(int waterDepth, Player who)
        {
            switch (Game1.whichFarm)
            {
                case 1:
                    return ConcatDictionary(MagnifyProbabilities(GetFishes(waterDepth, who, "Forest"), 0.3), MagnifyProbabilities(GetFishes(waterDepth, who, "Town"), 0.7));
                case 3:
                    return MagnifyProbabilities(GetFishes(waterDepth, who, "Forest"), 0.5);
                case 2:
                    {
                        double p = 0.05 + Game1.dailyLuck;
                        return ConcatDictionary(
                            new Dictionary<int, double> { { 734, p } },
                            MagnifyProbabilities(
                                GetFishes(waterDepth, who, "Forest"),
                                (1 - p) * 0.45)
                            );
                    }
                case 4:
                    {
                        return MagnifyProbabilities(
                            GetFishes(waterDepth, who, "Mountain"),
                            0.35);
                    }
                default:
                    return GetFishes(waterDepth, who);
            }
        }


        private static void CollectObj(GameLocation loc, SVObject obj)
        {
            Player who = Game1.player;
            List<KeyValuePair<Vector2, SVObject>> objs = loc.Objects.Where(kv => kv.Value == obj).ToList();
            if(!objs.Any())
            {
                return;
            }
            Vector2 vector = objs.First().Key;

            int quality = obj.quality;
            Random random = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + (int)vector.X + (int)vector.Y * 777);
            if (who.professions.Contains(16) && obj.isForage(loc))
            {
                obj.quality = 4;
            }
            else if (obj.isForage(loc))
            {
                if (random.NextDouble() < who.ForagingLevel / 30f)
                {
                    obj.quality = 2;
                }
                else if (random.NextDouble() < who.ForagingLevel / 15f)
                {
                    obj.quality = 1;
                }
            }
            if (who.couldInventoryAcceptThisItem(obj))
            {
                if (who.IsMainPlayer)
                {
                    Game1.playSound("pickUpItem");
                    DelayedAction.playSoundAfterDelay("coin", 300);
                }
                if(!who.isRidingHorse() && !who.ridingMineElevator)
                {
                    who.animateOnce(279 + who.FacingDirection);
                }

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
                who.addItemToInventoryBool(obj.getOne());
                Game1.stats.ItemsForaged++;
                if (who.professions.Contains(13) && random.NextDouble() < 0.2 && !obj.questItem && who.couldInventoryAcceptThisItem(obj) && !loc.isFarmBuildingInterior())
                {
                    who.addItemToInventoryBool(obj.getOne());
                    who.gainExperience(2, 7);
                }
                loc.objects.Remove(vector);
                return;
            }
            obj.quality = quality;
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

        public static void ToggleBlacklistUnderCursor()
        {
            GameLocation location = Game1.currentLocation;
            Vector2 tile = Game1.currentCursorTile;
            if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrain))
            {
                if (terrain is HoeDirt dirt)
                {
                    if (dirt.crop == null)
                    {
                        ShowHUDMessage("There is no crop under the cursor");
                    }
                    else
                    {
                        string name = "";
                        name = dirt.crop.forageCrop ? new SVObject(dirt.crop.whichForageCrop, 1).Name : new SVObject(dirt.crop.indexOfHarvest, 1).Name;
                        if (name == "")
                        {
                            return;
                        }
                        string text = "";
                        text = ToggleBlackList(dirt.crop) ? $"{name} has been added to AutoHarvest exception" : $"{name} has been removed from AutoHarvest exception";
                        ShowHUDMessage(text, 1000);
                        Monitor.Log(text);
                    }
                }
            }
        }

        public static bool Harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null)
        {
            Crop crop = soil.crop;

            if (crop.dead)
            {
                if (junimoHarvester != null)
                {
                    return true;
                }
                return false;
            }
            if (crop.forageCrop)
            {
                SVObject @object = null;
                const int howMuch = 3;
                int num = crop.whichForageCrop;
                if (num == 1)
                {
                    @object = new SVObject(399, 1);
                }
                if (@object == null)
                {
                    return false;
                }
                if (Game1.player.professions.Contains(16))
                {
                    @object.quality = 4;
                }
                else if (Game1.random.NextDouble() < Game1.player.ForagingLevel / 30f)
                {
                    @object.quality = 2;
                }
                else if (Game1.random.NextDouble() < Game1.player.ForagingLevel / 15f)
                {
                    @object.quality = 1;
                }
                
                {
                    Game1.stats.ItemsForaged += (uint) @object.Stack;
                    if (junimoHarvester != null)
                    {
                        junimoHarvester.tryToAddItemToHut(@object);
                        return true;
                    }

                    if (Game1.player.addItemToInventoryBool(@object))
                    {
                        Vector2 vector = new Vector2(xTile, yTile);
                        Game1.player.animateOnce(279 + Game1.player.facingDirection);
                        Game1.player.canMove = false;
                        Game1.playSound("harvest");
                        DelayedAction.playSoundAfterDelay("coin", 260);
                        if (crop.regrowAfterHarvest == -1)
                        {
                            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(17,
                                new Vector2(vector.X * Game1.tileSize, vector.Y * Game1.tileSize), Color.White, 7,
                                Game1.random.NextDouble() < 0.5, 125f));
                            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(14,
                                new Vector2(vector.X * Game1.tileSize, vector.Y * Game1.tileSize), Color.White, 7,
                                Game1.random.NextDouble() < 0.5, 50f));
                        }

                        Game1.player.gainExperience(2, howMuch);
                        return true;
                    }
                }
            }
            else if (crop.currentPhase >= crop.phaseDays.Count - 1 && (!crop.fullyGrown || crop.dayOfCurrentPhase <= 0))
            {
                int num2 = 1;
                int num3 = 0;
                int num4 = 0;
                if (crop.indexOfHarvest == 0)
                {
                    return true;
                }
                Random random = new Random(xTile * 7 + yTile * 11 + (int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame);
                switch (soil.fertilizer)
                {
                    case 368:
                        num4 = 1;
                        break;
                    case 369:
                        num4 = 2;
                        break;
                }
                double num5 = 0.2 * (Game1.player.FarmingLevel / 10.0) + 0.2 * num4 * ((Game1.player.FarmingLevel + 2.0) / 12.0) + 0.01;
                double num6 = Math.Min(0.75, num5 * 2.0);
                if (random.NextDouble() < num5)
                {
                    num3 = 2;
                }
                else if (random.NextDouble() < num6)
                {
                    num3 = 1;
                }
                if (crop.minHarvest > 1 || crop.maxHarvest > 1)
                {
                    num2 = random.Next(crop.minHarvest, Math.Min(crop.minHarvest + 1, crop.maxHarvest + 1 + Game1.player.FarmingLevel / crop.maxHarvestIncreasePerFarmingLevel));
                }
                if (crop.chanceForExtraCrops > 0.0)
                {
                    while (random.NextDouble() < Math.Min(0.9, crop.chanceForExtraCrops))
                    {
                        num2++;
                    }
                }
                if (crop.harvestMethod == 1)
                {
                    if (junimoHarvester == null)
                    {
                        DelayedAction.playSoundAfterDelay("daggerswipe", 150);
                    }
                    if (junimoHarvester != null && Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), Game1.tileSize, junimoHarvester.currentLocation))
                    {
                        Game1.playSound("harvest");
                    }
                    if (junimoHarvester != null && Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), Game1.tileSize, junimoHarvester.currentLocation))
                    {
                        DelayedAction.playSoundAfterDelay("coin", 260);
                    }
                    for (int i = 0; i < num2; i++)
                    {
                        if (junimoHarvester != null)
                        {
                            junimoHarvester.tryToAddItemToHut(new SVObject(crop.indexOfHarvest, 1, false, -1, num3));
                        }
                        else
                        {
                            Game1.createObjectDebris(crop.indexOfHarvest, xTile, yTile, -1, num3);
                        }
                    }
                    if (crop.regrowAfterHarvest == -1)
                    {
                        return true;
                    }
                    crop.dayOfCurrentPhase = crop.regrowAfterHarvest;
                    crop.fullyGrown = true;
                }
                else if (junimoHarvester != null || Game1.player.addItemToInventoryBool(crop.programColored ? new ColoredObject(crop.indexOfHarvest, 1, crop.tintColor)
                {
                    quality = num3
                } : new SVObject(crop.indexOfHarvest, 1, false, -1, num3)))
                {
                    Vector2 vector2 = new Vector2(xTile, yTile);
                    if (junimoHarvester == null)
                    {
                        Game1.player.animateOnce(279 + Game1.player.facingDirection);
                        Game1.player.canMove = false;
                    }
                    else
                    {
                        junimoHarvester.tryToAddItemToHut(crop.programColored ? new ColoredObject(crop.indexOfHarvest, 1, crop.tintColor)
                        {
                            quality = num3
                        } : new SVObject(crop.indexOfHarvest, 1, false, -1, num3));
                    }
                    if (random.NextDouble() < Game1.player.LuckLevel / 1500f + Game1.dailyLuck / 1200.0 + 9.9999997473787516E-05)
                    {
                        num2 *= 2;
                        if (junimoHarvester == null || Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), Game1.tileSize, junimoHarvester.currentLocation))
                        {
                            Game1.playSound("dwoop");
                        }
                    }
                    else if (crop.harvestMethod == 0)
                    {
                        if (junimoHarvester == null || Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), Game1.tileSize, junimoHarvester.currentLocation))
                        {
                            Game1.playSound("harvest");
                        }
                        if (junimoHarvester == null || Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), Game1.tileSize, junimoHarvester.currentLocation))
                        {
                            DelayedAction.playSoundAfterDelay("coin", 260);
                        }
                        if (crop.regrowAfterHarvest == -1 && (junimoHarvester == null || junimoHarvester.currentLocation.Equals(Game1.currentLocation)))
                        {
                            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(17, new Vector2(vector2.X * Game1.tileSize, vector2.Y * Game1.tileSize), Color.White, 7, Game1.random.NextDouble() < 0.5, 125f));
                            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(14, new Vector2(vector2.X * Game1.tileSize, vector2.Y * Game1.tileSize), Color.White, 7, Game1.random.NextDouble() < 0.5, 50f));
                        }
                    }
                    if (crop.indexOfHarvest == 421)
                    {
                        crop.indexOfHarvest = 431;
                        num2 = random.Next(1, 4);
                    }
                    for (int j = 0; j < num2 - 1; j++)
                    {
                        if (junimoHarvester == null)
                        {
                            Game1.createObjectDebris(crop.indexOfHarvest, xTile, yTile);
                        }
                        else
                        {
                            junimoHarvester.tryToAddItemToHut(new SVObject(crop.indexOfHarvest, 1));
                        }
                    }
                    int num7 = Convert.ToInt32(Game1.objectInformation[crop.indexOfHarvest].Split('/')[1]);
                    float num8 = (float)(16.0 * Math.Log(0.018 * num7 + 1.0, 2.7182818284590451));
                    if (junimoHarvester == null)
                    {
                        Game1.player.gainExperience(0, (int)Math.Round(num8));
                    }
                    if (crop.regrowAfterHarvest == -1)
                    {
                        return true;
                    }
                    crop.dayOfCurrentPhase = crop.regrowAfterHarvest;
                    crop.fullyGrown = true;
                }
            }
            return false;
        }

        public static void TryToEatIfNeeded(Player player)
        {
            if (Game1.isEating || Game1.activeClickableMenu != null)
            {
                return;
            }
            if (player.CurrentTool != null && player.CurrentTool is FishingRod rod)
            {
                if (rod.inUse() && !player.UsingTool)
                {
                    return;
                }
            }
            if (player.Stamina <= player.MaxStamina * ModEntry.Conf.StaminaToEatRatio || player.health <= player.maxHealth * ModEntry.Conf.HealthToEatRatio)
            {
                SVObject itemToEat = null;
                foreach (SVObject item in player.items.OfType<SVObject>())
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
                    Log("You ate {0}.", itemToEat.DisplayName);
                    Game1.playerEatObject(itemToEat);
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
            if (player.CurrentTool is T tool)
            {
                return tool;
            }
            T find = null;
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

        private static bool IsBlackListed(Crop crop)
        {
            int index = crop.forageCrop ? crop.whichForageCrop : crop.indexOfHarvest;
            return ModEntry.Conf.HarvestException.Contains(index);
        }

        private static bool ToggleBlackList(Crop crop)
        {
            int index = crop.forageCrop ? crop.whichForageCrop : crop.indexOfHarvest;
            if (IsBlackListed(crop))
                ModEntry.Conf.HarvestException.Remove(index);
            else
                ModEntry.Conf.HarvestException.Add(index);

            ModInstance.WriteConfig();
            return IsBlackListed(crop);
        }

        private static bool? IsUpsideDown(Fence fence)
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

            if (fence.isGate)
            {
                if (num2 == 110)
                {
                    return true;
                }
                if (num2 == 1500)
                {
                    return false;
                }
            }

            return null;
        }

        private static bool IsObjectMachine(SVObject obj)
        {
            if(obj is CrabPot)
            {
                return true;
            }
            if(!obj.bigCraftable)
            {
                return false;
            }
            switch (obj.Name)
            {
                case "Incubator":
                case "Slime Incubator":
                case "Keg":
                case "Preserves Jar":
                case "Cheese Press":
                case "Mayonnaise Machine":
                case "Loom":
                case "Oil Maker":
                case "Seed Maker":
                case "Crystalarium":
                case "Recycling Machine":
                case "Furnace":
                case "Charcoal Kiln":
                case "Slime Egg-Press":
                case "Cask":
                case "Bee House":
                case "Mushroom Box":
                case "Statue Of Endless Fortune":
                case "Statue Of Perfection":
                case "Tapper":
                    return true;
                default: return false;
            }
        }

        private static bool IsPlayerInClose(Player player, Fence fence, Vector2 fenceLocation, bool? isUpDown)
        {
            if (isUpDown == null)
            {
                return fence.getBoundingBox(fence.TileLocation).Intersects(player.GetBoundingBox());
            }
            Vector2 playerTileLocation = player.getTileLocation();
            if(playerTileLocation == fenceLocation)
            {
                return true;
            }
            if(!IsPlayerFaceOrBackToFence(isUpDown == true, player))
            {
                return false;
            }
            if (isUpDown == true)
            {
                return ExpandSpecific(fence.getBoundingBox(fenceLocation), 0, 16).Intersects(player.GetBoundingBox());
            }
            return ExpandSpecific(fence.getBoundingBox(fenceLocation), 16, 0).Intersects(player.GetBoundingBox());
        }

        private static Rectangle ExpandSpecific(Rectangle rect, int deltaX, int deltaY)
        {
            return new Rectangle(rect.X - deltaX, rect.Y - deltaY, rect.Width + deltaX * 2, rect.Height + deltaY * 2);
        }

        private static bool IsPlayerFaceOrBackToFence(bool isUpDown, Player player)
        {
            return isUpDown ? player.FacingDirection % 2 == 0 : player.FacingDirection % 2 == 1;
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

            float up = barPos;

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
            float distance = fishPos - up;

            float threshold = ModEntry.Conf.CpuThresholdFishing;
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

        public static void Log(string format, params object[] args)
        {
            Monitor.Log(Format(format, args));
        }

        public static void Error(string text)
        {
            Monitor.Log(text, LogLevel.Error);
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
                foreach (KeyValuePair<long, FarmAnimal> animal in farm.animals)
                {
                    list.Add(animal.Value);
                }
            }
            else if (player.currentLocation is AnimalHouse house)
            {
                foreach (KeyValuePair<long, FarmAnimal> animal in house.animals)
                {
                    list.Add(animal.Value);
                }
            }
            return list;
        }

        public static void LetAnimalsInHome()
        {
            Farm farm = Game1.getFarm();
            foreach (KeyValuePair<long, FarmAnimal> kv in farm.animals.ToList())
            {
                FarmAnimal animal = kv.Value;
                animal.warpHome(farm, animal);
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

        public static RectangleE ExpandE(Rectangle rect, float radius)
        {
            return new RectangleE(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        public static Rectangle Expand(Rectangle rect, int radius)
        {
            return new Rectangle(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        public static void DrawSimpleTextbox(SpriteBatch batch, string text, int x, int y, SpriteFont font, Item item = null)
        {
            Vector2 stringSize = font.MeasureString(text);
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
                bottomY = (int)(Game1.tileSize * 1.2) + 32;
            }
            if (bottomY + y > Game1.viewport.Height)
            {
                y = Game1.viewport.Height - bottomY;
            }
            IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, rightX, bottomY, Color.White);
            if (!string.IsNullOrEmpty(text))
            {
                Vector2 vector2 = new Vector2(x + Game1.tileSize / 4, y + bottomY / 2 - stringSize.Y / 2);
                Utility.drawTextWithShadow(batch, text, font, vector2, Color.Black);
            }
            item?.drawInMenu(batch, new Vector2(x + (int)stringSize.X + 24, y + 16), 1.0f, 1.0f, 0.9f, false);
        }

        public static void DrawSimpleTextbox(SpriteBatch batch, string text, SpriteFont font, Item item = null)
        {
            DrawSimpleTextbox(batch, text, Game1.getMouseX() + Game1.tileSize / 2, Game1.getMouseY() + Game1.tileSize / 2 + 16, font, item);
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

            Vector2 vec2 = new Vector2(x + 32, y + 80 + 16);
            DrawString(batch, font, ref vec2, speciesText, Color.Black, scale);
            DrawString(batch, font, ref vec2, sizeText, Color.Black, scale);
            DrawString(batch, font, ref vec2, qualityText1, Color.Black, scale, true);
            DrawString(batch, font, ref vec2, qualityText2, GetColorForQuality(fishQuality), scale);
            vec2.X = x + 32;
            if (!treasure)
                return;

            if (!treasureCaught)
            {
                if (treasureAppearTimer > 0f)
                {
                    DrawString(batch, font, ref vec2, incomingText, Color.Red, scale);
                }
                else
                {
                    DrawString(batch, font, ref vec2, appearedText, Color.LightGoldenrodYellow, scale);
                }
            }
            else
            {
                DrawString(batch, font, ref vec2, caughtText, Color.ForestGreen, scale);
            }
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
                // ignored
            }

            return "";
        }

        private static bool CanFurnaceAcceptItem(Item item, Player player)
        {
            if (player.getTallyOfObject(382, false) <= 0)
                return false;
            if (item.Stack < 5 && item.parentSheetIndex != 80 && item.parentSheetIndex != 82 && item.parentSheetIndex != 330)
                return false;
            switch (item.parentSheetIndex)
            {
                case 378:
                case 380:
                case 384:
                case 386:
                case 80:
                case 82:
                    break;
                default:
                    return false;
            }
            return true;
        }

        private static List<T> GetObjectsWithin<T>(int radius) where T : SVObject
        {
            GameLocation location = Game1.player.currentLocation;
            Vector2 ov = Game1.player.getTileLocation();
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

        private static Dictionary<Vector2, T> GetFeaturesWithin<T>(int radius) where T : TerrainFeature
        {
            GameLocation location = Game1.player.currentLocation;
            Vector2 ov = Game1.player.getTileLocation();
            Dictionary<Vector2, T> list = new Dictionary<Vector2, T>();
            List<LargeTerrainFeature> lFeatures = location.largeTerrainFeatures;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2 loc = ov + new Vector2(dx, dy);
                    if (location.terrainFeatures.ContainsKey(loc) && location.terrainFeatures[loc] is T t)
                    {
                        list.Add(loc, t);
                    }
                    else if (lFeatures != null && lFeatures.Count > 0 && typeof(LargeTerrainFeature).IsAssignableFrom(typeof(T)))
                    {
                        foreach (LargeTerrainFeature feature in lFeatures)
                        {
                            if (feature is T && (int)feature.tilePosition.X == (int)loc.X && (int)feature.tilePosition.Y == (int)loc.Y)
                            {
                                list.Add(loc, feature as T);
                            }
                        }
                    }
                }
            }
            return list;
        }
    }
}
