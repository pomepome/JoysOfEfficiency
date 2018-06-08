using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley.Monsters;
using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework.Input;
using JoysOfEfficiency.ModCheckers;

namespace JoysOfEfficiency
{

    using Player = StardewValley.Farmer;
    using SVObject = StardewValley.Object;
    public class ModEntry : Mod
    {
        private static readonly int fpsCounterThreashold = 500;

        public static bool IsCJBCheatsOn { get; private set; } = false;

        public static Mod Instance { get; private set; }
        public static Config Conf { get; private set; }
        
        private string hoverText;

        private static bool isNight;
        private static int ticks;

        private double lastMillisec = 0;
        private int frameCount = 0;
        private double fps = 0;

        public ModEntry()
        {
            Instance = this;
        }

        public override void Entry(IModHelper helper)
        {
            Util.Helper = helper;
            Util.Monitor = Monitor;

            Conf = helper.ReadConfig<Config>();
            GameEvents.EighthUpdateTick += OnGameUpdate;
            GameEvents.UpdateTick += OnGameTick;

            ControlEvents.KeyPressed += OnKeyPressed;

            SaveEvents.BeforeSave += OnBeforeSave;
            TimeEvents.AfterDayStarted += OnPostSave;

            GraphicsEvents.OnPostRenderEvent += OnPostRender;
            GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHUD;
            GraphicsEvents.OnPostRenderHudEvent += OnPostRenderHUD;

            Conf.CPUThresholdFishing = Util.Cap(Conf.CPUThresholdFishing, 0, 0.5f);
            Conf.HealthToEatRatio = Util.Cap(Conf.HealthToEatRatio, 0, 0.8f);
            Conf.StaminaToEatRatio = Util.Cap(Conf.StaminaToEatRatio, 0, 0.8f);
            Conf.AutoCollectRadius = (int)Util.Cap(Conf.AutoCollectRadius, 1, 3);
            Conf.AutoHarvestRadius = (int)Util.Cap(Conf.AutoHarvestRadius, 1, 3);
            Conf.AutoPetRadius = (int)Util.Cap(Conf.AutoPetRadius, 1, 3);
            Conf.AutoWaterRadius = (int)Util.Cap(Conf.AutoWaterRadius, 1, 3);
            Conf.AutoDigRadius = (int)Util.Cap(Conf.AutoDigRadius, 1, 3);
            Conf.AutoShakeRadius = (int)Util.Cap(Conf.AutoShakeRadius, 1, 3);
            Conf.AddedSpeedMultiplier = (int)Util.Cap(Conf.AddedSpeedMultiplier, 1, 19);
            Conf.MachineRadius = (int)Util.Cap(Conf.MachineRadius, 1, 3);
            Conf.FPSlocation = (int)Util.Cap(Conf.FPSlocation, 0, 3);

            if(ModChecker.IsCJBCheatsLoaded(helper))
            {
                IsCJBCheatsOn = true;
                Monitor.Log("FasterRunningSpeed will be disabled since detected CJBCheatsMenu");

                Conf.FasterRunningSpeed = false;
            }

            helper.WriteConfig(Conf);

            MineIcons.Init(helper);
        }

        #region EventHandlers

        private void OnGameTick(object senderm, EventArgs args)
        {
            if (Game1.currentGameTime == null)
            {
                return;
            }
            if (lastMillisec == 0)
            {
                lastMillisec = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            }
            if (!Context.IsWorldReady)
            {
                return;
            }
            Player player = Game1.player;
            if (!IsCJBCheatsOn)
            {
                if (Conf.FasterRunningSpeed && player.running)
                {
                    player.addedSpeed = Conf.AddedSpeedMultiplier;
                }
                else
                {
                    player.addedSpeed = 0;
                }
                if (player.controller != null)
                {
                    player.addedSpeed = 0;
                }
            }
            if (Conf.AutoGate)
            {
                Util.TryToggleGate(player);
            }
        }

        private void OnGameUpdate(object sender, EventArgs args)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            Player player = Game1.player;
            IReflectionHelper reflection = Helper.Reflection;
            try
            {
                if (Conf.GiftInformation)
                {
                    hoverText = null;
                    if (player.CurrentItem != null && player.CurrentItem.canBeGivenAsGift())
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
                if (Conf.AutoWaterNearbyCrops)
                {
                    Util.WaterNearbyCrops();
                }
                if (Conf.AutoEat)
                {
                    Util.TryToEatIfNeeded(player);
                }
                if(Conf.AutoAnimalDoor && !isNight && Game1.timeOfDay >= 1900)
                {
                    isNight = true;
                    OnBeforeSave(null, null);
                }
                if (player.CurrentTool is FishingRod rod)
                {
                    IReflectedField<int> whichFish = reflection.GetField<int>(rod, "whichFish");
                    if (rod.isNibbling && !rod.isReeling && !rod.hit && !rod.pullingOutOfWater && !rod.fishCaught)
                    {
                        if (Conf.AutoReelRod)
                        {
                            rod.DoFunction(player.currentLocation, 1, 1, 1, player);
                        }
                    }
                    if (Conf.MuchFasterBiting && !rod.isNibbling && !rod.isReeling && !rod.hit && !rod.pullingOutOfWater && !rod.fishCaught)
                    {
                        rod.timeUntilFishingBite = 0;
                    }
                }
                if (Conf.AutoAnimalDoor && !isNight && Game1.timeOfDay >= 1900)
                {
                    isNight = true;
                    OnBeforeSave(null, null);
                }
                ticks = (ticks + 1) % 8;
                if(Conf.BalancedMode && ticks > 0)
                {
                    return;
                }
                if (Conf.AutoPetNearbyAnimals)
                {
                    int radius = Conf.AutoPetRadius * Game1.tileSize;
                    RectangleE bb = Util.ExpandE(player.GetBoundingBox(), radius);
                    List<FarmAnimal> animalList = Util.GetAnimalsList(player);
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
                if (Conf.AutoHarvest)
                {
                    Util.HarvestNearCrops(player);
                }
                if (Conf.AutoDestroyDeadCrops)
                {
                    Util.DestroyNearDeadCrops(player);
                }
                if (Conf.AutoRefillWateringCan)
                {
                    WateringCan can = Util.FindToolFromInventory<WateringCan>(Conf.FindCanFromInventory);
                    if (can != null && can.WaterLeft < Util.GetMaxCan(can) && Util.IsThereAnyWaterNear(player.currentLocation, player.getTileLocation()))
                    {
                        can.WaterLeft = can.waterCanMax;
                        Game1.playSound("slosh");
                        DelayedAction.playSoundAfterDelay("glug", 250);
                    }
                }
                if (Conf.AutoCollectCollectibles)
                {
                    Util.CollectNearbyCollectibles();
                }
                if (Conf.AutoDigArtifactSpot)
                {
                    int radius = Conf.AutoDigRadius;
                    Hoe hoe = Util.FindToolFromInventory<Hoe>(Conf.FindHoeFromInventory);
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
                                    Util.Log($"BURIED @[{x},{y}]");
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
                if (Conf.AutoShakeFruitedPlants)
                {
                    Util.ShakeNearbyFruitedTree();
                    Util.ShakeNearbyFruitedBush();
                }
                if (Conf.FastToolUpgrade && player.daysLeftForToolUpgrade > 1)
                {
                    player.daysLeftForToolUpgrade = 1;
                }
                if(Conf.AutoDepositIngredient)
                {
                    Util.DepositIngredientsToMachines();
                }
                if(Conf.AutoPullMachineResult)
                {
                    Util.PullMachineResult();
                }
                if(Conf.AutoPetNearbyPets)
                {
                    Util.PetNearbyPets();
                }
            }
            catch (Exception e)
            {
                Util.Error(e.ToString());
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
            if(args.KeyPressed == Keys.H)
            {
                Util.ShowHUDMessage($"Hay:{Game1.getFarm().piecesOfHay}");
                Util.ShowHUDMessage($"Direction:{Game1.player.FacingDirection}");
            }
            if (args.KeyPressed == Conf.KeyShowMenu)
            {
                Player player = Game1.player;
                //Open Up Menu
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new JOEMenu(800, 500, this);
            }
        }

        private void OnPostRender(object sender, EventArgs args)
        {
            frameCount++;
            double delta = Game1.currentGameTime.TotalGameTime.TotalMilliseconds - lastMillisec;
            if (delta >= fpsCounterThreashold)
            {
                lastMillisec = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
                fps = (double)frameCount * 1000 / delta;
                frameCount = 0;
            }
        }

        private void OnPreRenderHUD(object sender, EventArgs args)
        {
            if (Game1.currentLocation is MineShaft shaft && Conf.MineInfoGUI)
            {
                Util.DrawMineGui(Game1.spriteBatch, Game1.smallFont, Game1.player, shaft);
            }
        }

        private void OnPostRenderHUD(object sender, EventArgs args)
        {
            if(Context.IsPlayerFree && !string.IsNullOrEmpty(hoverText) && Game1.player.CurrentItem != null)
            {
                Util.DrawSimpleTextbox(Game1.spriteBatch, hoverText, Game1.smallFont, Game1.player.CurrentItem);
            }
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is BobberBar bar)
            {
                if (Conf.FishingInfo)
                {
                    Util.DrawFishingInfoBox(Game1.spriteBatch, bar, Game1.dialogueFont);
                }
                if (Conf.AutoFishing)
                {
                    Util.AutoFishing(bar);
                }
            }
            if (Conf.FPSCounter)
            {
                Point point = new Point();
                switch(Conf.FPSlocation)
                {
                    case 1: point = new Point(0,10000); break;
                    case 2: point = new Point(10000, 10000); break;
                    case 3: point = new Point(10000, 0); break;
                }
                Util.DrawSimpleTextbox(Game1.spriteBatch, string.Format("{0:f1}fps", fps), point.X, point.Y, Game1.smallFont);
            }
        }

        private void OnBeforeSave(object sender, EventArgs args)
        {
            if(!Context.IsWorldReady || !Conf.AutoAnimalDoor)
            {
                return;
            }
            Util.LetAnimalsInHome();

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
            isNight = false;
            if(Game1.isRaining || Game1.isSnowing)
            {
                Util.Log("Don't open the animal doors because of rainy/snowy weather.");
                return;
            }
            if(Game1.IsWinter)
            {
                Util.Log("Don't open the animal doors because it's winter.");
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

        public void WriteConfig()
        {
            Helper.WriteConfig(Conf);
        }

        #endregion
    }
}
