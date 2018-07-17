using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework.Input;
using JoysOfEfficiency.ModCheckers;
using JoysOfEfficiency.Patches;

namespace JoysOfEfficiency
{

    using Player = StardewValley.Farmer;
    internal class ModEntry : Mod
    {
        public static bool IsCJBCheatsOn { get; private set; }
        public static bool IsCasksAnywhereOn { get; private set; }

        public static Mod Instance { get; private set; }
        internal static Config Conf { get; private set; }
        
        private string hoverText;

        private static bool isNight;
        private static int ticks;

        public ModEntry()
        {
            Instance = this;
        }

        public override void Entry(IModHelper helper)
        {
            Util.Helper = helper;
            Util.Monitor = Monitor;
            Util.ModInstance = this;

            HarmonyPatcher.Init();

            Conf = helper.ReadConfig<Config>();
            GameEvents.EighthUpdateTick += OnGameUpdate;
            GameEvents.UpdateTick += OnGameTick;

            ControlEvents.KeyPressed += OnKeyPressed;

            SaveEvents.BeforeSave += OnBeforeSave;
            TimeEvents.AfterDayStarted += OnPostSave;

            GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHUD;
            GraphicsEvents.OnPostRenderHudEvent += OnPostRenderHUD;


            Conf.CpuThresholdFishing = Util.Cap(Conf.CpuThresholdFishing, 0, 0.5f);
            Conf.HealthToEatRatio = Util.Cap(Conf.HealthToEatRatio, 0, 0.8f);
            Conf.StaminaToEatRatio = Util.Cap(Conf.StaminaToEatRatio, 0, 0.8f);
            Conf.AutoCollectRadius = (int)Util.Cap(Conf.AutoCollectRadius, 1, 3);
            Conf.AutoHarvestRadius = (int)Util.Cap(Conf.AutoHarvestRadius, 1, 3);
            Conf.AutoPetRadius = (int)Util.Cap(Conf.AutoPetRadius, 1, 3);
            Conf.AutoWaterRadius = (int)Util.Cap(Conf.AutoWaterRadius, 1, 3);
            Conf.AutoDigRadius = (int)Util.Cap(Conf.AutoDigRadius, 1, 3);
            Conf.AutoShakeRadius = (int)Util.Cap(Conf.AutoShakeRadius, 1, 3);
            Conf.MachineRadius = (int)Util.Cap(Conf.MachineRadius, 1, 3);

            if (ModChecker.IsCJBCheatsLoaded(helper))
            {
                Monitor.Log("CJBCheatsMenu detected.");
                IsCJBCheatsOn = true;
            }
            if (ModChecker.IsCasksAnywhereLoaded(helper))
            {
                Monitor.Log("CasksAnywhere detected.");
                IsCasksAnywhereOn = true;
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
            if (!Context.IsWorldReady)
            {
                return;
            }
            Player player = Game1.player;
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
                if (player.CurrentTool is FishingRod rod)
                {
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
                if (Game1.currentLocation is MineShaft shaft)
                {
                    bool isFallingDownShaft = Helper.Reflection.GetField<bool>(shaft, "isFallingDownShaft").GetValue();
                    if (isFallingDownShaft)
                    {
                        return;
                    }
                }
                if (!Context.CanPlayerMove)
                {
                    return;
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
                                Vector2 loc = player.getTileLocation() + new Vector2(i, j);
                                if (location.Objects.ContainsKey(loc) && location.Objects[loc].ParentSheetIndex == 590 && !location.isTileHoeDirt(loc))
                                {
                                    Util.Log($"BURIED @[{loc.X},{loc.Y}]");
                                    location.digUpArtifactSpot((int) loc.X, (int) loc.Y, player);
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

            if(args.KeyPressed == Keys.H)
            {
                Util.ShowHUDMessage($"Hay:{Game1.getFarm().piecesOfHay}");
                Util.ShowHUDMessage($"Direction:{Game1.player.FacingDirection}");
            }
            if (args.KeyPressed == Conf.KeyShowMenu)
            {
                //Open Up Menu
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new JoeMenu(800, 548, this);
            }
            else if(args.KeyPressed == Conf.KeyToggleBlackList)
            {
                Util.ToggleBlacklistUnderCursor();
            }
        }

        private void OnPreRenderHUD(object sender, EventArgs args)
        {
            if (Game1.currentLocation is MineShaft shaft && Conf.MineInfoGui)
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

            if (Conf.FishingProbabilitiesInfo && Game1.player.CurrentTool is FishingRod rod && rod.isFishing)
            {
                Util.PrintFishingInfo(rod);
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
