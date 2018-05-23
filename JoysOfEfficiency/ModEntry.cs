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
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using JoysOfEfficiency.Options;
using StardewValley.Monsters;

namespace JoysOfEfficiency
{

    using Player = StardewValley.Farmer;
    using SVObject = StardewValley.Object;
    public class ModEntry : Mod
    {
        public static Mod Instance { get; private set; }
        public static Config Conf { get; private set; }
        
        private string hoverText;
        private bool catchingTreasure = false;

        private bool MineInfoVisible = true;

        private List<Monster> lastMonsters = new List<Monster>();
        private string lastKilledMonster;

        public ModEntry()
        {
            Instance = this;
        }

        public override void Entry(IModHelper helper)
        {
            Conf = helper.ReadConfig<Config>();
            GameEvents.UpdateTick += OnGameUpdate;

            ControlEvents.KeyPressed += OnKeyPressed;

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
            if (Conf.AutoWaterNearbyCrops)
            {
                RectangleE bb = ExpandE(player.GetBoundingBox(), Conf.AutoWaterRadius * Game1.tileSize);
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
                if (can != null)
                {
                    bool watered = false;
                    foreach (KeyValuePair<Vector2, TerrainFeature> kv in player.currentLocation.terrainFeatures)
                    {
                        Vector2 location = kv.Key;
                        TerrainFeature tf = kv.Value;
                        Point centre = tf.getBoundingBox(location).Center;
                        if (bb.IsInternalPoint(centre.X, centre.Y) && tf is HoeDirt dirt)
                        {
                            if (dirt.crop != null && !dirt.crop.dead && dirt.state == 0 && player.Stamina >= 2 && can.WaterLeft > 0)
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
            }
            if (Conf.GiftInformation)
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
            if (Conf.AutoPetNearbyAnimals)
            {
                int radius = Conf.AutoPetRadius * Game1.tileSize;
                RectangleE bb = new RectangleE(player.position.X - radius, player.position.Y - radius, radius * 2, radius * 2);
                List<FarmAnimal> animalList = GetAnimalsList(player);
                foreach (FarmAnimal animal in animalList)
                {
                    if (bb.IsInternalPoint(animal.position.X, animal.position.Y) && !animal.wasPet)
                    {
                        if (Game1.timeOfDay >= 1900 && !animal.isMoving())
                        {
                            //Skipping Slept Animals
                            continue;
                        }
                        animal.pet(player);
                    }
                }
            }
            if (player.CurrentTool is FishingRod rod)
            {
                IReflectedField<int> whichFish = reflection.GetField<int>(rod, "whichFish");
                if (rod.isNibbling && !rod.isReeling && !rod.hit && !rod.pullingOutOfWater && !rod.fishCaught)
                {
                    if (Conf.AutoFishing)
                    {
                        rod.DoFunction(player.currentLocation, 1, 1, 1, player);
                        rod.hit = true;
                    }
                }
                if (Conf.MuchFasterBiting)
                {
                    rod.timeUntilFishingBite -= 1000;
                }
            }
            if(Conf.AutoGate)
            {
                TryToggleGate(player);
            }
            if(Conf.AutoEat)
            {
                TryToEatIfNeeded(player);
            }
            if(Conf.AutoHarvest)
            {
                HarvestNearCrops(player);
                HarvestNearCrabPot(player);
            }
            if(Conf.AutoDestroyDeadCrops)
            {
                DestroyNearDeadCrops(player);
            }
            if(Conf.AutoRefillWateringCan)
            {
                WateringCan can = null;
                foreach(Item item in player.Items)
                {
                    if(item is WateringCan wc && wc.WaterLeft < wc.waterCanMax)
                    {
                        can = wc;
                    }
                }
                if(can != null && IsThereAnyWaterNear(player.currentLocation, player.getTileLocation()))
                {
                    can.WaterLeft = can.waterCanMax;
                    Game1.playSound("slosh");
                    DelayedAction.playSoundAfterDelay("glug", 250);
                }
            }
            if (Conf.AutoCollectCollectibles)
            {
                Rectangle bb = Expand(player.GetBoundingBox(), Conf.AutoCollectRadius * Game1.tileSize);
                foreach (KeyValuePair<Vector2, SVObject> kv in player.currentLocation.Objects.ToList())
                {
                    Vector2 loc = kv.Key;
                    SVObject obj = kv.Value;
                    if (obj.IsSpawnedObject && bb.Intersects(obj.getBoundingBox(loc)))
                    {
                        CollectObj(player.currentLocation, loc, obj);
                    }
                }
            }
            if(Conf.AutoShakeFruitedTree)
            {
                Rectangle bb = Expand(player.GetBoundingBox(), Conf.AutoShakeRadius * Game1.tileSize);
                foreach (KeyValuePair<Vector2, TerrainFeature> kv in player.currentLocation.terrainFeatures)
                {
                    Vector2 loc = kv.Key;
                    TerrainFeature feature = kv.Value;
                    if(!bb.Intersects(feature.getBoundingBox(loc)))
                    {
                        continue;
                    }
                    if(feature is Tree tree)
                    {
                        if(tree.hasSeed && !tree.stump)
                        {
                            reflection.GetMethod(tree, "shake").Invoke(loc, false);
                        }
                    }
                    if(feature is FruitTree ftree)
                    {
                        if(ftree.growthStage >= 4 && ftree.fruitsOnTree > 0 && !ftree.stump)
                        {
                            ftree.shake(loc, false);
                        }
                    }
                }
            }
        }

        private void OnKeyPressed(object sender, EventArgsKeyPressed args)
        {
            if (!Context.IsPlayerFree || Game1.activeClickableMenu != null)
            {
                return;
            }
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;
            if (Conf.MineInfoGUI && args.KeyPressed == Conf.ToggleKeyMineGUI)
            {
                MineInfoVisible = !MineInfoVisible;
                if (MineInfoVisible)
                {
                    ShowHUDMessage(translation.Get("mineinfo.enabled"));
                }
                else
                {
                    ShowHUDMessage(translation.Get("mineinfo.disabled"));
                }
            }
            if (args.KeyPressed == Conf.KeyShowMenu)
            {
                //Open Up Menu
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new JOEMenu(800, 500, this);
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
                if (Conf.FishingInfo)
                {
                    DrawFishingInfoBox(Game1.spriteBatch, bar, Game1.dialogueFont);
                }
                if (Conf.AutoFishing)
                {
                    AutoFishing(bar);
                }
            }
            if (Game1.currentLocation is MineShaft shaft && Conf.MineInfoGUI)
            {
                DrawMineGui(Game1.spriteBatch, Game1.smallFont, Game1.player, shaft);
            }
        }

        private void OnBeforeSave(object sender, EventArgs args)
        {
            if(!Context.IsWorldReady || !Conf.AutoAnimalDoor)
            {
                return;
            }
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
            if (!Context.IsWorldReady || !Conf.AutoAnimalDoor)
            {
                return;
            }
            if(Game1.isRaining || Game1.isSnowing)
            {
                Log("Don't open because of rainy/snowy weather.");
                return;
            }
            if(Game1.IsWinter)
            {
                Log("Don't open because it's winter.");
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

        private bool HarvestCrubPot(Player who, CrabPot obj, bool justCheckingForActivity = false)
        {
            IReflectionHelper reflection = Helper.Reflection;
            IReflectedField<bool> lidFlapping = reflection.GetField<bool>(obj, "lidFlapping");
            IReflectedField<float> lidFlapTimer = reflection.GetField<float>(obj, "lidFlapTimer");
            IReflectedField<Vector2> shake = reflection.GetField<Vector2>(obj, "shake");
            IReflectedField<float> shakeTimer = reflection.GetField<float>(obj, "shakeTimer");
            if (obj.tileIndexToShow == 714)
            {
                if (obj.heldObject == null)
                {
                    return false;
                }
                if (justCheckingForActivity)
                {
                    return true;
                }
                if (who.IsMainPlayer && !who.addItemToInventoryBool(obj.heldObject, false))
                {
                    Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"), Color.Red, 3500f));
                    return false;
                }
                Dictionary<int, string> dictionary = Game1.content.Load<Dictionary<int, string>>("Data\\Fish");
                if (dictionary.ContainsKey(obj.heldObject.parentSheetIndex))
                {
                    string[] array = dictionary[obj.heldObject.parentSheetIndex].Split('/');
                    int minValue = (array.Length <= 5) ? 1 : Convert.ToInt32(array[5]);
                    int num = (array.Length > 5) ? Convert.ToInt32(array[6]) : 10;
                    who.caughtFish(obj.heldObject.parentSheetIndex, Game1.random.Next(minValue, num + 1));
                }
                obj.readyForHarvest = false;
                obj.heldObject = null;
                obj.tileIndexToShow = 710;
                lidFlapping.SetValue(true);
                lidFlapTimer.SetValue(60f);
                obj.bait = null;
                who.animateOnce(279 + who.FacingDirection);
                Game1.playSound("fishingRodBend");
                DelayedAction.playSoundAfterDelay("coin", 500);
                who.gainExperience(1, 5);
                shake.SetValue(Vector2.Zero);
                shakeTimer.SetValue(0f);
                return true;
            }
            if (obj.bait == null)
            {
                if (justCheckingForActivity)
                {
                    return true;
                }
                if (Game1.player.addItemToInventoryBool(obj.getOne(), false))
                {
                    Game1.playSound("coin");
                    Game1.currentLocation.objects.Remove(obj.tileLocation);
                    return true;
                }
            }
            return false;
        }

        private void DrawMineGui(SpriteBatch batch, SpriteFont font, Player player, MineShaft shaft)
        {
            if (!MineInfoVisible)
            {
                return;
            }
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;
            int stonesLeft = reflection.GetField<int>(shaft, "stonesLeftOnThisLevel").GetValue();
            bool ladder = reflection.GetField<bool>(shaft, "ladderHasSpawned").GetValue();

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
            if (lastKilledMonster != null && Game1.stats.specificMonstersKilled.ContainsKey(lastKilledMonster))
            {
                int kills = Game1.stats.specificMonstersKilled[lastKilledMonster];
                tallyStr = string.Format(translation.Get("monsters.tally"), lastKilledMonster, kills);
            }

            string stonesStr = "";
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
            Point winSize = GetSuggestedMineInfoGuiSize(Game1.smallFont, stonesStr, tallyStr, ladder, translation.Get("ladder"));
            IClickableMenu.drawTextureBox(batch, 32, 320, winSize.X, winSize.Y, Color.White);
            int x = 32 + 16, y = 320 + 24;
            Vector2 size = Game1.smallFont.MeasureString(stonesStr);
            Utility.drawTextWithShadow(batch, stonesStr, Game1.smallFont, new Vector2(x, y), Color.Black);
            y += (int)size.Y + 16;
            if (tallyStr != null)
            {
                Utility.drawTextWithShadow(batch, tallyStr, Game1.smallFont, new Vector2(x, y), Color.Black);
                y += (int)size.Y + 16;
            }
            if (ladder)
            {
                Utility.drawTextWithShadow(batch, translation.Get("ladder"), Game1.smallFont, new Vector2(x, y), Color.Red);
            }
        }

        private Point GetSuggestedMineInfoGuiSize(SpriteFont font, string stonesStr, string tallyStr, bool ladder, string ladderStr)
        {
            int x = 32, y = 32;
            {
                Vector2 size = font.MeasureString(stonesStr);
                if (size.X + 32 > x)
                {
                    x = (int)size.X + 32;
                }
                y += (int)size.Y + 16;
            }
            if (tallyStr != null)
            {
                Vector2 size = font.MeasureString(tallyStr);
                if (size.X + 32 > x)
                {
                    x = (int)size.X + 32;
                }
                y += (int)size.Y + 16;
            }
            if (ladder)
            {
                Vector2 size = font.MeasureString(ladderStr);
                if (size.X + 32 > x)
                {
                    x = (int)size.X + 32;
                }
                y += (int)size.Y + 16;
            }
            return new Point(x, y);
        }

        private bool CollectObj(GameLocation loc, Vector2 vector, SVObject obj)
        {
            Player who = Game1.player;

            int quality = obj.quality;
            Random random = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + (int)vector.X + (int)vector.Y * 777);
            if (who.professions.Contains(16) && obj.isForage(loc))
            {
                obj.quality = 4;
            }
            else if (obj.isForage(loc))
            {
                if (random.NextDouble() < (double)((float)who.ForagingLevel / 30f))
                {
                    obj.quality = 2;
                }
                else if (random.NextDouble() < (double)((float)who.ForagingLevel / 15f))
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
                if (who.professions.Contains(13) && random.NextDouble() < 0.2 && !obj.questItem && who.couldInventoryAcceptThisItem(obj) && !loc.isFarmBuildingInterior())
                {
                    who.addItemToInventoryBool(obj.getOne(), false);
                    who.gainExperience(2, 7);
                }
                loc.objects.Remove(vector);
                return true;
            }
            obj.quality = quality;
            return false;
        }

        private bool IsThereAnyWaterNear(GameLocation location, Vector2 tileLocation)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 toCheck = tileLocation + new Vector2(i, j);
                    int x = (int)toCheck.X, y = (int)toCheck.Y;
                    if(location.doesTileHaveProperty(x, y, "Water", "Back") != null || location.doesTileHaveProperty(x, y, "WaterSource", "Back") != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void DestroyNearDeadCrops(Player player)
        {
            GameLocation location = player.currentLocation;
            int radius = 2 * Game1.tileSize;
            Rectangle bb = new Rectangle((int)player.position.X - radius, (int)player.position.Y - radius, 2 * radius, 2 * radius);
            foreach (KeyValuePair<Vector2, TerrainFeature> kv in location.terrainFeatures)
            {
                Vector2 loc = kv.Key;
                if(kv.Value is HoeDirt dirt)
                {
                    if(!bb.Intersects(dirt.getBoundingBox(loc)))
                    {
                        continue;
                    }
                    if(dirt.crop != null && dirt.crop.dead)
                    {
                        dirt.destroyCrop(loc);
                    }
                }
            }
        }

        private void HarvestNearCrabPot(Player player)
        {
            GameLocation location = player.currentLocation;
            int radius = Conf.AutoHarvestRadius * Game1.tileSize;
            Rectangle bb = Expand(player.GetBoundingBox(), radius);

            foreach(KeyValuePair<Vector2, SVObject> kv in location.Objects.ToList())
            {
                Vector2 loc = kv.Key;
                SVObject obj = kv.Value;
                if(obj is CrabPot pot)
                {
                    if(pot.readyForHarvest)
                    {
                        HarvestCrubPot(player, pot);
                    }
                }
            }
        }

        private void HarvestNearCrops(Player player)
        {
            GameLocation location = player.currentLocation;
            int radius = Conf.AutoHarvestRadius * Game1.tileSize;
            Rectangle bb = new Rectangle((int)player.position.X - radius, (int)player.position.Y - radius, 2 * radius, 2 * radius);
            foreach (KeyValuePair<Vector2, TerrainFeature> kv in location.terrainFeatures)
            {
                Vector2 loc = kv.Key;
                if (kv.Value is HoeDirt dirt)
                {
                    if (!bb.Intersects(dirt.getBoundingBox(loc)) || dirt.crop == null)
                    {
                        continue;
                    }
                    if(dirt.readyForHarvest())
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
        }

        public bool Harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null)
        {
            IReflectionHelper reflection = Helper.Reflection;
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
                int howMuch = 3;
                int num = crop.whichForageCrop;
                if (num == 1)
                {
                    @object = new SVObject(399, 1, false, -1, 0);
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
                Game1.stats.ItemsForaged += (uint)@object.Stack;
                if (junimoHarvester != null)
                {
                    junimoHarvester.tryToAddItemToHut(@object);
                    return true;
                }
                if (Game1.player.addItemToInventoryBool(@object, false))
                {
                    Vector2 vector = new Vector2(xTile, yTile);
                    Game1.player.animateOnce(279 + Game1.player.facingDirection);
                    Game1.player.canMove = false;
                    Game1.playSound("harvest");
                    DelayedAction.playSoundAfterDelay("coin", 260);
                    if (crop.regrowAfterHarvest == -1)
                    {
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(17, new Vector2(vector.X * (float)Game1.tileSize, vector.Y * (float)Game1.tileSize), Color.White, 7, Game1.random.NextDouble() < 0.5, 125f, 0, -1, -1f, -1, 0));
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(14, new Vector2(vector.X * (float)Game1.tileSize, vector.Y * (float)Game1.tileSize), Color.White, 7, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0));
                    }
                    Game1.player.gainExperience(2, howMuch);
                    return true;
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
                            Game1.createObjectDebris(crop.indexOfHarvest, xTile, yTile, -1, num3, 1f, null);
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
                } : new SVObject(crop.indexOfHarvest, 1, false, -1, num3), false))
                {
                    Vector2 vector2 = new Vector2((float)xTile, (float)yTile);
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
                    if (random.NextDouble() < (double)((float)Game1.player.LuckLevel / 1500f) + Game1.dailyLuck / 1200.0 + 9.9999997473787516E-05)
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
                            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(17, new Vector2(vector2.X * (float)Game1.tileSize, vector2.Y * (float)Game1.tileSize), Color.White, 7, Game1.random.NextDouble() < 0.5, 125f, 0, -1, -1f, -1, 0));
                            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(14, new Vector2(vector2.X * (float)Game1.tileSize, vector2.Y * (float)Game1.tileSize), Color.White, 7, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0));
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
                            Game1.createObjectDebris(crop.indexOfHarvest, xTile, yTile, -1, 0, 1f, null);
                        }
                        else
                        {
                            junimoHarvester.tryToAddItemToHut(new SVObject(crop.indexOfHarvest, 1, false, -1, 0));
                        }
                    }
                    int num7 = Convert.ToInt32(Game1.objectInformation[crop.indexOfHarvest].Split('/')[1]);
                    float num8 = (float)(16.0 * Math.Log(0.018 * (double)num7 + 1.0, 2.7182818284590451));
                    if (junimoHarvester == null)
                    {
                        Game1.player.gainExperience(0, (int)Math.Round((double)num8));
                    }
                    if (crop.regrowAfterHarvest == -1)
                    {
                        return true;
                    }
                    crop.dayOfCurrentPhase = crop.regrowAfterHarvest;
                    crop.fullyGrown = true;
                }
                else
                {
                }
            }
            return false;
        }

        private void TryToEatIfNeeded(Player player)
        {
            if(Game1.isEating)
            {
                return;
            }
            {
                bool flag = false;
                if (Conf.HealthToEatRatio < 0 || Conf.HealthToEatRatio > 0.8f)
                {
                    Conf.HealthToEatRatio = Cap(Conf.HealthToEatRatio, 0, 0.8f);
                    flag = true;
                }
                if (Conf.StaminaToEatRatio < 0 || Conf.StaminaToEatRatio > 0.8f)
                {
                    Conf.StaminaToEatRatio = Cap(Conf.StaminaToEatRatio, 0, 0.8f);
                    flag = true;
                }
                if(flag)
                {
                    Helper.WriteConfig(Conf);
                }
            }
            if(player.Stamina <= player.MaxStamina * Conf.StaminaToEatRatio || player.health <= player.maxHealth * Conf.HealthToEatRatio)
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

                RectangleE bb = ExpandE(fence.getBoundingBox(loc), 2.0f * Game1.tileSize);
                if (!bb.IsInternalPoint(player.Position))
                {
                    //It won't work if player is far away.
                    continue;
                }

                bool? isUpDown = IsUpsideDown(location, fence);
                if (isUpDown == null)
                {
                    if(!player.GetBoundingBox().Intersects(fence.getBoundingBox(loc)))
                    {
                        fence.gatePosition = 0;
                    }
                    continue;
                }

                int gatePosition = fence.gatePosition;
                bool flag = IsPlayerInClose(player, fence, fence.TileLocation, isUpDown);


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

        private bool IsPlayerInClose(Player player, Fence fence, Vector2 fenceLocation, bool? isUpDown)
        {
            if(isUpDown == null)
            {
                return Expand(player.GetBoundingBox(), 2 * Game1.tileSize).Intersects(player.GetBoundingBox());
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

            float threshold = Cap(Conf.CPUThresholdFishing, 0, 0.5f);
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

        private RectangleE ExpandE(Rectangle rect, float radius)
        {
            return new RectangleE(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        private Rectangle Expand(Rectangle rect, int radius)
        {
            return new Rectangle(rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius);
        }

        public static void DrawSimpleTextbox(SpriteBatch batch, string text, SpriteFont font, Item item)
        {
            Vector2 stringSize = font.MeasureString(text);
            int x = Game1.getMouseX() + Game1.tileSize / 2;
            int y = Game1.getMouseY() + Game1.tileSize / 2;

            if(x < 0)
            {
                x = 0;
            }
            if(y < 0)
            {
                y = 0;
            }
            int rightX = (int)stringSize.X + Game1.tileSize / 2 + 8;
            if(item != null)
            {
                rightX += Game1.tileSize;
            }
            if(x + rightX > Game1.viewport.Width)
            {
                x = Game1.viewport.Width - rightX;
            }
            int bottomY = (int)stringSize.Y;
            if(item != null)
            {
                bottomY += (int)(Game1.tileSize * 1.2);
            }
            bottomY = Math.Max(60, bottomY);
            if(bottomY + y > Game1.viewport.Height)
            {
                y = Game1.viewport.Height - bottomY;
            }
            IClickableMenu.drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, rightX, bottomY, Color.White, 1f, true);
            if(!string.IsNullOrEmpty(text))
            {
                Vector2 vector2 = new Vector2(x + Game1.tileSize / 4, y + bottomY / 2 - 10);
                batch.DrawString(font, text, vector2 + new Vector2(2f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, text, vector2 + new Vector2(0f, 2f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, text, vector2 + new Vector2(2f, 0f), Game1.textShadowColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                batch.DrawString(font, text, vector2, Game1.textColor * 0.9f, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            }
            item?.drawInMenu(batch, new Vector2(x + (int)stringSize.X + 24, y + 16), 1.0f,1.0f,0.9f,false);
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
            catch
            {
            }
            return "";
        }

        public void WriteConfig()
        {
            Helper.WriteConfig(Conf);
        }
        #endregion
    }
}
