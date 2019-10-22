using System;
using JoysOfEfficiency.Automation;
using JoysOfEfficiency.Core;
using JoysOfEfficiency.Huds;
using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;

namespace JoysOfEfficiency.EventHandler
{
    internal class UpdateEvents
    {
        public static bool Paused => EventHolder.Update._paused;

        public static bool DayEnded { get; set; }

        public static int LastTimeOfDay { get; set; }

        private bool _paused;

        private int _ticks;

        private double _timeoutCounter;

        private static Config Conf => InstanceHolder.Config;
        private static IMonitor Monitor => InstanceHolder.Monitor;

        public void OnGameUpdateEvent(object sender, UpdateTickedEventArgs args)
        {
            OnEveryUpdate();
            if (args.IsMultipleOf(8))
            {
                OnGameEighthUpdate();
            }
        }

        public void OnEveryUpdate()
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            if (Conf.PauseWhenIdle)
            {
                if (Util.IsPlayerIdle())
                {
                    _timeoutCounter += Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
                    if (_timeoutCounter > Conf.IdleTimeout * 1000)
                    {
                        if (!_paused)
                        {
                            Monitor.Log("Paused game");
                            _paused = true;
                        }

                        Game1.timeOfDay = LastTimeOfDay;
                    }
                }
                else
                {
                    if (_paused)
                    {
                        _paused = false;
                        Monitor.Log("Resumed game");
                    }

                    _timeoutCounter = 0;
                    LastTimeOfDay = Game1.timeOfDay;
                }
            }
            else
            {
                _paused = false;
            }

            Farmer player = Game1.player;
            if (Conf.AutoGate)
            {
                Util.TryToggleGate(player);
            }

            if (player.CurrentTool is FishingRod rod)
            {
                FishingProbabilitiesBox.UpdateProbabilities(rod);
            }

            GiftInformationTooltip.UpdateTooltip();
        }

        private void OnGameEighthUpdate()
        {
            if (Game1.currentGameTime == null)
            {
                return;
            }

            if (Conf.CloseTreasureWhenAllLooted && Game1.activeClickableMenu is ItemGrabMenu menu)
            {
                InventoryAutomation.TryCloseItemGrabMenu(menu);
            }

            if (!Context.IsWorldReady || !Context.IsPlayerFree)
            {
                return;
            }

            Farmer player = Game1.player;
            GameLocation location = Game1.currentLocation;
            IReflectionHelper reflection = InstanceHolder.Reflection;
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
                }
                if (Game1.currentLocation is MineShaft shaft)
                {
                    bool isFallingDownShaft = InstanceHolder.Reflection.GetField<bool>(shaft, "isFallingDownShaft").GetValue();
                    if (isFallingDownShaft)
                    {
                        return;
                    }
                }
                if (!Context.CanPlayerMove)
                {
                    return;
                }
                if (Conf.UnifyFlowerColors)
                {
                    FlowerColorUnifier.UnifyFlowerColors();
                }

                _ticks = (_ticks + 1) % 8;
                if (Conf.BalancedMode && _ticks != 0)
                {
                    return;
                }

                if (Conf.AutoEat)
                {
                    FoodAutomation.TryToEatIfNeeded(player);
                }
                if (Conf.AutoPickUpTrash)
                {
                    TrashCanScavenger.ScavengeTrashCan();
                }
                if (Conf.AutoWaterNearbyCrops)
                {
                    HarvestAutomation.WaterNearbyCrops();
                }
                if (Conf.AutoPetNearbyAnimals)
                {
                    int radius = Conf.AutoPetRadius * Game1.tileSize;
                    Rectangle bb = Util.Expand(player.GetBoundingBox(), radius);
                    foreach (FarmAnimal animal in Util.GetAnimalsList(player))
                    {
                        if (!bb.Contains((int) animal.Position.X, (int) animal.Position.Y) || animal.wasPet.Value)
                            continue;

                        if (Game1.timeOfDay >= 1900 && !animal.isMoving())
                        {
                            continue;
                        }
                        animal.pet(player);
                    }
                }

                if (Conf.AutoShearingAndMilking)
                {
                    AnimalAutomation.ShearingAndMilking(player);
                }
                if (Conf.AutoPullMachineResult)
                {
                    MachineOperator.PullMachineResult();
                }
                if (Conf.AutoDepositIngredient)
                {
                    MachineOperator.DepositIngredientsToMachines();
                }
                if (Conf.AutoHarvest)
                {
                    HarvestAutomation.HarvestNearbyCrops(player);
                }
                if (Conf.AutoDestroyDeadCrops)
                {
                    HarvestAutomation.DestroyNearDeadCrops(player);
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
                if (Conf.AutoAnimalDoor && !DayEnded && Game1.timeOfDay >= 1900)
                {
                    DayEnded = true;
                    EventHolder.Save.OnBeforeSave(null, null);
                }
                if (Conf.AutoPetNearbyPets)
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
    }
}
