# Overview
This is a list of features and configs available in [Joys of Efficiency (JoE)](https://www.nexusmods.com/stardewvalley/mods/2162).

# Features

## Balanced Mode
﻿Did you thought following utilities are a bit cheaty?
﻿This utility lets them not to be executed so often. (almost 1 executing per seconds), and
﻿automation radius will be 1 tile.
﻿
﻿This utility affects to AutoWaterNearbyCrops, AutoPetNearbyAnimals, AutoHarvest, AutoCollectCollectibles, AutoShakeFruitedPlants,
﻿and AutoDigArtifactSpot.
﻿
### CONFIG
﻿* BalancedMode(bool, default:true) - whether this utility is enabled.

## Mine Info GUI
﻿With this utility, you can check how many stones left, and monster kills tally counter, and whether ladder has spawned in mines.
﻿You can see those when your mouse hovered over the icons.
﻿
### CONFIG
﻿MineInfoGUI(bool, default:true) - whether this utility is enabled.

## Auto Water Nearby Crops
 With this utility, the plant crops will be watered automatically.
﻿To use this, you must have at least one of Watering Can and it to have enough water within.
﻿And this costs stamina of the farmer each crops.

﻿### CONFIG
﻿AutoWaterNearbyCrops(bool, default:true) - whether this utility is enabled.
﻿AutoWaterRadius(int, default:1) - How far tiles can be affected by this utility.
﻿FindCanFromInventory(bool, default:true) - Find Can from entire inventory or just held by player.

## Gift Information Tooltip﻿﻿﻿
﻿﻿With this utility, you can check how much do villagers like, dislike the gift before giving it to them.
﻿Text will be localized to languages supported by Stardew Valley itself. (English, German, Japanese, Portuguese, 
﻿Spanish,﻿﻿ Russian, Simplified Chinese)
﻿
﻿### CONFIG
GiftInformation(bool, default:true) - whether this utility is enabled.

## Auto Pet Nearby Animals
﻿﻿With this utility, ﻿you don't have to click on animals to pet, just get close to them.

﻿### CONFIG
AutoPetNearbyAnimals(bool, default:false) - whether this utility is enabled.
﻿AutoPetRadius(int, default:1) - How far tiles can be affected by this utility.

## Auto Animal Door
﻿With this utility, animal doors will open in morning if it is sunny and not winter, and close at the time day changed without click it manually.﻿

﻿### CONFIG
AutoAnimalDoor(bool, default:true) - whether this utility is enabled.

## Auto Fishing
Are you tired to deal with fishing minigame? When this utility is enabled, your computer will play the minigame instead of you.
﻿This was requested by @GastlyMister. Many thanks!

﻿﻿### CONFIG
AutoFishing(bool, default:false) - whether this utility is enabled.
﻿CPUThresholdFishing(float default:0.2 min:0.0 max:0.5) - determines how often cpu reel up the rod.

## Fishing Tweaks
﻿This is a set of tweaks of fishing.
﻿
﻿﻿﻿### CONFIG
AutoReelRod(bool, default:true) - whether it automatically reels up the rod when fish nibbled.
﻿MuchFasterBiting(bool, default:false) - if it is true, fish will bite the bait much faster.

## Fishing Information GUI
This feature shows the information about the fish when playing fishing minigame.
﻿
﻿﻿﻿### CONFIG
FishingInfo(bool, default:true) - whether this utility enabled.

## Auto Gate﻿﻿﻿
﻿Are you tired of clicking fence gates? Then try this.
﻿This feature let gates open when farmer is close to them, and otherwise, close them automatically.
It should work with both single-player and coop game modes﻿.
﻿This was requested by @plaah007. Thanks alot!

﻿### CONFIG
AutoGate(bool, default:true) - whether this utility enabled.

## Auto Eat
﻿This utility let the farmer to eat something automatically when his/her health or stamina is low.
﻿These threshold ratio can be changed.
﻿﻿This was requested by @GastlyMister. thanks!

﻿﻿### CONFIG
AutoEat(bool, default:false) - whether this utility enabled.
﻿StaminaToEatRatio(float, default:0.3 min:0.3 max:0.8) - the threshold ratio of stamina to eat something
﻿HealthToEatRatio(float, default:0.3 min:0.3 max:0.8) -  the threshold ratio of health to eat something

## Auto Harvest
﻿﻿This utility let the farmer to harvest crops (and spring onions) automatically when he/she gets closed to it.

﻿﻿### CONFIG
﻿AutoHarvest(bool, default:false) - whether this utility enabled.
﻿ProtectNectarProducingFlower(bool, default:true) - this option protects flowers producing nectar not to be Auto harvested.
AutoHarvestRadius(int, default:1) - How far tiles can be affected by this utility.
﻿HarvestException(List<int>) - Crop id list not to be auto harvested.﻿
﻿KeyToggleBlackList(Keys, default:"F2") - Add/Remove crop under cursor to/from blacklist.

## Auto Destroy Dead Crops
This utility destorys dead crops automatically when he/she gets closed to it.

﻿﻿### CONFIG
AutoDestroyDeadCropsbool, default:true) - whether this utility enabled.

## Auto Collect Collectibles
﻿﻿﻿This utility let the farmer to collect collectibles (crystals, forages, animal products, and so on) automatically 
﻿when he/she gets closed to it.

﻿﻿### CONFIG
AutoCollectCollectibles(bool, default:false) - whether this utility enabled.
AutoCollectRadius(int, default:1) - How far tiles can be affected by this utility.

## Auto Shake Fruited Plants
﻿This utility shakes fruited tree(pines, acorns, apples, cherries, and so on) and berry bushes
﻿automatically when the farmer gets closed to it.

﻿### CONFIG
AutoShakeFruitedPlamts(bool, default:true) - whether this utility enabled.
AutoShakeRadius(int, default:1) - How far tiles can be affected by this utility.

## Auto Dig Artifact Spot
﻿﻿This utility digs artifact spots nearby the farmer automatically.

﻿### CONFIG
AutoDigArtifactSpot(bool, default:false) - whether this utility enabled.
AutoDigRadius(int, default:1) - How far tiles can be affected by this utility.
﻿FindHoeFromInventory(bool, default:true) - Find hoe from entire inventory or just held by player.

## Auto Deposit Ingredient
﻿﻿﻿This utility will try to deposit ingredient you held to nearby machines automatically.

﻿### CONFIG
AutoDepositIngredient(bool, default:false) - whether this utility enabled.
MachineRadius(int, default:1) - How far tiles can be affected by this utility.

## Auto Pull Machine Result
﻿This utility will try to pull results from nearby machines and give it to the farmer automatically.

﻿### CONFIG
AutoPullMachineResult(bool, default:true) - whether this utility enabled.
MachineRadius(int, default:1) - How far tiles can be affected by this utility.
﻿
## Auto Pet Nearby Pets
﻿﻿Oh, seriously you want to pet pets automatically?
﻿All right, this utility pets nearby pets automatically.

﻿### CONFIG
- AutoPetNearbyPets(bool, default:false) - whether this utility is enabled.
﻿- AutoPetRadius(int, default:1) - How far tiles can be affected by this utility.

## Fishing Probabilities Info
﻿﻿This utility let you know what fish could be caught (and estimated probability of catching) when you are fishing.

﻿### CONFIG
- FishingProbabilitiesInfo(bool, default:false) - whether this utility is enabled.
﻿- ProbBoxX(int, default:100) - Base X location of rendering info box.
﻿- ProbBoxY(int, default:500) - Base Y location of rendering info box.

## Show Shipping Price
This utility shows estimated total shipping price when you opened shipping box.

### CONFIG
- EstimateShippingPrice(bool, default:false) - whether this utility is enabled.

## Unify Flower Colors
This utility unifies flower colors to reduce occupying space according to its species.
In config file, you can change the color using "R, G, B, A" format.

### CONFIG
- JazzColor(Color, default:{0, 255, 255, 255}) - The color of Blue Jazz.
- TulipColor(Color, default:{255, 0, 0, 255}) - The color of Turip.
- PoppyColor(Color, default:{255, 69, 0, 255}) - The color of Poppy.
- SummerSpangleColor(Color, default:{255, 215, 0, 255}) - The color of Summer Spangle.
- FairyRoseColor(Color, default:{216, 191, 216, 255}) - The color of Fairy Rose.