
using System;
using System.Collections.Generic;
using HarmonyLib;
using JoysOfEfficiency.Automation;
using JoysOfEfficiency.Core;
using JoysOfEfficiency.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using Object = StardewValley.Object;
using HarmonyObj = HarmonyLib.Harmony;

namespace JoysOfEfficiency.Harmony
{
    internal class HarmonyPatcher
    {
        private static readonly HarmonyObj Harmony = new HarmonyObj("com.pome.joysofefficiency");

        public static bool UseToolKeyDown { get; set; }

        public static void DoPatching()
        {
            Harmony.PatchAll();
        }

        public static void OverrideUseButton()
        {
            UseToolKeyDown = true;
        }

    }

    [HarmonyPatch(typeof(FishingRod), "tickUpdate")]
    class Patch01
    {
        private static IReflectionHelper Reflection => InstanceHolder.Reflection;
        private static InputState Input => InstanceHolder.Input;
        private static Config Config => InstanceHolder.Config;

        private static Logger Logger = new Logger("tickUpdate");
        static bool Prefix(ref FishingRod __instance)
        {
            tickUpdate(__instance, Game1.currentGameTime, Game1.player);
            return false;
        }

        public static void tickUpdate(FishingRod rod, GameTime time, Farmer who)
        {
            Farmer lastUser = who;
            Reflection.GetField<NetEvent0>(rod, "beginReelingEvent").GetValue().Poll();
            Reflection.GetField<NetEvent0>(rod, "putAwayEvent").GetValue().Poll();
            Reflection.GetField<NetEvent0>(rod, "startCastingEvent").GetValue().Poll();
            Reflection.GetField<NetEventBinary>(rod, "pullFishFromWaterEvent").GetValue().Poll();
            Reflection.GetField<NetEvent1Field<bool, NetBool>>(rod  ,"doneFishingEvent").GetValue().Poll();
            Reflection.GetField<NetEvent0>(rod,"castingEndEnableMovementEvent").GetValue().Poll();

            IReflectedField<int> recastTimerMs = Reflection.GetField<int>(rod, "recastTimerMs");
            IReflectedField<bool> _hasPlayerAdjustedBobber = Reflection.GetField<bool>(rod, "_hasPlayerAdjustedBobber");
            IReflectedField<int> _totalMotionBufferIndex = Reflection.GetField<int>(rod, "_totalMotionBufferIndex");
            IReflectedField<Vector2> _lastAppliedMotion = Reflection.GetField<Vector2>(rod, "_lastAppliedMotion");

            NetVector2 _totalMotion = Reflection.GetField<NetVector2>(rod, "_totalMotion").GetValue();
            int whichFish = Reflection.GetField<int>(rod, "whichFish").GetValue();
            int fishQuality = Reflection.GetField<int>(rod, "fishQuality").GetValue();
            bool usedGamePadToCast = Reflection.GetField<bool>(rod, "usedGamePadToCast").GetValue();
            Vector2[] _totalMotionBuffer = Reflection.GetField<Vector2[]>(rod, "_totalMotionBuffer").GetValue();

            IReflectedMethod getAddedDistance = Reflection.GetMethod(rod, "getAddedDistance");
            IReflectedMethod startCasting = Reflection.GetMethod(rod, "startCasting");
            IReflectedMethod calculateTimeUntilFishingBite = Reflection.GetMethod(rod, "calculateTimeUntilFishingBite");
            IReflectedMethod calculateBobberTile = Reflection.GetMethod(rod, "calculateBobberTile");

            if (recastTimerMs.GetValue ()  > 0 && who.IsLocalPlayer)
            {
                if (Input.GetMouseState().LeftButton == ButtonState.Pressed || Game1.didPlayerJustClickAtAll() || Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton))
                {
                    recastTimerMs.SetValue( recastTimerMs.GetValue() - time.ElapsedGameTime.Milliseconds);
                    if (recastTimerMs.GetValue() <= 0)
                    {
                        recastTimerMs.SetValue(0);
                        if (Game1.activeClickableMenu == null)
                            who.BeginUsingTool();
                    }
                }
                else
                    recastTimerMs.SetValue(0);
            }
            if (rod.isFishing && !Game1.shouldTimePass())
            {
                switch (Game1.activeClickableMenu)
                {
                    case null:
                    case BobberBar _:
                        break;
                    default:
                        return;
                }
            }
            if (who.CurrentTool != null && who.CurrentTool.Equals(rod) && who.UsingTool)
                who.CanMove = false;
            else if (Game1.currentMinigame == null && (who.CurrentTool == null || !(who.CurrentTool is FishingRod) || !who.UsingTool))
            {
                if (FishingRod.chargeSound == null || !FishingRod.chargeSound.IsPlaying || !who.IsLocalPlayer)
                    return;
                FishingRod.chargeSound.Stop(AudioStopOptions.Immediate);
                FishingRod.chargeSound = null;
                return;
            }
            for (int index = rod.animations.Count - 1; index >= 0; --index)
            {
                if (rod.animations[index].update(time))
                    rod.animations.RemoveAt(index);
            }
            if (rod.sparklingText != null && rod.sparklingText.update(time))
                rod.sparklingText = null;
            if (rod.castingChosenCountdown > 0.0)
            {
                rod.castingChosenCountdown -= time.ElapsedGameTime.Milliseconds;
                if (rod.castingChosenCountdown <= 0.0)
                {
                    switch (who.FacingDirection)
                    {
                        case 0:
                            who.FarmerSprite.animateOnce(295, 1f, 1);
                            who.CurrentTool.Update(0, 0, who);
                            break;
                        case 1:
                            who.FarmerSprite.animateOnce(296, 1f, 1);
                            who.CurrentTool.Update(1, 0, who);
                            break;
                        case 2:
                            who.FarmerSprite.animateOnce(297, 1f, 1);
                            who.CurrentTool.Update(2, 0, who);
                            break;
                        case 3:
                            who.FarmerSprite.animateOnce(298, 1f, 1);
                            who.CurrentTool.Update(3, 0, who);
                            break;
                    }
                    if (who.FacingDirection == 1 || who.FacingDirection == 3)
                    {
                        float num1 = Math.Max(128f, (float)(rod.castingPower * (double)(getAddedDistance.Invoke<int>(who) + 4) * 64.0)) - 8f;
                        float y = 0.005f;
                        float num2 = num1 * (float)Math.Sqrt(y / (2.0 * (num1 + 96.0)));
                        float animationInterval = (float)(2.0 * (num2 / (double)y)) + ((float)Math.Sqrt(num2 * (double)num2 + 2.0 * y * 96.0) - num2) / y;
                        if (lastUser.IsLocalPlayer)
                            rod.bobber.Set(new Vector2(who.getStandingX() + (who.FacingDirection == 3 ? -1f : 1f) * num1, who.getStandingY()));
                        rod.animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(170, 1903, 7, 8), animationInterval, 1, 0, who.Position + new Vector2(0.0f, -96f), false, false, who.getStandingY() / 10000f, 0.0f, Color.White, 4f, 0.0f, 0.0f, Game1.random.Next(-20, 20) / 100f)
                        {
                            motion = new Vector2((who.FacingDirection == 3 ? -1f : 1f) * num2, -num2),
                            acceleration = new Vector2(0.0f, y),
                            endFunction = rod.castingEndFunction,
                            timeBasedMotion = true
                        });
                    }
                    else
                    {
                        float num1 = -Math.Max(128f, (float)(rod.castingPower * (double)(getAddedDistance.Invoke<int>(who) + 3) * 64.0));
                        float num2 = Math.Abs(num1 - 64f);
                        if (lastUser.FacingDirection == 0)
                        {
                            num1 = -num1;
                            num2 += 64f;
                        }
                        float y = 0.005f;
                        float num3 = (float)Math.Sqrt(2.0 * y * num2);
                        float animationInterval = (float)(Math.Sqrt(2.0 * (num2 - (double)num1) / y) + num3 / (double)y) * 1.05f;
                        if (lastUser.FacingDirection == 0)
                            animationInterval *= 1.05f;
                        if (lastUser.IsLocalPlayer)
                            rod.bobber.Set(new Vector2(who.getStandingX(), who.getStandingY() - num1));
                        rod.animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(170, 1903, 7, 8), animationInterval, 1, 0, who.Position + new Vector2(24f, -96f), false, false, rod.bobber.Y / 10000f, 0.0f, Color.White, 4f, 0.0f, 0.0f, Game1.random.Next(-20, 20) / 100f)
                        {
                            alphaFade = 0.0001f,
                            motion = new Vector2(0.0f, -num3),
                            acceleration = new Vector2(0.0f, y),
                            endFunction = rod.castingEndFunction,
                            timeBasedMotion = true
                        });
                    }
                    _hasPlayerAdjustedBobber.SetValue(false);
                    rod.castedButBobberStillInAir = true;
                    rod.isCasting = false;
                    if (who.IsLocalPlayer)
                        who.currentLocation.playSound("cast");
                    if (who.IsLocalPlayer && Game1.soundBank != null)
                    {
                        FishingRod.reelSound = Game1.soundBank.GetCue("slowReel");
                        FishingRod.reelSound.SetVariable("Pitch", 1600);
                        FishingRod.reelSound.Play();
                    }
                }
            }
            else if (!rod.isTimingCast && rod.castingChosenCountdown <= 0.0)
                who.jitterStrength = 0.0f;
            if (rod.isTimingCast)
            {
                if (FishingRod.chargeSound == null && Game1.soundBank != null)
                    FishingRod.chargeSound = Game1.soundBank.GetCue("SinWave");
                if (who.IsLocalPlayer && FishingRod.chargeSound != null && !FishingRod.chargeSound.IsPlaying)
                    FishingRod.chargeSound.Play();
                rod.castingPower = Math.Max(0.0f, Math.Min(1f, rod.castingPower + rod.castingTimerSpeed * time.ElapsedGameTime.Milliseconds));
                if (who.IsLocalPlayer && FishingRod.chargeSound != null)
                    FishingRod.chargeSound.SetVariable("Pitch", 2400f * rod.castingPower);
                if (rod.castingPower == 1.0 || rod.castingPower == 0.0)
                    rod.castingTimerSpeed = -rod.castingTimerSpeed;
                who.armOffset.Y = (float)(2.0 * Math.Round(Math.Sin(DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 250.0), 2));
                who.jitterStrength = Math.Max(0.0f, rod.castingPower - 0.5f);
                if (!who.IsLocalPlayer ||
                    (usedGamePadToCast || Input.GetMouseState().LeftButton != ButtonState.Released)
                    &&
                    (!usedGamePadToCast || !Game1.options.gamepadControls || !Input.GetGamePadState().IsButtonUp(Buttons.X))
                    || !Game1.areAllOfTheseKeysUp(Game1.GetKeyboardState(), Game1.options.useToolButton)
                    || AutoFisher.AfkMode && Math.Abs(rod.castingPower - Config.ThrowPower) >= 0.005f)
                {
                    return;
                }
                startCasting.Invoke();
            }
            else if (rod.isReeling)
            {
                if (who.IsLocalPlayer && Game1.didPlayerJustClickAtAll())
                {
                    if (Game1.isAnyGamePadButtonBeingPressed())
                        Game1.lastCursorMotionWasMouse = false;
                    switch (who.FacingDirection)
                    {
                        case 0:
                            who.FarmerSprite.setCurrentSingleFrame(76);
                            break;
                        case 1:
                            who.FarmerSprite.setCurrentSingleFrame(72, 100);
                            break;
                        case 2:
                            who.FarmerSprite.setCurrentSingleFrame(75);
                            break;
                        case 3:
                            who.FarmerSprite.setCurrentSingleFrame(72, 100, false, true);
                            break;
                    }
                    who.armOffset.Y = (float)Math.Round(Math.Sin(DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 250.0), 2);
                    who.jitterStrength = 1f;
                }
                else
                {
                    switch (who.FacingDirection)
                    {
                        case 0:
                            who.FarmerSprite.setCurrentSingleFrame(36);
                            break;
                        case 1:
                            who.FarmerSprite.setCurrentSingleFrame(48, 100);
                            break;
                        case 2:
                            who.FarmerSprite.setCurrentSingleFrame(66);
                            break;
                        case 3:
                            who.FarmerSprite.setCurrentSingleFrame(48, 100, false, true);
                            break;
                    }
                    who.stopJittering();
                }
                who.armOffset = new Vector2(Game1.random.Next(-10, 11) / 10f, Game1.random.Next(-10, 11) / 10f);
                rod.bobberTimeAccumulator += time.ElapsedGameTime.Milliseconds;
            }
            else if (rod.isFishing)
            {
                if (lastUser.IsLocalPlayer)
                    rod.bobber.Y += (float)(0.100000001490116 * Math.Sin(DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 250.0));
                who.canReleaseTool = true;
                rod.bobberTimeAccumulator += time.ElapsedGameTime.Milliseconds;
                switch (who.FacingDirection)
                {
                    case 0:
                        who.FarmerSprite.setCurrentFrame(44);
                        break;
                    case 1:
                        who.FarmerSprite.setCurrentFrame(89);
                        break;
                    case 2:
                        who.FarmerSprite.setCurrentFrame(70);
                        break;
                    case 3:
                        who.FarmerSprite.setCurrentFrame(89, 0, 10, 1, true, false);
                        break;
                }
                who.armOffset.Y = (float)(Math.Round(Math.Sin(DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 250.0), 2) + (who.FacingDirection == 1 || who.FacingDirection == 3 ? 1.0 : -1.0));
                if (!who.IsLocalPlayer)
                    return;
                if (rod.timeUntilFishingBite != -1.0)
                {
                    rod.fishingBiteAccumulator += time.ElapsedGameTime.Milliseconds;
                    if (rod.fishingBiteAccumulator > (double)rod.timeUntilFishingBite)
                    {
                        rod.fishingBiteAccumulator = 0.0f;
                        rod.timeUntilFishingBite = -1f;
                        rod.isNibbling = true;
                        who.currentLocation.localSound("fishBite");
                        Rumble.rumble(0.75f, 250f);
                        rod.timeUntilFishingNibbleDone = FishingRod.maxTimeToNibble;
                        if (Game1.currentMinigame == null)
                            Game1.screenOverlayTempSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(395, 497, 3, 8), new Vector2(lastUser.getStandingX() - Game1.viewport.X, lastUser.getStandingY() - 128 - 8 - Game1.viewport.Y), false, 0.02f, Color.White)
                            {
                                scale = 5f,
                                scaleChange = -0.01f,
                                motion = new Vector2(0.0f, -0.5f),
                                shakeIntensityChange = -0.005f,
                                shakeIntensity = 1f
                            });
                        rod.timePerBobberBob = 1f;
                    }
                }
                if (rod.timeUntilFishingNibbleDone == -1.0 || rod.hit)
                    return;
                rod.fishingNibbleAccumulator += time.ElapsedGameTime.Milliseconds;
                if (rod.fishingNibbleAccumulator <= (double)rod.timeUntilFishingNibbleDone)
                    return;
                rod.fishingNibbleAccumulator = 0.0f;
                rod.timeUntilFishingNibbleDone = -1f;
                rod.isNibbling = false;
                rod.timeUntilFishingBite = calculateTimeUntilFishingBite.Invoke<float>(calculateBobberTile.Invoke<Vector2>(), false, who);
            }
            else if (who.UsingTool && rod.castedButBobberStillInAir)
            {
                Vector2 zero1 = Vector2.Zero;
                if (!Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveDownButton))
                {
                    if (Game1.options.gamepadControls)
                    {
                        GamePadState oldPadState = Game1.oldPadState;
                        if (!Game1.oldPadState.IsButtonDown(Buttons.DPadDown) && GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y >= 0.0)
                            goto label_97;
                    }
                    else
                        goto label_97;
                }
                if (who.FacingDirection != 2 && who.FacingDirection != 0)
                {
                    zero1.Y += 4f;
                    _hasPlayerAdjustedBobber.SetValue(true);
                }
                label_97:
                if (!Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveRightButton))
                {
                    if (Game1.options.gamepadControls)
                    {
                        GamePadState oldPadState = Game1.oldPadState;
                        if (!Game1.oldPadState.IsButtonDown(Buttons.DPadRight) && GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X <= 0.0)
                            goto label_102;
                    }
                    else
                        goto label_102;
                }
                if (who.FacingDirection != 1 && who.FacingDirection != 3)
                {
                    zero1.X += 2f;
                    _hasPlayerAdjustedBobber.SetValue(true);
                }
                label_102:
                if (!Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveUpButton))
                {
                    if (Game1.options.gamepadControls)
                    {
                        GamePadState oldPadState = Game1.oldPadState;
                        if (!Game1.oldPadState.IsButtonDown(Buttons.DPadUp) && GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y <= 0.0)
                            goto label_107;
                    }
                    else
                        goto label_107;
                }
                if (who.FacingDirection != 0 && who.FacingDirection != 2)
                {
                    zero1.Y -= 4f;
                    _hasPlayerAdjustedBobber.SetValue(true);
                }
                label_107:
                if (!Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.moveLeftButton))
                {
                    if (Game1.options.gamepadControls)
                    {
                        GamePadState oldPadState = Game1.oldPadState;
                        if (!Game1.oldPadState.IsButtonDown(Buttons.DPadLeft) && GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X >= 0.0)
                            goto label_112;
                    }
                    else
                        goto label_112;
                }
                if (who.FacingDirection != 3 && who.FacingDirection != 1)
                {
                    zero1.X -= 2f;
                    _hasPlayerAdjustedBobber.SetValue(true);
                }
                label_112:
                if (!_hasPlayerAdjustedBobber.GetValue())
                {
                    Vector2 bobberTile = calculateBobberTile.Invoke<Vector2>();
                    if (!lastUser.currentLocation.isTileFishable((int)bobberTile.X, (int)bobberTile.Y))
                    {
                        if (lastUser.FacingDirection == 3 || lastUser.FacingDirection == 1)
                        {
                            int num = 1;
                            if (bobberTile.Y % 1.0 < 0.5)
                                num = -1;
                            if (lastUser.currentLocation.isTileFishable((int)bobberTile.X, (int)bobberTile.Y + num))
                                zero1.Y += num * 4f;
                            else if (lastUser.currentLocation.isTileFishable((int)bobberTile.X, (int)bobberTile.Y - num))
                                zero1.Y -= num * 4f;
                        }
                        if (lastUser.FacingDirection == 0 || lastUser.FacingDirection == 2)
                        {
                            int num = 1;
                            if (bobberTile.X % 1.0 < 0.5)
                                num = -1;
                            if (lastUser.currentLocation.isTileFishable((int)bobberTile.X + num, (int)bobberTile.Y))
                                zero1.X += num * 4f;
                            else if (lastUser.currentLocation.isTileFishable((int)bobberTile.X - num, (int)bobberTile.Y))
                                zero1.X -= num * 4f;
                        }
                    }
                }
                if (who.IsLocalPlayer)
                {
                    rod.bobber.Set(rod.bobber + zero1);
                    _totalMotion.Set(_totalMotion.Value + zero1);
                }
                if (rod.animations.Count <= 0)
                    return;
                Vector2 zero2 = Vector2.Zero;
                Vector2 vector2;
                if (who.IsLocalPlayer)
                {
                    vector2 = _totalMotion.Value;
                }
                else
                {
                    _totalMotionBuffer[_totalMotionBufferIndex.GetValue()] = _totalMotion.Value;
                    for (int index = 0; index < _totalMotionBuffer.Length; ++index)
                        zero2 += _totalMotionBuffer[index];
                    vector2 = zero2 / _totalMotionBuffer.Length;
                    _totalMotionBufferIndex.SetValue((_totalMotionBufferIndex.GetValue() + 1) % _totalMotionBuffer.Length);
                }
                rod.animations[0].position -= _lastAppliedMotion.GetValue();
                _lastAppliedMotion.SetValue(vector2);
                rod.animations[0].position += vector2;
            }
            else if (rod.showingTreasure)
                who.FarmerSprite.setCurrentSingleFrame(0);
            else if (rod.fishCaught)
            {
                if (!Game1.isFestival())
                {
                    who.faceDirection(2);
                    who.FarmerSprite.setCurrentFrame(84);
                }
                if (Game1.random.NextDouble() < 0.025)
                    who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(653, 858, 1, 1), 9999f, 1, 1, who.Position + new Vector2(Game1.random.Next(-3, 2) * 4, -32f), false, false, (float)(who.getStandingY() / 10000.0 + 1.0 / 500.0), 0.04f, Color.LightBlue, 5f, 0.0f, 0.0f, 0.0f)
                    {
                        acceleration = new Vector2(0.0f, 0.25f)
                    });
                if (!who.IsLocalPlayer || Input.GetMouseState().LeftButton != ButtonState.Pressed && !Game1.didPlayerJustClickAtAll() && !Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton) && !HarmonyPatcher.UseToolKeyDown)
                {
                    return;
                }
                HarmonyPatcher.UseToolKeyDown = false;
                who.currentLocation.localSound("coin");
                if (!rod.treasureCaught)
                {
                    recastTimerMs.SetValue(200);
                    Object @object = new Object(whichFish, 1, false, -1, fishQuality);
                    if (whichFish == GameLocation.CAROLINES_NECKLACE_ITEM)
                        @object.questItem.Value = true;
                    if (whichFish == 79)
                    {
                        @object = who.currentLocation.tryToCreateUnseenSecretNote(lastUser);
                        if (@object == null)
                            return;
                    }
                    if (rod.caughtDoubleFish)
                        @object.Stack = 2;
                    bool fromFishPond = rod.fromFishPond;
                    lastUser.completelyStopAnimatingOrDoingAction();
                    rod.doneFishing(lastUser, !fromFishPond);
                    if (Game1.isFestival() || lastUser.addItemToInventoryBool(@object))
                        return;
                    Game1.activeClickableMenu = new ItemGrabMenu(new List<Item>
                    {
             @object
          }, rod).setEssential(true);
                }
                else
                {
                    rod.fishCaught = false;
                    rod.showingTreasure = true;
                    who.UsingTool = true;
                    int initialStack = 1;
                    if (rod.caughtDoubleFish)
                        initialStack = 2;
                    bool inventoryBool = lastUser.addItemToInventoryBool(new Object(whichFish, initialStack, false, -1, fishQuality));
                    rod.animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(64, 1920, 32, 32), 500f, 1, 0, lastUser.Position + new Vector2(-32f, -160f), false, false, (float)(lastUser.getStandingY() / 10000.0 + 1.0 / 1000.0), 0.0f, Color.White, 4f, 0.0f, 0.0f, 0.0f)
                    {
                        motion = new Vector2(0.0f, -0.128f),
                        timeBasedMotion = true,
                        endFunction = rod.openChestEndFunction,
                        extraInfoForEndBehavior = inventoryBool ? 0 : 1,
                        alpha = 0.0f,
                        alphaFade = -1f / 500f
                    });
                }
            }
            else if (who.UsingTool && rod.castedButBobberStillInAir && rod.doneWithAnimation)
            {
                switch (who.FacingDirection)
                {
                    case 0:
                        who.FarmerSprite.setCurrentFrame(39);
                        break;
                    case 1:
                        who.FarmerSprite.setCurrentFrame(89);
                        break;
                    case 2:
                        who.FarmerSprite.setCurrentFrame(28);
                        break;
                    case 3:
                        who.FarmerSprite.setCurrentFrame(89, 0, 10, 1, true, false);
                        break;
                }
                who.armOffset.Y = (float)Math.Round(Math.Sin(DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 250.0), 2);
            }
            else
            {
                if (rod.castedButBobberStillInAir || whichFish == -1 || (rod.animations.Count <= 0 || rod.animations[0].timer <= 500.0) || Game1.eventUp)
                    return;
                lastUser.faceDirection(2);
                lastUser.FarmerSprite.setCurrentFrame(57);
            }
        }
    }
}
