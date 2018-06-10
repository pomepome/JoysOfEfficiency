using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoysOfEfficiency.Automation;
using JoysOfEfficiency.Common;
using JoysOfEfficiency.Configuration;
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

namespace JoysOfEfficiency
{
    using Game = Game1;
    using Player = Farmer;
    public class ModEntry : Mod
    {
        private static readonly int fpsCounterThreashold = 500;

        internal static bool IsCJBCheatsOn { get; private set; }
        internal static Config config { get; private set; } = null;

        internal static IModHelper ModHelper { get; private set; } = null;

        private string hoverText;
        private bool DayEnded = false;

        private double lastMillisec = 0;
        private int frameCount = 0;
        private double fps = 0;

        public override void Entry( IModHelper helper )
        {
            ModHelper = helper;
            Util.Helper = helper;
            Util.Monitor = Monitor;
            config = helper.ReadConfig<Config>();
            GameEvents.UpdateTick += OnGameTick;
            GameEvents.EighthUpdateTick += OnGameUpdate;
            ControlEvents.KeyPressed += OnKeyPressed;

            TimeEvents.AfterDayStarted += OnPostSave;
            SaveEvents.BeforeSave += OnBeforeSave;

            GraphicsEvents.OnPostRenderEvent += OnPostRender;
            GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHUD;
            GraphicsEvents.OnPostRenderHudEvent += OnPostRenderHUD;

            Config.Init( config );

            Game.graphics.SynchronizeWithVerticalRetrace = false;

            if ( ModChecker.IsCJBCheatsLoaded( helper ) ) {
                IsCJBCheatsOn = true;

                Monitor.Log( "FasterRunningSpeed will be disabled since detected CJB Cheats Menu", LogLevel.Info );

                config.fasterRunningSpeed = false;
            }

            helper.WriteConfig( config );

            MineIcons.Init( helper );
        }

        private void OnGameTick( object senderm, EventArgs args )
        {
            if ( !Context.IsWorldReady ) 
                return;
            
            Player player = Game.player;
            if ( !IsCJBCheatsOn ) {
                if ( config.fasterRunningSpeed && player.running ) {
                    player.addedSpeed = config.addedSpeedMultiplier;
                } else {
                    player.addedSpeed = 0;
                }
                if ( player.controller != null ) {
                    player.addedSpeed = 0;
                }
            }
            if ( config.autoGate ) {
                Util.TryToggleGate( player );
            }
        }

        private void OnGameUpdate( object sender, EventArgs args )
        {
            if ( Game.currentGameTime == null )
                return;
            
            if ( lastMillisec == 0 ) 
                lastMillisec = Game.currentGameTime.TotalGameTime.TotalMilliseconds;
            
            if ( !Context.IsWorldReady || !Context.IsPlayerFree ) 
                return;
            
            Player player = Game.player;
            GameLocation location = Game.currentLocation;
            IReflectionHelper reflection = Helper.Reflection;
            try {
                if ( config.giftInformation ) {
                    hoverText = null;
                    if ( player.CurrentItem != null && player.CurrentItem.canBeGivenAsGift() ) {
                        List<NPC> npcList = player.currentLocation.characters.Where( a => a.isVillager() ).ToList();
                        foreach ( NPC npc in npcList ) {
                            RectangleE npcRect = new RectangleE( npc.position.X, npc.position.Y - npc.Sprite.getHeight() - Game.tileSize / 1.5f, npc.Sprite.getWidth() * 3 + npc.Sprite.getWidth() / 1.5f, ( npc.Sprite.getHeight() * 3.5f ) );

                            if ( npcRect.IsInternalPoint( Game.getMouseX() + Game.viewport.X, Game.getMouseY() + Game.viewport.Y ) ) {
                                //Mouse hovered on the NPC
                                StringBuilder key = new StringBuilder( "taste." );
                                switch ( npc.getGiftTasteForThisItem( player.CurrentItem ) ) {
                                    case 0: key.Append( "love." ); break;
                                    case 2: key.Append( "like." ); break;
                                    case 4: key.Append( "dislike." ); break;
                                    case 6: key.Append( "hate." ); break;
                                    default: key.Append( "neutral." ); break;
                                }
                                switch ( npc.Gender ) {
                                    case 0: key.Append( "male" ); break;
                                    default: key.Append( "female" ); break;
                                }
                                Translation translation = Helper.Translation.Get( key.ToString() );
                                hoverText = translation?.ToString();
                            }
                        }
                    }
                }

                if ( player.CurrentTool is FishingRod rod && Game.activeClickableMenu == null ) {
                    IReflectedField<int> whichFish = reflection.GetField<int>( rod, "whichFish" );
                    if ( rod.isNibbling && rod.isFishing && whichFish.GetValue() == -1 && !rod.isReeling && !rod.hit && !rod.isTimingCast && !rod.pullingOutOfWater && !rod.fishCaught && config.autoReelRod )
                        rod.DoFunction( player.currentLocation, 1, 1, 1, player );
                    
                    if ( config.muchFasterBiting && rod.isFishing && !rod.isNibbling && !rod.isReeling && !rod.hit && !rod.isTimingCast && !rod.pullingOutOfWater && !rod.fishCaught ) 
                        rod.timeUntilFishingBite -= 10000;
                }

                if ( config.autoEat )
                    Util.TryToEatIfNeeded( player );
                
                if ( config.balancedMode && FoxBalance.clickCooldown-- > 0 )
                    return;
                
                if ( config.balancedMode && FoxBalance.clickCooldown == -2 && config.balanceReadySound )
                    Game.playSound( "objectiveComplete" );

                if ( config.autoWaterNearbyCrops ) {
                    if ( config.balancedMode )
                        FoxWatering.WaterCrops( player );
                    else
                        Util.WaterNearbyCrops();
                }

                if ( config.autoHarvest ) {
                    if ( config.balancedMode )
                        FoxHarvesting.HarvestCrops( player );
                    else
                        Util.HarvestNearCrops( player );
                }
                
                if ( config.autoPetNearbyAnimals ) {
                    int radius = FoxBalance.getRadius( config.autoPetRadius ) * Game.tileSize;
                    Rectangle bb = Util.Expand( player.GetBoundingBox(), radius );
                    List<FarmAnimal> animalList = Util.GetAnimalsList( player );
                    foreach ( FarmAnimal animal in animalList ) {
                        if ( bb.Contains( (int)animal.Position.X, (int)animal.Position.Y ) && !animal.wasPet.Value ) {
                            if ( Game.timeOfDay >= 1900 && !animal.isMoving() ) {
                                continue;
                            }
                            animal.pet( player );
                        }
                    }
                }
                if ( config.autoPullMachineResult ) 
                    Util.PullMachineResult();
                
                if ( config.autoDepositIngredient ) 
                    Util.DepositIngredientsToMachines();
                
                if ( config.autoDestroyDeadCrops )
                    Util.DestroyNearDeadCrops( player );

                if ( config.autoRefillWateringCan ) {
                    WateringCan can = Util.FindToolFromInventory<WateringCan>( config.mustHoldCan );
                    if ( can != null && can.WaterLeft < Util.GetMaxCan( can ) && Util.IsThereAnyWaterNear( player.currentLocation, player.getTileLocation() ) ) {
                        can.WaterLeft = can.waterCanMax;
                        Game.playSound( "slosh" );
                        DelayedAction.playSoundAfterDelay( "glug", 250 );
                    }
                }
                if ( config.autoCollectCollectibles ) {
                    Util.CollectNearbyCollectibles( location );
                }
                if ( config.autoDigArtifactSpot ) {
                    Util.DigNearbyArtifactSpots();
                }
                if ( config.autoShakeFruitedPlants ) {
                    Util.ShakeNearbyFruitedTree();
                    Util.ShakeNearbyFruitedBush();
                }
                if ( config.fastToolUpgrade && player.daysLeftForToolUpgrade.Value > 1 ) {
                    player.daysLeftForToolUpgrade.Value = 1;
                }
                if ( config.autoAnimalDoor && !DayEnded && Game.timeOfDay >= 1900 ) {
                    DayEnded = true;
                    OnBeforeSave( null, null );
                }
                if ( config.autoPetNearbyPets ) {
                    Util.PetNearbyPets();
                }
            } catch ( Exception ex ) {
                Monitor.Log( ex.Source );
                Monitor.Log( ex.ToString() );
            }
        }

        private void OnKeyPressed( object sender, EventArgsKeyPressed args )
        {
            if ( !Context.IsWorldReady ) {
                return;
            }
            if ( args.KeyPressed == Keys.H ) {
                Player player = Game.player;
                Util.ShowHUDMessage( $"Hay:{Game.getFarm().piecesOfHay}" );
                if ( player.CurrentItem != null ) {
                    Util.ShowHUDMessage( player.CurrentItem.ParentSheetIndex + "" );
                }
            }
            if ( !Context.IsPlayerFree || Game.activeClickableMenu != null ) {
                return;
            }
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;
            if ( args.KeyPressed == config.keyShowMenu ) {
                //Open Up Menu
                Game.playSound( "bigSelect" );
                Game.activeClickableMenu = new JOEMenu( 800, 500, this );
            }
        }

        private void OnPostRender( object sender, EventArgs args )
        {
            frameCount++;
            double delta = Game.currentGameTime.TotalGameTime.TotalMilliseconds - lastMillisec;
            if ( delta >= fpsCounterThreashold ) {
                lastMillisec = Game.currentGameTime.TotalGameTime.TotalMilliseconds;
                fps = (double)frameCount * 1000 / delta;
                frameCount = 0;
            }
        }

        private void OnPreRenderHUD( object sender, EventArgs args )
        {
            if ( Game.currentLocation is MineShaft shaft && config.mineInfoGUI ) {
                Util.DrawMineGui( Game.spriteBatch, Game.smallFont, Game.player, shaft );
            }
        }

        private void OnPostRenderHUD( object sender, EventArgs args )
        {
            if ( Context.IsPlayerFree && !string.IsNullOrEmpty( hoverText ) && Game.player.CurrentItem != null ) {
                Util.DrawSimpleTextbox( Game.spriteBatch, hoverText, Game.smallFont, Game.player.CurrentItem );
            }
            if ( Game.activeClickableMenu != null && Game.activeClickableMenu is BobberBar bar ) {
                if ( config.fishingInfo ) {
                    Util.DrawFishingInfoBox( Game.spriteBatch, bar, Game.dialogueFont );
                }

                if ( config.autoFishing ) {
                    Util.AutoFishing( bar );
                }
            }
            if ( config.FPSCounter ) {
                Point point = new Point();
                switch ( config.FPSlocation ) {
                    case 1: point = new Point( 0, 10000 ); break;
                    case 2: point = new Point( 10000, 10000 ); break;
                    case 3: point = new Point( 10000, 0 ); break;
                }
                Util.DrawSimpleTextbox( Game.spriteBatch, string.Format( "{0:f1}fps", fps ), point.X, point.Y, Game.smallFont );
            }
        }

        private void OnBeforeSave( object sender, EventArgs args )
        {
            if ( !Context.IsWorldReady || !config.autoAnimalDoor ) {
                return;
            }
            Util.LetAnimalsInHome();
            Farm farm = Game.getFarm();
            foreach ( Building building in farm.buildings ) {
                if ( building is Coop coop ) {
                    if ( coop.indoors.Value is AnimalHouse house ) {
                        if ( house.animals.Any() && coop.animalDoorOpen.Value ) {
                            coop.animalDoorOpen.Value = false;
                            Helper.Reflection.GetField<NetInt>( coop, "animalDoorMotion" ).SetValue( new NetInt( 2 ) );
                        }
                    }
                } else if ( building is Barn barn ) {
                    if ( barn.indoors.Get() is AnimalHouse house ) {
                        if ( house.animals.Any() && barn.animalDoorOpen.Value ) {
                            barn.animalDoorOpen.Value = false;
                            Helper.Reflection.GetField<NetInt>( barn, "animalDoorMotion" ).SetValue( new NetInt( 2 ) );
                        }
                    }
                }
            }
        }

        private void OnPostSave( object sender, EventArgs args )
        {
            if ( !Context.IsWorldReady || !config.autoAnimalDoor ) {
                return;
            }
            DayEnded = false;
            if ( Game.isRaining || Game.isSnowing ) {
                Monitor.Log( "Don't open the animal door because of rainy/snowy weather." );
                return;
            }
            if ( Game.IsWinter ) {
                Monitor.Log( "Don't open the animal door because it's winter" );
                return;
            }
            Farm farm = Game.getFarm();
            foreach ( Building building in farm.buildings ) {
                if ( building is Coop coop ) {
                    if ( coop.indoors.Get() is AnimalHouse house ) {
                        if ( house.animals.Any() && !coop.animalDoorOpen.Value ) {
                            coop.animalDoorOpen.Value = true;
                            Helper.Reflection.GetField<NetInt>( coop, "animalDoorMotion" ).SetValue( new NetInt( -2 ) );
                        }
                    }
                } else if ( building is Barn barn ) {
                    if ( barn.indoors.Value is AnimalHouse house ) {
                        if ( house.animals.Any() && !barn.animalDoorOpen.Value ) {
                            barn.animalDoorOpen.Value = true;
                            Helper.Reflection.GetField<NetInt>( barn, "animalDoorMotion" ).SetValue( new NetInt( -3 ) );
                        }
                    }
                }
            }
        }
        public void WriteConfig()
        {
            Helper.WriteConfig( config );
        }
    }
}
