using JoysOfEfficiency.ModCheckers;
using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoysOfEfficiency
{
    using Player = Farmer;
    internal class ModEntry : Mod
    {
        public static bool IsCjbCheatsOn { get; private set; }
        public static bool IsCoGOn { get; private set; }

        public static Config Conf { get; private set; }

        public static IModHelper ModHelper { get; private set; }
        
        private string _hoverText;
        private bool _dayEnded;

        private int _ticks;

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            Util.Helper = helper;
            Util.Monitor = Monitor;
            Util.ModInstance = this;
            Conf = helper.ReadConfig<Config>();

            ControlEvents.KeyPressed += OnKeyPressed;

            GameEvents.UpdateTick += OnGameTick;
            GameEvents.EighthUpdateTick += OnGameUpdate;

            GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHud;
            GraphicsEvents.OnPostRenderHudEvent += OnPostRenderHud;
            
            SaveEvents.BeforeSave += OnBeforeSave;

            TimeEvents.AfterDayStarted += OnPostSave;


            Conf.CpuThresholdFishing = Util.Cap(Conf.CpuThresholdFishing, 0, 0.5f);
            Conf.HealthToEatRatio = Util.Cap(Conf.HealthToEatRatio, 0.3f, 0.8f);
            Conf.StaminaToEatRatio = Util.Cap(Conf.StaminaToEatRatio, 0.3f, 0.8f);
            Conf.AutoCollectRadius = (int)Util.Cap(Conf.AutoCollectRadius, 1, 3);
            Conf.AutoHarvestRadius = (int)Util.Cap(Conf.AutoHarvestRadius, 1, 3);
            Conf.AutoPetRadius = (int)Util.Cap(Conf.AutoPetRadius, 1, 3);
            Conf.AutoWaterRadius = (int)Util.Cap(Conf.AutoWaterRadius, 1, 3);
            Conf.AutoDigRadius = (int)Util.Cap(Conf.AutoDigRadius, 1, 3);
            Conf.AutoShakeRadius = (int)Util.Cap(Conf.AutoShakeRadius, 1, 3);
            Conf.MachineRadius = (int)Util.Cap(Conf.MachineRadius, 1, 3);

            if(ModChecker.IsCjbCheatsLoaded(helper))
            {
                IsCjbCheatsOn = true;
            }
            if(ModChecker.IsCoGLoaded(helper))
            {
                Monitor.Log("CasksOnGround detected.");
                IsCoGOn = true;
            }

            helper.WriteConfig(Conf);

            MineIcons.Init(helper);
        }

        private void OnGameTick(object senderm, EventArgs args)
        {
            _hoverText = null;
            if (!Context.IsWorldReady)
            {
                return;
            }
            Player player = Game1.player;
            if(Conf.AutoGate)
            {
                Util.TryToggleGate(player);
            }

            if (Conf.GiftInformation)
            {
                if (player.CurrentItem == null || !player.CurrentItem.canBeGivenAsGift())
                {
                    return;
                }

                List<NPC> npcList = player.currentLocation.characters.Where(a => a != null && a.isVillager()).ToList();
                foreach (NPC npc in npcList)
                {
                    RectangleE npcRect = new RectangleE(npc.position.X,
                        npc.position.Y - npc.Sprite.getHeight() - Game1.tileSize / 1.5f,
                        npc.Sprite.getWidth() * 3 + npc.Sprite.getWidth() / 1.5f, (npc.Sprite.getHeight() * 3.5f));

                    if (!npcRect.IsInternalPoint(Game1.getMouseX() + Game1.viewport.X,
                        Game1.getMouseY() + Game1.viewport.Y))
                    {
                        continue;
                    }

                    //Mouse hovered on the NPC
                    StringBuilder key = new StringBuilder("taste.");
                    switch (npc.getGiftTasteForThisItem(player.CurrentItem))
                    {
                        case 0:
                            key.Append("love.");
                            break;
                        case 2:
                            key.Append("like.");
                            break;
                        case 4:
                            key.Append("dislike.");
                            break;
                        case 6:
                            key.Append("hate.");
                            break;
                        default:
                            key.Append("neutral.");
                            break;
                    }

                    switch (npc.Gender)
                    {
                        case 0:
                            key.Append("male");
                            break;
                        default:
                            key.Append("female");
                            break;
                    }

                    Translation translation = Helper.Translation.Get(key.ToString());
                    _hoverText = translation?.ToString();
                }
            }
        }

        private void OnGameUpdate(object sender, EventArgs args)
        {
            if(Conf.BalancedMode)
            {
                Conf.MuchFasterBiting = false;
            }
            if(Game1.currentGameTime == null)
            {
                return;
            }
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
            {
                return;
            }
            Player player = Game1.player;
            GameLocation location = Game1.currentLocation;
            IReflectionHelper reflection = Helper.Reflection;
            try
            {
                if (player.CurrentTool is FishingRod rod && Game1.activeClickableMenu == null)
                {
                    IReflectedField<int> whichFish = reflection.GetField<int>(rod, "whichFish");
                    if (rod.isNibbling && rod.isFishing && whichFish.GetValue() == -1 && !rod.isReeling && !rod.hit && !rod.isTimingCast && !rod.pullingOutOfWater && !rod.fishCaught)
                    {
                        if (Conf.AutoReelRod)
                        {
                            rod.DoFunction(player.currentLocation, 1, 1, 1, player);
                        }
                    }
                    if (Conf.MuchFasterBiting && rod.isFishing && !rod.isNibbling && !rod.isReeling && !rod.hit && !rod.isTimingCast && !rod.pullingOutOfWater && !rod.fishCaught)
                    {
                        rod.timeUntilFishingBite -= 10000;
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
                if (Conf.AutoEat)
                {
                    Util.TryToEatIfNeeded(player);
                }
                _ticks = (_ticks + 1) % 8;
                if(Conf.BalancedMode && _ticks % 8 != 0)
                {
                    return;
                }

                if (Conf.AutoWaterNearbyCrops)
                {
                    Util.WaterNearbyCrops();
                }
                if (Conf.AutoPetNearbyAnimals)
                {
                    int radius = Conf.AutoPetRadius * Game1.tileSize;
                    Rectangle bb = Util.Expand(player.GetBoundingBox(), radius);
                    List<FarmAnimal> animalList = Util.GetAnimalsList(player);
                    foreach (FarmAnimal animal in animalList)
                    {
                        if (bb.Contains((int)animal.Position.X, (int)animal.Position.Y) && !animal.wasPet.Value)
                        {
                            if (Game1.timeOfDay >= 1900 && !animal.isMoving())
                            {
                                continue;
                            }
                            animal.pet(player);
                        }
                    }
                }
                if(Conf.AutoPullMachineResult)
                {
                    Util.PullMachineResult();
                }
                if(Conf.AutoDepositIngredient)
                {
                    Util.DepositIngredientsToMachines();
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
                    Util.CollectNearbyCollectibles(location);
                }
                if (Conf.AutoDigArtifactSpot)
                {
                    Util.DigNearbyArtifactSpots();
                }
                if (Conf.AutoShakeFruitedPlants)
                {
                    Util.ShakeNearbyFruitedTree();
                    Util.ShakeNearbyFruitedBush();
                }
                if(Conf.AutoAnimalDoor && !_dayEnded && Game1.timeOfDay >= 1900)
                {
                    _dayEnded = true;
                    OnBeforeSave(null, null);
                }
                if(Conf.AutoPetNearbyPets)
                {
                    Util.PetNearbyPets();
                }
            }
            catch (Exception ex)
            {
                Monitor.Log(ex.Source);
                Monitor.Log(ex.ToString());
            }
        }

        private void OnKeyPressed(object sender, EventArgsKeyPressed args)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            if (args.KeyPressed == Keys.H)
            {
                Util.ShowHudMessage($"Hay:{Game1.getFarm().piecesOfHay}");
            }
            if (!Context.IsPlayerFree || Game1.activeClickableMenu != null)
            {
                return;
            }
            if (args.KeyPressed == Conf.KeyShowMenu)
            {
                //Open Up Menu
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new JoeMenu(900, 548, this);
            }
            else if (args.KeyPressed == Conf.KeyToggleBlackList)
            {
                Util.ToggleBlacklistUnderCursor();
            }
        }

        private void OnPreRenderHud(object sender, EventArgs args)
        {
            if (Game1.currentLocation is MineShaft shaft && Conf.MineInfoGui)
            {
                Util.DrawMineGui(Game1.spriteBatch, Game1.smallFont, Game1.player, shaft);
            }
        }

        private void OnPostRenderHud(object sender, EventArgs args)
        {
            if (Context.IsPlayerFree && !string.IsNullOrEmpty(_hoverText) && Game1.player.CurrentItem != null)
            {
                Util.DrawSimpleTextbox(Game1.spriteBatch, _hoverText, Game1.smallFont, Game1.player.CurrentItem);
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
                    if (coop.indoors.Value is AnimalHouse house)
                    {
                        if (house.animals.Any() && coop.animalDoorOpen.Value)
                        {
                            coop.animalDoorOpen.Value = false;
                            Helper.Reflection.GetField<NetInt>(coop, "animalDoorMotion").SetValue(new NetInt(2));
                        }
                    }
                }
                else if (building is Barn barn)
                {
                    if (barn.indoors.Get() is AnimalHouse house)
                    {
                        if (house.animals.Any() && barn.animalDoorOpen.Value)
                        {
                            barn.animalDoorOpen.Value = false;
                            Helper.Reflection.GetField<NetInt>(barn, "animalDoorMotion").SetValue(new NetInt(2));
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
            _dayEnded = false;
            if (Game1.isRaining || Game1.isSnowing)
            {
                Monitor.Log("Don't open the animal door because of rainy/snowy weather.");
                return;
            }
            if(Game1.IsWinter)
            {
                Monitor.Log("Don't open the animal door because it's winter");
                return;
            }
            Farm farm = Game1.getFarm();
            foreach (Building building in farm.buildings)
            {
                if (building is Coop coop)
                {
                    if (coop.indoors.Get() is AnimalHouse house)
                    {
                        if (house.animals.Any() && !coop.animalDoorOpen.Value)
                        {
                            coop.animalDoorOpen.Value = true;
                            Helper.Reflection.GetField<NetInt>(coop, "animalDoorMotion").SetValue(new NetInt(-2));
                        }
                    }
                }
                else if(building is Barn barn)
                {
                    if (barn.indoors.Value is AnimalHouse house)
                    {
                        if (house.animals.Any() && !barn.animalDoorOpen.Value)
                        {
                            barn.animalDoorOpen.Value = true;
                            Helper.Reflection.GetField<NetInt>(barn, "animalDoorMotion").SetValue(new NetInt(-3));
                        }
                    }
                }
            }
        }

        public void WriteConfig()
        {
            Helper.WriteConfig(Conf);
        }
    }
}
