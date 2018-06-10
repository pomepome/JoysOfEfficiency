using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

using JoysOfEfficiency.OptionsElements;
using JoysOfEfficiency.Utils;

namespace JoysOfEfficiency
{
    using Game = Game1;

    public class JOEMenu : IClickableMenu
    {
        private static readonly string[] fpsLocationStringKey = new string[] { "option.tl", "option.bl", "option.br", "option.tr" };

        private ModEntry mod;

        private ITranslationHelper translation;
        private readonly IMonitor mon;

        private readonly List<MenuTab> tabs = new List<MenuTab>();

        private ClickableTextureComponent upCursor;
        private ClickableTextureComponent downCursor;

        private Rectangle tabAuto;
        private Rectangle tabGUI;
        private Rectangle tabCheats;
        private Rectangle tabControls;

        private int tabIndex = 0;
        private int firstIndex = 0;

        private readonly SpriteFont font = Game.smallFont;
        private readonly string tabAutoString;
        private readonly string tabGUIString;
        private readonly string tabCheatsString;
        private readonly string tabControlsString;

        private bool isListening;

        private bool isFirstTime;
        private ModifiedInputListener listener = null;

        public JOEMenu( int width, int height, ModEntry mod ) : base( Game.viewport.Width / 2 - width / 2, Game.viewport.Height / 2 - height / 2, width, height, true )
        {

            this.mod = mod;
            translation = mod.Helper.Translation;
            upCursor = new ClickableTextureComponent( "up-arrow", new Rectangle( xPositionOnScreen + this.width + Game.tileSize / 4, yPositionOnScreen + Game.tileSize, 11 * Game.pixelZoom, 12 * Game.pixelZoom ), "", "", Game.mouseCursors, new Rectangle( 421, 459, 11, 12 ), Game.pixelZoom );
            downCursor = new ClickableTextureComponent( "down-arrow", new Rectangle( xPositionOnScreen + this.width + Game.tileSize / 4, yPositionOnScreen + this.height - Game.tileSize, 11 * Game.pixelZoom, 12 * Game.pixelZoom ), "", "", Game.mouseCursors, new Rectangle( 421, 472, 11, 12 ), Game.pixelZoom );

            tabAutoString = translation.Get( "tab.auto" );
            Vector2 Size = font.MeasureString( tabAutoString );
            tabAuto = new Rectangle( xPositionOnScreen - (int)Size.X - 20, yPositionOnScreen, ( (int)Size.X + 32 ), 64 );

            tabGUIString = translation.Get( "tab.gui" );
            Size = font.MeasureString( tabGUIString );
            tabGUI = new Rectangle( xPositionOnScreen - (int)Size.X - 20, yPositionOnScreen + 68, ( (int)Size.X + 32 ), 64 );

            tabCheatsString = translation.Get( "tab.cheats" );
            Size = font.MeasureString( tabCheatsString );
            tabCheats = new Rectangle( xPositionOnScreen - (int)Size.X - 20, yPositionOnScreen + 136, ( (int)Size.X + 32 ), 64 );

            tabControlsString = translation.Get( "tab.controls" );
            Size = font.MeasureString( tabControlsString );
            tabControls = new Rectangle( xPositionOnScreen - (int)Size.X - 20, yPositionOnScreen + 204, ( (int)Size.X + 32 ), 64 );

            Configuration.Config c = ModEntry.config;
            {
                /// Automation Tab

                MenuTab tab = new MenuTab();
                tab.AddOptionsElement( new ModifiedCheckBox( "Balanced Mode", 20, c.balancedMode, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedCheckBox( " > Only automate when walking (shift)", 27, c.mustBeWalking, OnCheckboxValueChanged, ( i => !c.balancedMode ) ) );
                tab.AddOptionsElement( new ModifiedCheckBox( " > Play sound when cooldown ready", 28, c.balanceReadySound, OnCheckboxValueChanged, ( i => !c.balancedMode ) ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Refill Watering Can", 13, c.autoRefillWateringCan, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Water Crops", 2, c.autoWaterNearbyCrops, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "Radius", 3, c.autoWaterRadius, 1, 3, OnSliderValueChanged, ( () => !c.autoWaterNearbyCrops || c.balancedMode ) ) );
                tab.AddOptionsElement( new ModifiedCheckBox( " > Only when holding tool", 16, c.mustHoldCan, OnCheckboxValueChanged, ( i => !( c.autoWaterNearbyCrops || c.autoRefillWateringCan ) ) ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Harvest Crops", 11, c.autoHarvest, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "Radius", 5, c.autoHarvestRadius, 1, 3, OnSliderValueChanged, ( () => !c.autoHarvest || c.balancedMode ) ) );
                tab.AddOptionsElement( new ModifiedCheckBox( " > Ignore flowers near beehouse", 25, c.protectNectarProducingFlower, OnCheckboxValueChanged, i => !c.autoHarvest ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Destroy Dead Crops", 12, c.autoDestroyDeadCrops, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Collect Collectibles", 14, c.autoCollectCollectibles, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "Radius", 6, c.autoCollectRadius, 1, 3, OnSliderValueChanged, ( () => !c.autoCollectCollectibles || c.balancedMode ) ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Shake Bushes and Trees", 15, c.autoShakeFruitedPlants, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "Radius", 7, c.autoShakeRadius, 1, 3, OnSliderValueChanged, ( () => !c.autoShakeFruitedPlants || c.balancedMode ) ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Dig Artifact Spots", 17, c.autoDigArtifactSpot, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "Radius", 8, c.autoDigRadius, 1, 3, OnSliderValueChanged, ( () => !c.autoDigArtifactSpot || c.balancedMode ) ) );
                tab.AddOptionsElement( new ModifiedCheckBox( " > Only when holding tool", 18, c.mustHoldHoe, OnCheckboxValueChanged, i => !c.autoDigArtifactSpot ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Pet Nearby Dog / Cat", 24, c.autoPetNearbyPets, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "Radius", 4, c.autoPetRadius, 1, 3, OnSliderValueChanged, ( () => !c.autoPetNearbyAnimals || c.balancedMode ) ) );
                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Pet Nearby Livestock", 3, c.autoPetNearbyAnimals, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Deposit Ingredients", 22, c.autoDepositIngredient, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Pull Machine Results,", 23, c.autoPullMachineResult, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "Radius", 10, c.machineRadius, 1, 3, OnSliderValueChanged, ( () => !( c.autoPullMachineResult || c.autoDepositIngredient ) || c.balancedMode ) ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Gate", 9, c.autoGate, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Door (Barn / Coop)", 4, c.autoAnimalDoor, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Hook Fish", 6, c.autoReelRod, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedCheckBox( "Auto-Fish", 5, c.autoFishing, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( " > CPU Threshold", 0, (int)( c.CPUThresholdFishing * 10 ), 0, 5, OnSliderValueChanged, ( () => !c.autoFishing ), Format ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "AutoEat", 10, c.autoEat, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( " > Health-To-Eat Ratio", 2, (int)( c.healthToEatRatio * 10 ), 3, 8, OnSliderValueChanged, ( () => !c.autoEat ), Format ) );
                tab.AddOptionsElement( new ModifiedSlider( " > Stamina-To-Eat Ratio", 1, (int)( c.staminaToEatRatio * 10 ), 3, 8, OnSliderValueChanged, ( () => !c.autoEat ), Format ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tabs.Add( tab );
            }
            {
                /// GUI Tab

                MenuTab tab = new MenuTab();
                tab.AddOptionsElement( new ModifiedCheckBox( "MineInfoGUI", 0, c.mineInfoGUI, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "GiftInformation", 1, c.giftInformation, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "FishingInfo", 8, c.fishingInfo, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "FPSCounter", 26, c.FPSCounter, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new ModifiedSlider( "FPSlocation", 11, c.FPSlocation, 0, 3, OnSliderValueChanged, ( () => !c.FPSCounter ), Format ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tabs.Add( tab );
            }

            {
                /// Cheat Tab

                MenuTab tab = new MenuTab();
                tab.AddOptionsElement( new ModifiedCheckBox( "MuchFasterBiting", 7, c.muchFasterBiting, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "FastToolUpgrade", 19, c.fastToolUpgrade, OnCheckboxValueChanged ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tab.AddOptionsElement( new ModifiedCheckBox( "FasterRunningSpeed", 21, c.fasterRunningSpeed, OnCheckboxValueChanged, i => ModEntry.IsCJBCheatsOn ) );
                tab.AddOptionsElement( new ModifiedSlider( "AddedSpeedMultiplier", 9, c.addedSpeedMultiplier, 1, 19, OnSliderValueChanged, ( () => !c.fasterRunningSpeed ) ) );
                tab.AddOptionsElement( new MenuEmptySpace() );


                tabs.Add( tab );
            }

            {
                //Controls Tab
                MenuTab tab = new MenuTab();
                tab.AddOptionsElement( new ModifiedInputListener( this, "Show Menu", 0, c.keyShowMenu, translation, OnInputListnerChanged, OnStartListening ) );
                tab.AddOptionsElement( new MenuEmptySpace() );

                tabs.Add( tab );
            }
            mon = mod.Monitor;
        }
        private void OnStartListening( int i, ModifiedInputListener listener )
        {
            isListening = true;
            this.listener = listener;
        }
        private void OnInputListnerChanged( int index, Keys value )
        {
            Configuration.Config c = ModEntry.config;
            if ( index == 0 ) {
                c.keyShowMenu = value;
            }
            mod.WriteConfig();
            isListening = false;
            listener = null;
        }
        private void OnCheckboxValueChanged( int index, bool value )
        {
            Configuration.Config c = ModEntry.config;
            switch ( index ) {
                case 0: c.mineInfoGUI = value; break;
                case 1: c.giftInformation = value; break;
                case 2: c.autoWaterNearbyCrops = value; break;
                case 3: c.autoPetNearbyAnimals = value; break;
                case 4: c.autoAnimalDoor = value; break;
                case 5: c.autoFishing = value; break;
                case 6: c.autoReelRod = value; break;
                case 7: c.muchFasterBiting = value; break;
                case 8: c.fishingInfo = value; break;
                case 9: c.autoGate = value; break;
                case 10: c.autoEat = value; break;
                case 11: c.autoHarvest = value; break;
                case 12: c.autoDestroyDeadCrops = value; break;
                case 13: c.autoRefillWateringCan = value; break;
                case 14: c.autoCollectCollectibles = value; break;
                case 15: c.autoShakeFruitedPlants = value; break;
                case 16: c.mustHoldCan = value; break;
                case 17: c.autoDigArtifactSpot = value; break;
                case 18: c.mustHoldHoe = value; break;
                case 19: c.fastToolUpgrade = value; break;
                case 20: c.balancedMode = value; break;
                case 21: c.fasterRunningSpeed = value; break;
                case 22: c.autoDepositIngredient = value; break;
                case 23: c.autoPullMachineResult = value; break;
                case 24: c.autoPetNearbyPets = value; break;
                case 25: c.protectNectarProducingFlower = value; break;
                case 26: c.FPSCounter = value; break;
                case 27: c.mustBeWalking = value; break;
                case 28: c.balanceReadySound = value; break;
                default: return;
            }
            mod.WriteConfig();
        }
        private void OnSliderValueChanged( int index, int value )
        {
            Configuration.Config c = ModEntry.config;
            if ( index == 0 ) {
                c.CPUThresholdFishing = value / 10.0f;
            }
            if ( index == 1 ) {
                c.staminaToEatRatio = value / 10.0f;
            }
            if ( index == 2 ) {
                c.healthToEatRatio = value / 10.0f;
            }
            if ( index == 3 ) {
                c.autoWaterRadius = value;
            }
            if ( index == 4 ) {
                c.autoPetRadius = value;
            }
            if ( index == 5 ) {
                c.autoHarvestRadius = value;
            }
            if ( index == 6 ) {
                c.autoCollectRadius = value;
            }
            if ( index == 7 ) {
                c.autoShakeRadius = value;
            }
            if ( index == 8 ) {
                c.autoDigRadius = value;
            }
            if ( index == 9 ) {
                c.addedSpeedMultiplier = value;
            }
            if ( index == 10 ) {
                c.machineRadius = value;
            }
            if ( index == 11 ) {
                c.FPSlocation = value;
            }
            mod.WriteConfig();
        }

        private string Format( int id, int value )
        {
            if ( id >= 0 && id < 3 ) {
                return string.Format( "{0:f1}", value / 10f );
            } else if ( id == 11 ) {
                return mod.Helper.Translation.Get( fpsLocationStringKey[value] );
            }
            return value + "";
        }

        public override void update( GameTime time )
        {
            base.update( time );
            upCursor.visible = firstIndex > 0;
            downCursor.visible = !CanDrawAll();
        }

        public override void receiveScrollWheelAction( int direction )
        {
            if ( isListening ) {
                return;
            }
            //Scroll with Mouse Wheel
            if ( direction > 0 && upCursor.visible ) {
                Game.playSound( "shwip" );
                firstIndex--;
            } else if ( direction < 0 && downCursor.visible ) {
                Game.playSound( "shwip" );
                firstIndex++;
            }
        }

        public override void draw( SpriteBatch b )
        {
            int x = 16, y = 16;


            drawTextureBox( b, tabAuto.Left, tabAuto.Top, tabAuto.Width, tabAuto.Height, Color.White * ( tabIndex == 0 ? 1.0f : 0.6f ) );
            b.DrawString( Game.smallFont, tabAutoString, new Vector2( tabAuto.Left + 16, tabAuto.Top + ( tabAuto.Height - font.MeasureString( tabAutoString ).Y ) / 2 ), Color.Black * ( tabIndex == 0 ? 1.0f : 0.6f ) );

            drawTextureBox( b, tabGUI.Left, tabGUI.Top, tabGUI.Width, tabGUI.Height, Color.White * ( tabIndex == 1 ? 1.0f : 0.6f ) );
            b.DrawString( Game.smallFont, tabGUIString, new Vector2( tabGUI.Left + 16, tabGUI.Top + ( tabGUI.Height - font.MeasureString( tabGUIString ).Y ) / 2 ), Color.Black * ( tabIndex == 1 ? 1.0f : 0.6f ) );

            drawTextureBox( b, tabCheats.Left, tabCheats.Top, tabCheats.Width, tabCheats.Height, Color.White * ( tabIndex == 2 ? 1.0f : 0.6f ) );
            b.DrawString( Game.smallFont, tabCheatsString, new Vector2( tabCheats.Left + 16, tabCheats.Top + ( tabCheats.Height - font.MeasureString( tabCheatsString ).Y ) / 2 ), Color.Black * ( tabIndex == 2 ? 1.0f : 0.6f ) );

            drawTextureBox( b, tabControls.Left, tabControls.Top, tabControls.Width, tabControls.Height, Color.White * ( tabIndex == 3 ? 1.0f : 0.6f ) );
            b.DrawString( Game.smallFont, tabControlsString, new Vector2( tabControls.Left + 16, tabControls.Top + ( tabControls.Height - font.MeasureString( tabControlsString ).Y ) / 2 ), Color.Black * ( tabIndex == 3 ? 1.0f : 0.6f ) );

            drawTextureBox( b, Game.menuTexture, new Rectangle( 0, 256, 60, 60 ), xPositionOnScreen, yPositionOnScreen, this.width, this.height, Color.White, 1.0f, false );
            base.draw( b );

            {
                int X = ( Game.viewport.Width - 400 ) / 2;
                drawTextureBox( b, X, yPositionOnScreen - 108, 400, 100, Color.White );

                string str = "JOE Settings";
                Vector2 size = Game.dialogueFont.MeasureString( str ) * 1.1f;

                Utility.drawTextWithShadow( b, str, Game.dialogueFont, new Vector2( ( Game.viewport.Width - (int)size.X ) / 2, yPositionOnScreen - 50 - (int)size.Y / 2 ), Color.Black, 1.1f );
            }

            foreach ( OptionsElement element in GetElementsToShow() ) {
                element.draw( b, x + this.xPositionOnScreen, y + yPositionOnScreen );
                y += element.bounds.Height + 16;
            }
            upCursor.draw( b );
            downCursor.draw( b );

            if ( isListening ) {
                Point size = listener.GetListeningMessageWindowSize();
                drawTextureBox( b, ( Game.viewport.Width - size.X ) / 2, ( Game.viewport.Height - size.Y ) / 2, size.X, size.Y, Color.White );
                listener.DrawStrings( b, ( Game.viewport.Width - size.X ) / 2, ( Game.viewport.Height - size.Y ) / 2 );
            }

            Util.DrawCursor();
        }

        public override void performHoverAction( int x, int y )
        {
            base.performHoverAction( x, y );
            upCursor.tryHover( x, y );
            downCursor.tryHover( x, y );
        }

        public override void leftClickHeld( int x, int y )
        {
            if ( isListening ) {
                return;
            }
            base.leftClickHeld( x, y );
            foreach ( OptionsElement element in GetElementsToShow() ) {
                if ( element.bounds.Contains( x - xPositionOnScreen - element.bounds.X / 2, y - yPositionOnScreen - element.bounds.Y / 2 ) ) {
                    element.leftClickHeld( x - element.bounds.X - xPositionOnScreen, y - element.bounds.Y - yPositionOnScreen );
                }
                y -= element.bounds.Height + 16;
            }
        }

        public override void receiveKeyPress( Keys key )
        {
            Configuration.Config c = ModEntry.config;
            if ( isListening ) {
                foreach ( OptionsElement element in tabs[tabIndex].GetElements() ) {
                    element.receiveKeyPress( key );
                }
            } else if ( key == Keys.Escape ) {
                CloseMenu();
            } else if ( key == c.keyShowMenu ) {
                if ( !isFirstTime ) {
                    isFirstTime = true;
                    return;
                }
                CloseMenu();
            }
        }

        private void CloseMenu()
        {
            Game.playSound( "bigDeSelect" );
            Game.activeClickableMenu = null;
        }

        public override void receiveLeftClick( int x, int y, bool playSound = true )
        {
            base.receiveLeftClick( x, y, playSound );
            if ( isListening ) {
                return;
            }
            if ( tabAuto.Contains( x, y ) ) {
                TryToChangeTab( 0 );
                return;
            }
            if ( tabGUI.Contains( x, y ) ) {
                TryToChangeTab( 1 );
                return;
            }
            if ( tabCheats.Contains( x, y ) ) {
                TryToChangeTab( 2 );
                return;
            }
            if ( tabControls.Contains( x, y ) ) {
                TryToChangeTab( 3 );
                return;
            }
            if ( upCursor.bounds.Contains( x, y ) && upCursor.visible ) {
                Game.playSound( "shwip" );
                firstIndex--;
                return;
            }
            if ( downCursor.bounds.Contains( x, y ) && downCursor.visible ) {
                Game.playSound( "shwip" );
                firstIndex++;
                return;
            }
            foreach ( OptionsElement element in GetElementsToShow() ) {
                if ( element.bounds.Contains( x - xPositionOnScreen - element.bounds.X / 2, y - yPositionOnScreen - element.bounds.Y / 2 ) ) {
                    element.receiveLeftClick( x - element.bounds.X - xPositionOnScreen, y - element.bounds.Y - yPositionOnScreen );
                }
                y -= element.bounds.Height + 16;
            }
        }

        public override void releaseLeftClick( int x, int y )
        {
            base.releaseLeftClick( x, y );
            if ( isListening ) {
                return;
            }
            foreach ( OptionsElement element in GetElementsToShow() ) {
                if ( element.bounds.Contains( x - xPositionOnScreen - element.bounds.X / 2, y - yPositionOnScreen - element.bounds.Y / 2 ) ) {
                    element.leftClickReleased( x - element.bounds.X - xPositionOnScreen, y - element.bounds.Y - yPositionOnScreen );
                }
                y -= element.bounds.Height + 16;
            }
        }

        public List<OptionsElement> GetElementsToShow()
        {
            List<OptionsElement> menuElements = tabs[tabIndex].GetElements();
            List<OptionsElement> elements = new List<OptionsElement>();
            int y = 16;
            for ( int i = firstIndex ; i < menuElements.Count ; i++ ) {
                OptionsElement element = menuElements[i];
                int hElem = element is ModifiedSlider ? element.bounds.Height + 4 : element.bounds.Height;
                if ( y + hElem < height ) {
                    y += hElem + 16;
                    elements.Add( element );
                }
            }
            return elements;
        }

        public bool CanDrawAll()
        {
            List<OptionsElement> menuElements = tabs[tabIndex].GetElements();
            int y = 16;
            for ( int i = firstIndex ; i < menuElements.Count ; i++ ) {
                OptionsElement element = menuElements[i];
                int hElem = element is ModifiedSlider ? element.bounds.Height + 4 : element.bounds.Height;
                if ( y + hElem < height ) {
                    y += hElem + 16;
                } else {
                    return false;
                }
            }
            return true;
        }

        private void TryToChangeTab( int which )
        {
            if ( tabIndex != which ) {
                tabIndex = which;
                firstIndex = 0;
                Game.playSound( "drumkit6" );
            }
        }

        public override void receiveRightClick( int x, int y, bool playSound = true ) { }
    }
}
