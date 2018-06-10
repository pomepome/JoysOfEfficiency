﻿using JoysOfEfficiency.Common;
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

namespace JoysOfEfficiency.Utils
{
    using Player = Farmer;
    using Game = Game1;
    using SVObject = StardewValley.Object;
    public class Util
    {
        public static IModHelper Helper;
        public static IMonitor Monitor;
        private static bool catchingTreasure;

        private static MineIcons icons = new MineIcons();
        private static List<Monster> lastMonsters = new List<Monster>();
        private static List<Vector2> flowerLocationProducingNectar = new List<Vector2>();
        private static string lastKilledMonster;

        private static List<T> GetObjectsWithin<T>( int radius ) where T : SVObject
        {
            if ( !Context.IsWorldReady ) {
                return new List<T>();
            }
            GameLocation location = Game.player.currentLocation;
            Vector2 ov = Game.player.getTileLocation();
            List<T> list = new List<T>();
            for ( int dx = -radius ; dx <= radius ; dx++ ) {
                for ( int dy = -radius ; dy <= radius ; dy++ ) {
                    Vector2 loc = ov + new Vector2( dx, dy );
                    if ( location.Objects.ContainsKey( loc ) && location.Objects[loc] is T t ) {
                        list.Add( t );
                    }
                }
            }
            return list;
        }

        private static Dictionary<Vector2, T> GetFeaturesWithin<T>( int radius ) where T : TerrainFeature
        {
            if ( !Context.IsWorldReady ) {
                return new Dictionary<Vector2, T>();
            }
            GameLocation location = Game.player.currentLocation;
            Vector2 ov = Game.player.getTileLocation();
            Dictionary<Vector2, T> list = new Dictionary<Vector2, T>();

            List<LargeTerrainFeature> lFeatures = Game.currentLocation.largeTerrainFeatures == null ? null : new List<LargeTerrainFeature>( Game.currentLocation.largeTerrainFeatures );

            for ( int dx = -radius ; dx <= radius ; dx++ ) {
                for ( int dy = -radius ; dy <= radius ; dy++ ) {
                    Vector2 loc = ov + new Vector2( dx, dy );
                    if ( location.terrainFeatures.ContainsKey( loc ) && location.terrainFeatures[loc] is T t ) {
                        list.Add( loc, t );
                        continue;
                    } else if ( lFeatures != null && typeof( LargeTerrainFeature ).IsAssignableFrom( typeof( T ) ) ) {
                        foreach ( LargeTerrainFeature feature in lFeatures ) {
                            if ( feature != null && feature is T && feature.tilePosition.X == loc.X && feature.tilePosition.Y == loc.Y ) {
                                list.Add( loc, feature as T );
                            }
                        }
                    }
                }
            }
            return list;
        }

        public static void UpdateNectarInfo()
        {
            flowerLocationProducingNectar.Clear();
            foreach ( KeyValuePair<Vector2, SVObject> kv in Game.currentLocation.Objects.Pairs.Where( pair => pair.Value.Name == "Bee House" ) ) {
                Vector2 houseLoc = kv.Key;
                Vector2 flowerLoc = GetCropLocation( Utility.findCloseFlower( Game.currentLocation, houseLoc ) );
                if ( flowerLoc.X != -1 && flowerLoc.Y != -1 && !flowerLocationProducingNectar.Contains( flowerLoc ) ) {
                    flowerLocationProducingNectar.Add( flowerLoc );
                }
            }
        }

        public static void PetNearbyPets()
        {
            GameLocation location = Game.currentLocation;
            Player player = Game.player;

            Rectangle bb = Expand( player.GetBoundingBox(), FoxBalance.getRadius( ModEntry.config.autoPetRadius ) * Game.tileSize );

            foreach ( Pet pet in location.characters.OfType<Pet>().Where( pet => pet.GetBoundingBox().Intersects( bb ) ) ) {
                bool wasPet = Helper.Reflection.GetField<bool>( pet, "wasPetToday" ).GetValue();
                if ( !wasPet ) {
                    pet.checkAction( player, location ); // Pet pet... lol =)
                }
            }
        }

        public static void DepositIngredientsToMachines()
        {
            Player player = Game.player;
            if ( player.CurrentItem == null || !( Game.player.CurrentItem is SVObject ) ) {
                return;
            }
            foreach ( SVObject obj in GetObjectsWithin<SVObject>( FoxBalance.getRadius( ModEntry.config.machineRadius ) ) ) {
                Vector2 loc = GetLocationOf( Game.currentLocation, obj );
                if ( IsObjectMachine( obj ) && obj.heldObject.Value == null ) {
                    if ( Utility.isThereAnObjectHereWhichAcceptsThisItem( Game.currentLocation, player.CurrentItem, (int)loc.X * Game.tileSize, (int)loc.Y * Game.tileSize ) ) {
                        obj.performObjectDropInAction( player.CurrentItem, false, player );
                        if ( obj.Name != "Furnace" || player.CurrentItem.getStack() == 0 )
                            player.reduceActiveItemByOne();
                        if ( ModEntry.config.balancedMode ) {
                            FoxBalance.clickCooldown = 1;
                            return;
                        }
                    }
                }
            }
        }

        public static void PullMachineResult()
        {
            Player player = Game.player;
            foreach ( SVObject obj in GetObjectsWithin<SVObject>( FoxBalance.getRadius( ModEntry.config.machineRadius ) ) ) {
                if ( obj.readyForHarvest.Value && obj.heldObject.Value != null ) {
                    Item item = obj.heldObject.Value;
                    if ( player.couldInventoryAcceptThisItem( item ) ) {
                        obj.checkForAction( player );
                    }
                    if ( ModEntry.config.balancedMode ) {
                        FoxBalance.clickCooldown = 1;
                        return;
                    }
                }
            }
        }

        public static void ShakeNearbyFruitedBush()
        {
            foreach ( KeyValuePair<Vector2, Bush> bushes in GetFeaturesWithin<Bush>( FoxBalance.getRadius( ModEntry.config.autoHarvestRadius ) )) {
                Vector2 loc = bushes.Key;
                Bush bush = bushes.Value;

                if ( !bush.townBush.Value && bush.tileSheetOffset.Value == 1 && bush.inBloom( Game.currentSeason, Game.dayOfMonth ) ) {
                    bush.performUseAction( loc, Game.currentLocation );
                    if ( ModEntry.config.balancedMode ) {
                        FoxBalance.clickCooldown = 1;
                        return;
                    }
                }
            }
        }

        public static void ShakeNearbyFruitedTree()
        {
            foreach ( KeyValuePair<Vector2, TerrainFeature> kv in GetFeaturesWithin<TerrainFeature>( FoxBalance.getRadius( ModEntry.config.autoShakeRadius )) ) {
                Vector2 loc = kv.Key;
                TerrainFeature feature = kv.Value;
                if ( feature is Tree tree ) {
                    if ( tree.hasSeed.Value && !tree.stump.Value ) {
                        Helper.Reflection.GetMethod( tree, "shake" ).Invoke( loc, false );
                        if ( ModEntry.config.balancedMode ) {
                            FoxBalance.clickCooldown = 1;
                            return;
                        }
                    }
                }
                if ( feature is FruitTree ftree ) {
                    if ( ftree.growthStage.Value >= 4 && ftree.fruitsOnTree.Value > 0 && !ftree.stump.Value ) {
                        ftree.shake( loc, false );
                        if ( ModEntry.config.balancedMode ) {
                            FoxBalance.clickCooldown = 1;
                            return;
                        }
                    }
                }
            }
        }

        public static void DigNearbyArtifactSpots()
        {
            Player player = Game.player;
            int radius = FoxBalance.getRadius( ModEntry.config.autoDigRadius );

            Hoe hoe = null;

            if ( !ModEntry.config.mustHoldHoe )
                hoe = FindToolFromInventory<Hoe>( player );
            else {
                if ( !( player.CurrentTool is Hoe h ) )
                    return;
                hoe = h;
            }

            GameLocation location = player.currentLocation;
            if ( hoe != null && player.Stamina > 3 ) {
                bool flag = false;
                for ( int i = -radius ; i <= radius ; i++ ) {
                    for ( int j = -radius ; j <= radius ; j++ ) {
                        int x = player.getTileX() + i;
                        int y = player.getTileY() + j;
                        Vector2 loc = new Vector2( x, y );
                        if ( location.Objects.ContainsKey( loc ) && location.Objects[loc].ParentSheetIndex == 590 && !location.isTileHoeDirt( loc ) ) {
                            location.digUpArtifactSpot( x, y, player );
                            location.Objects.Remove( loc );
                            location.terrainFeatures.Add( loc, new HoeDirt() );
                            if ( ModEntry.config.balancedMode ) {
                                Game.playSound( "hoeHit" );
                                FoxBalance.clickCooldown = 3;
                                player.Stamina -= 2 - ( player.farmingLevel.Value / 10.0F );
                                return;
                            }
                        }
                    }
                }
                if ( flag ) {
                    Game.playSound( "hoeHit" );
                }
            }
        }

        public static void CollectNearbyCollectibles( GameLocation location )
        {
            foreach ( SVObject obj in GetObjectsWithin<SVObject>( FoxBalance.getRadius( ModEntry.config.autoCollectRadius )) ) {
                if ( obj.IsSpawnedObject || obj.isAnimalProduct() ) {
                    CollectObj( location, obj );
                    if ( ModEntry.config.balancedMode ) {
                        FoxBalance.clickCooldown = 2;
                        return;
                    }
                }
            }
        }


        public static void DestroyNearDeadCrops( Player player )
        {
            GameLocation location = player.currentLocation;
            bool flag = false;
            foreach ( KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>( 1 ) ) {
                Vector2 loc = kv.Key;
                HoeDirt dirt = kv.Value;
                if ( dirt.crop != null && dirt.crop.dead.Value ) {
                    dirt.destroyCrop( loc, true, location );
                    flag = true;
                    if ( ModEntry.config.balancedMode ) {
                        Game.playSound( "cut" );
                        FoxBalance.clickCooldown = 1;
                        return;
                    }
                }
            }
            foreach ( IndoorPot pot in GetObjectsWithin<IndoorPot>( 1 ) ) {
                Vector2 loc = GetLocationOf( location, pot );
                if ( pot.hoeDirt.Value != null ) {
                    HoeDirt dirt = pot.hoeDirt.Value;
                    if ( dirt.crop != null && dirt.crop.dead.Value ) {
                        dirt.destroyCrop( loc, true, location );
                        flag = true;
                        if ( ModEntry.config.balancedMode ) {
                            Game.playSound( "cut" );
                            FoxBalance.clickCooldown = 1;
                            return;
                        }
                    }
                }
            }
            if ( flag ) {
                Game.playSound( "cut" );
            }
        }

        public static void DrawCursor()
        {
            if ( !Game.options.hardwareCursor ) {
                Game.spriteBatch.Draw( Game.mouseCursors, new Vector2( Game.getOldMouseX(), Game.getOldMouseY() ), Game.getSourceRectForStandardTileSheet( Game.mouseCursors, Game.options.gamepadControls ? 44 : 0, 16, 16 ), Color.White, 0f, Vector2.Zero, Game.pixelZoom + Game.dialogueButtonScale / 150f, SpriteEffects.None, 1f );
            }
        }

        public static int GetMaxCan( WateringCan can )
        {
            if ( can == null ) {
                return -1;
            }
            switch ( can.UpgradeLevel ) {
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

        public static void HarvestNearCrops( Player player )
        {
            GameLocation location = player.currentLocation;
            int radius = FoxBalance.getRadius( ModEntry.config.autoHarvestRadius);

            if ( ModEntry.config.protectNectarProducingFlower ) {
                UpdateNectarInfo();
            }

            foreach ( KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>( radius ) ) {
                Vector2 loc = kv.Key;
                HoeDirt dirt = kv.Value;
                if ( dirt.crop == null )
                    continue;

                if ( dirt.readyForHarvest() ) {
                    if ( ModEntry.config.protectNectarProducingFlower && IsProducingNectar( loc ) )
                        continue;

                    if ( Harvest( (int)loc.X, (int)loc.Y, dirt ) ) {
                        if ( dirt.crop.regrowAfterHarvest.Value == -1 || dirt.crop.forageCrop.Value ) {
                            //destroy crop if it does not reqrow.
                            dirt.destroyCrop( loc, true, location );
                        }
                    }
                }
            }
            foreach ( IndoorPot pot in GetObjectsWithin<IndoorPot>( radius ) ) {
                if ( pot.hoeDirt.Value != null ) {
                    HoeDirt dirt = pot.hoeDirt.Value;
                    if ( dirt.crop != null && dirt.readyForHarvest() ) {
                        Vector2 tileLoc = GetLocationOf( location, pot );
                        if ( dirt.crop.harvest( (int)tileLoc.X, (int)tileLoc.Y, dirt ) ) {
                            if ( dirt.crop.regrowAfterHarvest.Value == -1 || dirt.crop.forageCrop.Value ) {
                                //destroy crop if it does not reqrow.
                                dirt.destroyCrop( tileLoc, true, location );
                            }
                        }
                    }
                }
            }
        }

        public static void WaterNearbyCrops()
        {
            Player player = Game.player;
            WateringCan can = FindToolFromInventory<WateringCan>( player );
            if ( can != null ) {
                GetMaxCan( can );
                bool watered = false;
                foreach ( KeyValuePair<Vector2, HoeDirt> kv in GetFeaturesWithin<HoeDirt>( FoxBalance.getRadius( ModEntry.config.autoWaterRadius )) ) {
                    Vector2 location = kv.Key;
                    HoeDirt dirt = kv.Value;
                    float consume = 2 * ( 1.0f / ( can.UpgradeLevel / 2.0f + 1 ) );
                    if ( dirt.crop != null && !dirt.crop.dead.Value && dirt.state.Value == 0 && player.Stamina >= consume && can.WaterLeft > 0 ) {
                        dirt.state.Value = 1;
                        player.Stamina -= consume;
                        can.WaterLeft--;
                        watered = true;
                    }
                }
                foreach ( IndoorPot pot in GetObjectsWithin<IndoorPot>( FoxBalance.getRadius( ModEntry.config.autoWaterRadius )) ) {
                    if ( pot.hoeDirt.Value != null ) {
                        HoeDirt dirt = pot.hoeDirt.Value;
                        float consume = 2 * ( 1.0f / ( can.UpgradeLevel / 2.0f + 1 ) );
                        if ( dirt.crop != null && !dirt.crop.dead.Value && dirt.state.Value != 1 && player.Stamina >= consume && can.WaterLeft > 0 ) {
                            dirt.state.Value = 1;
                            pot.showNextIndex.Value = true;
                            player.Stamina -= consume;
                            can.WaterLeft--;
                            watered = true;
                        }
                    }
                }
                if ( watered ) {
                    Game.playSound( "slosh" );
                }
            }
        }

        private static Vector2 FindLadder( MineShaft shaft )
        {
            for ( int i = 0 ; i < shaft.Map.GetLayer( "Buildings" ).LayerWidth ; i++ ) {
                for ( int j = 0 ; j < shaft.Map.GetLayer( "Buildings" ).LayerHeight ; j++ ) {
                    int index = shaft.getTileIndexAt( new Point( i, j ), "Buildings" );
                    Vector2 loc = new Vector2( i, j );
                    if ( !shaft.Objects.ContainsKey( loc ) && !shaft.terrainFeatures.ContainsKey( loc ) ) {
                        if ( index == 171 || index == 173 || index == 174 )
                            return loc;
                    }
                }
            }
            return Vector2.Zero;
        }

        public static void DrawMineGui( SpriteBatch batch, SpriteFont font, Player player, MineShaft shaft )
        {
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;
            int stonesLeft = reflection.GetField<NetIntDelta>( shaft, "netStonesLeftOnThisLevel" ).GetValue();
            Vector2 ladderPos = FindLadder( shaft );
            bool ladder = ladderPos != null && ladderPos != Vector2.Zero;

            List<Monster> currentMonsters = shaft.characters.OfType<Monster>().ToList();
            foreach ( Monster mon in lastMonsters ) {
                if ( !currentMonsters.Contains( mon ) && mon.Name != "ignoreMe" ) {
                    lastKilledMonster = mon.Name;
                }
            }
            lastMonsters = currentMonsters.ToList();
            string tallyStr = null;
            string ladderStr = null;
            if ( lastKilledMonster != null ) {
                int kills = Game.stats.getMonstersKilled( lastKilledMonster );
                tallyStr = string.Format( translation.Get( "monsters.tally" ), lastKilledMonster, kills );
            }

            string stonesStr = null;
            if ( stonesLeft == 0 ) {
                stonesStr = translation.Get( "stones.none" );
            } else {
                bool single = stonesLeft == 1;
                if ( single ) {
                    stonesStr = translation.Get( "stones.one" );
                } else {
                    stonesStr = string.Format( translation.Get( "stones.many" ), stonesLeft );
                }
            }
            if ( ladder ) {
                ladderStr = translation.Get( "ladder" );
            }
            icons.Draw( stonesStr, tallyStr, ladderStr );
        }


        public static void DrawFishingInfoBox( SpriteBatch batch, BobberBar bar, SpriteFont font )
        {
            IReflectionHelper reflection = Helper.Reflection;
            ITranslationHelper translation = Helper.Translation;

            int width = 0, height = 120;


            float scale = 1.0f;


            int whitchFish = reflection.GetField<int>( bar, "whichFish" ).GetValue();
            int fishSize = reflection.GetField<int>( bar, "fishSize" ).GetValue();
            int fishQuality = reflection.GetField<int>( bar, "fishQuality" ).GetValue();
            bool treasure = reflection.GetField<bool>( bar, "treasure" ).GetValue();
            bool treasureCaught = reflection.GetField<bool>( bar, "treasureCaught" ).GetValue();
            float treasureAppearTimer = reflection.GetField<float>( bar, "treasureAppearTimer" ).GetValue() / 1000;

            SVObject fish = new SVObject( whitchFish, 1 );

            if ( LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en ) {
                scale = 0.7f;
            }

            string speciesText = TryFormat( translation.Get( "fishinfo.species" ).ToString(), fish.DisplayName );
            string sizeText = TryFormat( translation.Get( "fishinfo.size" ).ToString(), GetFinalSize( fishSize ) );
            string qualityText1 = translation.Get( "fishinfo.quality" ).ToString();
            string qualityText2 = translation.Get( GetKeyForQuality( fishQuality ) ).ToString();
            string incomingText = TryFormat( translation.Get( "fishinfo.treasure.incoming" ).ToString(), treasureAppearTimer );
            string appearedText = translation.Get( "fishinfo.treasure.appear" ).ToString();
            string caughtText = translation.Get( "fishinfo.treasure.caught" ).ToString();

            {
                Vector2 size = font.MeasureString( speciesText ) * scale;
                if ( size.X > width ) {
                    width = (int)size.X;
                }
                height += (int)size.Y;
                size = font.MeasureString( sizeText ) * scale;
                if ( size.X > width ) {
                    width = (int)size.X;
                }
                height += (int)size.Y;
                Vector2 temp = font.MeasureString( qualityText1 );
                Vector2 temp2 = font.MeasureString( qualityText2 );
                size = new Vector2( temp.X + temp2.X, Math.Max( temp.Y, temp2.Y ) );
                if ( size.X > width ) {
                    width = (int)size.X;
                }
                height += (int)size.Y;
            }

            if ( treasure ) {
                if ( treasureAppearTimer > 0 ) {
                    Vector2 size = font.MeasureString( incomingText ) * scale;
                    if ( size.X > width ) {
                        width = (int)size.X;
                    }
                    height += (int)size.Y;
                } else {
                    if ( !treasureCaught ) {
                        Vector2 size = font.MeasureString( appearedText ) * scale;
                        if ( size.X > width ) {
                            width = (int)size.X;
                        }
                        height += (int)size.Y;
                    } else {
                        Vector2 size = font.MeasureString( caughtText ) * scale;
                        if ( size.X > width ) {
                            width = (int)size.X;
                        }
                        height += (int)size.Y;
                    }
                }
            }

            width += 64;

            int x = bar.xPositionOnScreen + bar.width + 96;
            if ( x + width > Game.viewport.Width ) {
                x = bar.xPositionOnScreen - width - 96;
            }
            int y = (int)Cap( bar.yPositionOnScreen, 0, Game.viewport.Height - height );

            IClickableMenu.drawTextureBox( batch, Game.menuTexture, new Rectangle( 0, 256, 60, 60 ), x, y, width, height, Color.White );
            fish.drawInMenu( batch, new Vector2( x + width / 2 - 32, y + 16 ), 1.0f, 1.0f, 0.9f, false );


            Vector2 stringSize = font.MeasureString( "X" );
            Vector2 addition = new Vector2( 0, stringSize.Y );

            Vector2 vec2 = new Vector2( x + 32, y + 96 );
            DrawString( batch, font, ref vec2, speciesText, Color.Black, scale, false );
            DrawString( batch, font, ref vec2, sizeText, Color.Black, scale, false );
            DrawString( batch, font, ref vec2, qualityText1, Color.Black, scale, true );
            DrawString( batch, font, ref vec2, qualityText2, GetColorForQuality( fishQuality ), scale );
            vec2.X = x + 32;
            if ( treasure ) {
                if ( !treasureCaught ) {
                    if ( treasureAppearTimer > 0f ) {
                        DrawString( batch, font, ref vec2, incomingText, Color.Red, scale, false );
                    } else {
                        DrawString( batch, font, ref vec2, appearedText, Color.LightGoldenrodYellow, scale, false );
                    }
                } else {
                    DrawString( batch, font, ref vec2, caughtText, Color.ForestGreen, scale, false );
                }
            }
        }

        public static void TryToggleGate( Player player )
        {
            GameLocation location = player.currentLocation;

            foreach ( Fence fence in GetObjectsWithin<Fence>( 2 ).Where( f => f.isGate.Value ) ) {
                Vector2 loc = fence.TileLocation;
                IReflectedField<NetInt> fieldPosition = Helper.Reflection.GetField<NetInt>( fence, "gatePosition" );

                bool? isUpDown = IsUpsideDown( location, fence );
                if ( isUpDown == null ) {
                    if ( !fence.getBoundingBox( loc ).Intersects( player.GetBoundingBox() ) ) {
                        fieldPosition.SetValue( new NetInt( 0 ) );
                    }
                    continue;
                }

                int gatePosition = fence.gatePosition.Value;
                bool flag = IsPlayerInClose( player, fence, fence.TileLocation, isUpDown );


                if ( flag && gatePosition == 0 ) {
                    fieldPosition.SetValue( new NetInt( 88 ) );
                    Game.playSound( "doorClose" );
                }
                if ( !flag && gatePosition >= 88 ) {
                    fieldPosition.SetValue( new NetInt( 0 ) );
                    Game.playSound( "doorClose" );
                }
            }
        }

        public static Vector2 GetLocationOf( GameLocation location, SVObject obj )
        {
            IEnumerable<KeyValuePair<Vector2, SVObject>> pairs = location.Objects.Pairs.Where( kv => kv.Value == obj );
            if ( pairs.Count() == 0 ) {
                return new Vector2( -1, -1 );
            }
            return pairs.First().Key;
        }

        public static Vector2 GetLocationOf( GameLocation location, TerrainFeature feature )
        {
            IEnumerable<KeyValuePair<Vector2, TerrainFeature>> pairs = location.terrainFeatures.Pairs.Where( kv => kv.Value == feature );
            if ( pairs.Count() == 0 ) {
                return new Vector2( -1, -1 );
            }
            return pairs.First().Key;
        }

        public static bool CollectObj( GameLocation loc, SVObject obj )
        {
            Player who = Game.player;

            Vector2 vector = GetLocationOf( loc, obj );
            if ( vector.X == -1 && vector.Y == -1 ) {
                return false;
            }

            int quality = obj.Quality;
            Random random = new Random( (int)Game.uniqueIDForThisGame / 2 + (int)Game.stats.DaysPlayed + (int)vector.X + (int)vector.Y * 777 );
            if ( who.professions.Contains( 16 ) && obj.isForage( loc ) ) {
                obj.Quality = 4;
            } else if ( obj.isForage( loc ) ) {
                if ( random.NextDouble() < who.ForagingLevel / 30f ) {
                    obj.Quality = 2;
                } else if ( random.NextDouble() < who.ForagingLevel / 15f ) {
                    obj.Quality = 1;
                }
            }
            if ( obj.questItem.Value && obj.questId.Value != 0 && !who.hasQuest( obj.questId.Value ) ) {
                return false;
            }
            if ( who.couldInventoryAcceptThisItem( obj ) ) {
                Monitor.Log( $"picked up {obj.DisplayName} at [{vector.X},{vector.Y}]" );
                if ( who.IsLocalPlayer ) {
                    loc.localSound( "pickUpItem" );
                    DelayedAction.playSoundAfterDelay( "coin", 300, null );
                }
                who.animateOnce( 279 + who.FacingDirection );
                if ( !loc.isFarmBuildingInterior() ) {
                    if ( obj.isForage( loc ) ) {
                        who.gainExperience( 2, 7 );
                    }
                } else {
                    who.gainExperience( 0, 5 );
                }
                who.addItemToInventoryBool( obj.getOne(), false );
                Game.stats.ItemsForaged++;
                if ( who.professions.Contains( 13 ) && random.NextDouble() < 0.2 && !obj.questItem.Value && who.couldInventoryAcceptThisItem( obj ) && !loc.isFarmBuildingInterior() ) {
                    who.addItemToInventoryBool( obj.getOne(), false );
                    who.gainExperience( 2, 7 );
                }
                loc.Objects.Remove( vector );
                return true;
            }
            obj.Quality = quality;
            return false;
        }

        public static bool IsThereAnyWaterNear( GameLocation location, Vector2 tileLocation )
        {
            for ( int i = -1 ; i <= 1 ; i++ ) {
                for ( int j = -1 ; j <= 1 ; j++ ) {
                    Vector2 toCheck = tileLocation + new Vector2( i, j );
                    int x = (int)toCheck.X, y = (int)toCheck.Y;
                    if ( location.doesTileHaveProperty( x, y, "Water", "Back" ) != null || location.doesTileHaveProperty( x, y, "WaterSource", "Back" ) != null ) {
                        return true;
                    }
                }
            }
            return false;
        }


        public static T FindToolFromInventory<T>( Player player ) where T : Tool
        {
            if ( player.CurrentTool is T t ) {
                return t;
            }
            return player.Items.OfType<T>().FirstOrDefault();
        }

        public static bool Harvest( int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null )
        {
            IReflectionHelper reflection = Helper.Reflection;

            Multiplayer multiplayer = reflection.GetField<Multiplayer>( typeof( Game ), "multiplayer" ).GetValue();
            Crop crop = soil.crop;
            if ( crop.dead.Value ) {
                if ( junimoHarvester != null ) {
                    return true;
                }
                return false;
            }
            if ( crop.forageCrop.Value ) {
                SVObject o = null;
                int experience2 = 3;
                int num = crop.whichForageCrop.Value;
                if ( num == 1 ) {
                    o = new SVObject( 399, 1, false, -1, 0 );
                }
                if ( Game.player.professions.Contains( 16 ) ) {
                    o.Quality = 4;
                } else if ( Game.random.NextDouble() < Game.player.ForagingLevel / 30f ) {
                    o.Quality = 2;
                } else if ( Game.random.NextDouble() < Game.player.ForagingLevel / 15f ) {
                    o.Quality = 1;
                }
                Game.stats.ItemsForaged += (uint)o.Stack;
                if ( junimoHarvester != null ) {
                    junimoHarvester.tryToAddItemToHut( o );
                    return true;
                }
                if ( Game.player.addItemToInventoryBool( o, false ) ) {
                    Vector2 initialTile2 = new Vector2( xTile, yTile );
                    Game.player.animateOnce( 279 + Game.player.FacingDirection );
                    Game.player.canMove = false;
                    Game.player.currentLocation.playSound( "harvest" );
                    DelayedAction.playSoundAfterDelay( "coin", 260, null );
                    if ( crop.regrowAfterHarvest.Value == -1 ) {
                        multiplayer.broadcastSprites( Game.currentLocation, new TemporaryAnimatedSprite( 17, new Vector2( initialTile2.X * 64f, initialTile2.Y * 64f ), Color.White, 7, Game.random.NextDouble() < 0.5, 125f, 0, -1, -1f, -1, 0 ) );
                        multiplayer.broadcastSprites( Game.currentLocation, new TemporaryAnimatedSprite( 14, new Vector2( initialTile2.X * 64f, initialTile2.Y * 64f ), Color.White, 7, Game.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0 ) );
                    }
                    Game.player.gainExperience( 2, experience2 );
                    return true;
                }
            } else if ( crop.currentPhase.Value >= crop.phaseDays.Count - 1 && ( !crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0 ) ) {
                int numToHarvest = 1;
                int cropQuality = 0;
                int fertilizerQualityLevel = 0;
                if ( crop.indexOfHarvest.Value == 0 ) {
                    return true;
                }
                Random r = new Random( xTile * 7 + yTile * 11 + (int)Game.stats.DaysPlayed + (int)Game.uniqueIDForThisGame );
                switch ( soil.fertilizer.Value ) {
                    case 368:
                        fertilizerQualityLevel = 1;
                        break;
                    case 369:
                        fertilizerQualityLevel = 2;
                        break;
                }
                double chanceForGoldQuality = 0.2 * ( Game.player.FarmingLevel / 10.0 ) + 0.2 * (double)fertilizerQualityLevel * ( ( (double)Game.player.FarmingLevel + 2.0 ) / 12.0 ) + 0.01;
                double chanceForSilverQuality = Math.Min( 0.75, chanceForGoldQuality * 2.0 );
                if ( r.NextDouble() < chanceForGoldQuality ) {
                    cropQuality = 2;
                } else if ( r.NextDouble() < chanceForSilverQuality ) {
                    cropQuality = 1;
                }
                if ( crop.minHarvest.Value > 1 || crop.maxHarvest.Value > 1 ) {
                    numToHarvest = r.Next( crop.minHarvest.Value, Math.Min( crop.minHarvest.Value + 1, crop.maxHarvest.Value + 1 + Game.player.FarmingLevel / crop.maxHarvestIncreasePerFarmingLevel.Value ) );
                }
                if ( crop.chanceForExtraCrops.Value > 0.0 ) {
                    while ( r.NextDouble() < Math.Min( 0.9, crop.chanceForExtraCrops.Value ) ) {
                        numToHarvest++;
                    }
                }
                if ( (int)crop.harvestMethod.Value == 1 ) {
                    for ( int j = 0 ; j < numToHarvest ; j++ ) {
                        Game.createObjectDebris( crop.indexOfHarvest.Value, xTile, yTile, -1, cropQuality, 1f, null );
                    }
                    if ( crop.regrowAfterHarvest.Value == -1 ) {
                        return true;
                    }
                    crop.dayOfCurrentPhase.Value = crop.regrowAfterHarvest.Value;
                    crop.fullyGrown.Value = true;
                } else if ( Game.player.addItemToInventoryBool( ( crop.programColored.Value ) ? new ColoredObject( crop.indexOfHarvest.Value, 1, crop.tintColor.Value )
                {
                    Quality = cropQuality
                } : new SVObject( crop.indexOfHarvest.Value, 1, false, -1, cropQuality ), false ) ) {
                    Vector2 initialTile = new Vector2( xTile, yTile );
                    if ( junimoHarvester == null ) {
                        Game.player.animateOnce( 279 + Game.player.FacingDirection );
                        Game.player.canMove = false;
                    } else {
                        junimoHarvester.tryToAddItemToHut( ( crop.programColored.Value ) ? new ColoredObject( crop.indexOfHarvest.Value, 1, crop.tintColor.Value )
                        {
                            Quality = cropQuality
                        } : new SVObject( crop.indexOfHarvest.Value, 1, false, -1, cropQuality ) );
                    }
                    if ( r.NextDouble() < (double)( (float)Game.player.LuckLevel / 1500f ) + Game.dailyLuck / 1200.0 + 9.9999997473787516E-05 ) {
                        numToHarvest *= 2;
                        if ( junimoHarvester == null ) {
                            Game.player.currentLocation.playSound( "dwoop" );
                        } else if ( Utility.isOnScreen( junimoHarvester.getTileLocationPoint(), 64, junimoHarvester.currentLocation ) ) {
                            junimoHarvester.currentLocation.playSound( "dwoop" );
                        }
                    } else if ( crop.harvestMethod.Value == 0 ) {
                        if ( junimoHarvester == null ) {
                            Game.player.currentLocation.playSound( "harvest" );
                        }
                        if ( junimoHarvester == null ) {
                            DelayedAction.playSoundAfterDelay( "coin", 260, Game.player.currentLocation );
                        } else if ( Utility.isOnScreen( junimoHarvester.getTileLocationPoint(), 64, junimoHarvester.currentLocation ) ) {
                            DelayedAction.playSoundAfterDelay( "coin", 260, junimoHarvester.currentLocation );
                        }
                        if ( crop.regrowAfterHarvest.Value == -1 ) {
                            multiplayer.broadcastSprites( Game.currentLocation, new TemporaryAnimatedSprite( 17, new Vector2( initialTile.X * 64f, initialTile.Y * 64f ), Color.White, 7, Game.random.NextDouble() < 0.5, 125f, 0, -1, -1f, -1, 0 ) );
                            multiplayer.broadcastSprites( Game.currentLocation, new TemporaryAnimatedSprite( 14, new Vector2( initialTile.X * 64f, initialTile.Y * 64f ), Color.White, 7, Game.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0 ) );
                        }
                    }
                    if ( crop.indexOfHarvest.Value == 421 ) {
                        crop.indexOfHarvest.Value = 431;
                        numToHarvest = r.Next( 1, 4 );
                    }
                    for ( int i = 0 ; i < numToHarvest - 1 ; i++ ) {
                        Game.createObjectDebris( crop.indexOfHarvest.Value, xTile, yTile, -1, 0, 1f, null );
                    }
                    int price = Convert.ToInt32( Game.objectInformation[crop.indexOfHarvest.Value].Split( '/' )[1] );
                    float experience = (float)( 16.0 * Math.Log( 0.018 * (double)price + 1.0, 2.7182818284590451 ) );
                    if ( junimoHarvester == null ) {
                        Game.player.gainExperience( 0, (int)Math.Round( (double)experience ) );
                    }
                    if ( crop.regrowAfterHarvest.Value == -1 ) {
                        return true;
                    }
                    crop.dayOfCurrentPhase.Value = crop.regrowAfterHarvest.Value;
                    crop.fullyGrown.Value = true;
                } else {
                }
            }
            return false;
        }

        private static Vector2 GetCropLocation( Crop crop )
        {
            foreach ( KeyValuePair<Vector2, TerrainFeature> kv in Game.currentLocation.terrainFeatures.Pairs ) {
                if ( kv.Value is HoeDirt dirt ) {
                    if ( dirt.crop != null && !dirt.crop.dead.Value && dirt.crop == crop ) {
                        return kv.Key;
                    }
                }
            }
            return new Vector2( -1, -1 );
        }

        /// <summary>
        /// Is the dirt's crop is a flower and producing nectar
        /// </summary>
        /// <param name="dirt">HoeDirt to evaluate</param>
        /// <returns></returns>
        private static bool IsProducingNectar( Vector2 location )
        {
            return flowerLocationProducingNectar.Contains( location );
        }

        private static bool IsObjectMachine( SVObject obj )
        {
            if ( obj is CrabPot ) {
                return true;
            }
            if ( !obj.bigCraftable.Value ) {
                return false;
            }
            switch ( obj.Name ) {
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
                    return true;
                default: return false;
            }
        }

        public static bool? IsUpsideDown( GameLocation location, Fence fence )
        {
            int num2 = 0;
            Vector2 tileLocation = fence.TileLocation;
            int whichType = fence.whichType.Value;
            tileLocation.X += 1f;
            if ( Game.currentLocation.objects.ContainsKey( tileLocation ) && Game.currentLocation.objects[tileLocation].GetType() == typeof( Fence ) && ( (Fence)Game.currentLocation.objects[tileLocation] ).countsForDrawing( whichType ) ) {
                num2 += 100;
            }
            tileLocation.X -= 2f;
            if ( Game.currentLocation.objects.ContainsKey( tileLocation ) && Game.currentLocation.objects[tileLocation].GetType() == typeof( Fence ) && ( (Fence)Game.currentLocation.objects[tileLocation] ).countsForDrawing( whichType ) ) {
                num2 += 10;
            }
            tileLocation.X += 1f;
            tileLocation.Y += 1f;
            if ( Game.currentLocation.objects.ContainsKey( tileLocation ) && Game.currentLocation.objects[tileLocation].GetType() == typeof( Fence ) && ( (Fence)Game.currentLocation.objects[tileLocation] ).countsForDrawing( whichType ) ) {
                num2 += 500;
            }
            tileLocation.Y -= 2f;
            if ( Game.currentLocation.objects.ContainsKey( tileLocation ) && Game.currentLocation.objects[tileLocation].GetType() == typeof( Fence ) && ( (Fence)Game.currentLocation.objects[tileLocation] ).countsForDrawing( whichType ) ) {
                num2 += 1000;
            }

            if ( fence.isGate.Value ) {
                if ( num2 == 110 ) {
                    return true;
                } else if ( num2 == 1500 ) {
                    return false;
                }
            }

            return null;
        }

        private static bool IsPlayerInClose( Player player, Fence fence, Vector2 fenceLocation, bool? isUpDown )
        {
            if ( isUpDown == null ) {
                return fence.getBoundingBox( fence.TileLocation ).Intersects( player.GetBoundingBox() );
            }
            Vector2 playerTileLocation = player.getTileLocation();
            if ( playerTileLocation == fenceLocation ) {
                return true;
            }
            if ( !IsPlayerFaceOrBackToFence( isUpDown == true, player ) ) {
                return false;
            }
            if ( isUpDown == true ) {
                return ExpandSpecific( fence.getBoundingBox( fenceLocation ), 0, 16 ).Intersects( player.GetBoundingBox() );
            }
            return ExpandSpecific( fence.getBoundingBox( fenceLocation ), 16, 0 ).Intersects( player.GetBoundingBox() );
        }

        private static Rectangle ExpandSpecific( Rectangle rect, int deltaX, int deltaY )
        {
            return new Rectangle( rect.X - deltaX, rect.Y - deltaY, rect.Width + deltaX * 2, rect.Height + deltaY * 2 );
        }

        private static bool IsPlayerFaceOrBackToFence( bool isUpDown, Player player )
        {
            return isUpDown ? player.FacingDirection % 2 == 0 : player.FacingDirection % 2 == 1;
        }
        public static void TryToEatIfNeeded( Player player )
        {
            if ( player.isEating ) {
                return;
            }
            if ( player.Stamina <= player.MaxStamina * ModEntry.config.staminaToEatRatio || player.health <= player.maxHealth * ModEntry.config.healthToEatRatio ) {
                SVObject itemToEat = null;
                foreach ( SVObject item in player.Items.OfType<SVObject>() ) {
                    if ( item.Edibility > 0 ) {
                        //It's a edible item
                        if ( itemToEat == null || ( itemToEat.Edibility / itemToEat.salePrice() < item.Edibility / item.salePrice() ) ) {
                            //Found good edibility per price or just first food
                            itemToEat = item;
                        }
                    }
                }
                if ( itemToEat != null ) {
                    player.eatObject( itemToEat );
                    itemToEat.Stack--;
                    if ( itemToEat.Stack == 0 ) {
                        player.removeItemFromInventory( itemToEat );
                    }
                }
            }
        }

        public static T FindToolFromInventory<T>( bool fromEntireInventory ) where T : Tool
        {
            Player player = Game.player;
            T find = null;
            if ( player.CurrentTool is T ) {
                return player.CurrentTool as T;
            }
            if ( fromEntireInventory ) {
                foreach ( Item item in player.Items ) {
                    if ( item is T t ) {
                        find = t;
                        break;
                    }
                }
            }
            return find;
        }
        public static void AutoFishing( BobberBar bar )
        {
            IReflectionHelper reflection = Helper.Reflection;

            IReflectedField<float> bobberSpeed = reflection.GetField<float>( bar, "bobberBarSpeed" );

            float barPos = reflection.GetField<float>( bar, "bobberBarPos" ).GetValue();
            int barHeight = reflection.GetField<int>( bar, "bobberBarHeight" ).GetValue();
            float fishPos = reflection.GetField<float>( bar, "bobberPosition" ).GetValue();
            float treasurePos = reflection.GetField<float>( bar, "treasurePosition" ).GetValue();
            float distanceFromCatching = reflection.GetField<float>( bar, "distanceFromCatching" ).GetValue();
            bool treasureCaught = reflection.GetField<bool>( bar, "treasureCaught" ).GetValue();
            bool treasure = reflection.GetField<bool>( bar, "treasure" ).GetValue();
            float treasureApeearTimer = reflection.GetField<float>( bar, "treasureAppearTimer" ).GetValue();
            float bobberBarSpeed = bobberSpeed.GetValue();

            float top = barPos, bottom = barPos + barHeight;

            if ( treasure && treasureApeearTimer <= 0 && !treasureCaught ) {
                if ( !catchingTreasure && distanceFromCatching > 0.7f ) {
                    catchingTreasure = true;
                }
                if ( catchingTreasure && distanceFromCatching < 0.3f ) {
                    catchingTreasure = false;
                }
                if ( catchingTreasure ) {
                    fishPos = treasurePos;
                }
            }

            float strength = ( fishPos - ( barPos + barHeight / 2 ) ) / 16f;
            float distance = fishPos - top;

            float threshold = Cap( ModEntry.config.CPUThresholdFishing, 0, 0.5f );
            if ( distance < threshold * barHeight || distance > ( 1 - threshold ) * barHeight ) {
                bobberBarSpeed = strength;
            }

            bobberSpeed.SetValue( bobberBarSpeed );
        }

        public static float Cap( float f, float min, float max )
        {
            return f < min ? min : ( f > max ? max : f );
        }

        public static string Format( string format, params object[] args )
        {
            return string.Format( format, args );
        }

        public static string GetRealLocation( string key )
        {
            string ret = "";
            for ( int i = 0 ; i < key.Length ; i++ ) {
                if ( key[i] >= '0' && key[i] <= '9' ) {
                    break;
                }
                ret += key[i];
            }
            return ret;
        }

        public static List<FarmAnimal> GetAnimalsList( Player player )
        {
            List<FarmAnimal> list = new List<FarmAnimal>();
            if ( player.currentLocation is Farm farm ) {
                foreach ( SerializableDictionary<long, FarmAnimal> animal in farm.animals ) {
                    foreach ( KeyValuePair<long, FarmAnimal> kv in animal ) {
                        list.Add( kv.Value );
                    }
                }
            } else if ( player.currentLocation is AnimalHouse house ) {
                foreach ( SerializableDictionary<long, FarmAnimal> animal in house.animals ) {
                    foreach ( KeyValuePair<long, FarmAnimal> kv in animal ) {
                        list.Add( kv.Value );
                    }
                }
            }
            return list;
        }

        public static void LetAnimalsInHome()
        {
            Farm farm = Game.getFarm();
            foreach ( SerializableDictionary<long, FarmAnimal> dic in farm.animals.ToList() ) {
                foreach ( KeyValuePair<long, FarmAnimal> kv in dic ) {
                    FarmAnimal animal = kv.Value;
                    animal.warpHome( farm, animal );
                }
            }
        }

        public static void ShowHUDMessage( string message, int duration = 3500 )
        {
            HUDMessage hudMessage = new HUDMessage( message, 3 )
            {
                noIcon = true,
                timeLeft = duration
            };
            Game.addHUDMessage( hudMessage );
        }

        public static Rectangle Expand( Rectangle rect, int radius )
        {
            return new Rectangle( rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius );
        }

        public static RectangleE ExpandE( Rectangle rect, int radius )
        {
            return new RectangleE( rect.Left - radius, rect.Top - radius, 2 * radius, 2 * radius );
        }

        public static void DrawSimpleTextbox( SpriteBatch batch, string text, int x, int y, SpriteFont font, Item item = null )
        {
            Vector2 stringSize = font.MeasureString( text );
            if ( x < 0 ) {
                x = 0;
            }
            if ( y < 0 ) {
                y = 0;
            }
            int rightX = (int)stringSize.X + Game.tileSize / 2 + 8;
            if ( item != null ) {
                rightX += Game.tileSize;
            }
            if ( x + rightX > Game.viewport.Width ) {
                x = Game.viewport.Width - rightX;
            }
            int bottomY = (int)stringSize.Y + 32;
            if ( item != null ) {
                bottomY = (int)( Game.tileSize * 1.2 ) + 32;
            }
            if ( bottomY + y > Game.viewport.Height ) {
                y = Game.viewport.Height - bottomY;
            }
            IClickableMenu.drawTextureBox( batch, Game.menuTexture, new Rectangle( 0, 256, 60, 60 ), x, y, rightX, bottomY, Color.White, 1f, true );
            if ( !string.IsNullOrEmpty( text ) ) {
                Vector2 vector2 = new Vector2( x + Game.tileSize / 4, y + bottomY / 2 - stringSize.Y / 2 );
                Utility.drawTextWithShadow( batch, text, font, vector2, Color.Black );
            }
            item?.drawInMenu( batch, new Vector2( x + (int)stringSize.X + 24, y + 16 ), 1.0f, 1.0f, 0.9f, false );
        }

        public static void DrawSimpleTextbox( SpriteBatch batch, string text, SpriteFont font, Item item = null )
        {
            DrawSimpleTextbox( batch, text, Game.getMouseX() + Game.tileSize / 2, Game.getMouseY() + Game.tileSize / 2 + 16, font, item );
        }

        public static string GetKeyForQuality( int fishQuality )
        {
            switch ( fishQuality ) {
                case 1: return "quality.silver";
                case 2: return "quality.gold";
                case 3: return "quality.iridium";
            }
            return "quality.normal";
        }

        public static Color GetColorForQuality( int fishQuality )
        {
            switch ( fishQuality ) {
                case 1: return Color.AliceBlue;
                case 2: return Color.Tomato;
                case 3: return Color.Purple;
            }
            return Color.WhiteSmoke;
        }

        public static float Round( float val, int exponent )
        {
            return (float)Math.Round( val, exponent, MidpointRounding.AwayFromZero );
        }
        public static float Floor( float val, int exponent )
        {
            int e = 1;
            for ( int i = 0 ; i < exponent ; i++ ) {
                e *= 10;
            }
            return (float)Math.Floor( val * e ) / e;
        }

        public static void DrawString( SpriteBatch batch, SpriteFont font, ref Vector2 location, string text, Color color, float scale, bool next = false )
        {
            Vector2 stringSize = font.MeasureString( text ) * scale;
            batch.DrawString( font, text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f );
            if ( next ) {
                location += new Vector2( stringSize.X, 0 );
            } else {
                location += new Vector2( 0, stringSize.Y + 4 );
            }
        }

        public static int GetFinalSize( int inch )
        {
            return LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en ? inch : (int)Math.Round( inch * 2.54 );
        }

        public static string TryFormat( string str, params object[] args )
        {
            try {
                string ret = string.Format( str, args );
                return ret;
            } catch {
            }
            return "";
        }
    }
}
