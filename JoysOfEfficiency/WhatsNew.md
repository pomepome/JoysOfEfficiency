# Overview
This is a changelog from 1.1.14-beta

# Changelog
## 1.1.14-beta
- Moved Utilities to Util.cs
- Added Balanced mode
- Fixed Collect Collectibles feature couldn't work in coop and mine
- Changed distance determining algorithm
- Changed AutoShakeNearbyFruitedTree to AutoShakeNearbyFruitedPlants

## 1.1.15-beta
- Adjusted GiftInfo Window size
- Added FasterRunningSpeed Function

## 1.1.16-beta
- Tweaked Auto Gate Function

## 1.1.17-beta
- AddedSpeedMultiplier will be capped from 1 to 19
- Fixed Bug that continuously throws NullReferenceException in barn, coop, etc.
- Added AutoDepositIngredient and AutoPullMachineResult function.
- Removed collecting crub pot from AutoHarvest. Use AutoPullMachineResult instead.

## 1.1.18-beta
- Adjusted some default settings to be more fair.
- Added AutoPetNearbyPets feature.
- Added Pot Support for AutoWaterNearbyCrops, AutoDestroyNearDeadCrops and AutoHarvest.
- Added CJBCheats Detection

## 1.1.19-beta
- Improved Machine detection algorithm.
- Added ProtectNectarProducingFlower option to AutoHarvest.

## 1.1.20-beta
- Fixed bug that DepositIngredient did not work with furnace well.

## 1.1.21-beta
- You must face to/back-to-back with fence to activate AutoGate

## 1.1.22-beta
- Reduced lag when using ProtectNectarProducingFlower.
- Fixed CanMax value did not changed by its upgrade level.
- Added FPS Counter.

## 1.1.23-beta
- Fixed auto things couldn't work when not holding watering can

## 1.1.24-beta-fokson
- Tidied and organized code a bit
- Reorganized config menu
- Added rebalanced automatic actions, such as watering and harvesting, to be a bit truer to how a player would act
- Rebalanced watering uses the shape and approximate cooldown of the watering can upgrade level you have
- Destroy Dead Crops plays a cutting sound when used