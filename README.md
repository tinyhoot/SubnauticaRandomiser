# Subnautica Randomiser

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/tinyhoot/SubnauticaRandomiser)](https://github.com/tinyhoot/SubnauticaRandomiser/releases)
[![GitHub](https://img.shields.io/github/license/tinyhoot/SubnauticaRandomiser)](https://github.com/tinyhoot/SubnauticaRandomiser/blob/master/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/tinyhoot/subnauticarandomiser/badge/master)](https://www.codefactor.io/repository/github/tinyhoot/subnauticarandomiser/overview/master)
[![wakatime](https://wakatime.com/badge/github/tinyhoot/SubnauticaRandomiser.svg)](https://wakatime.com/badge/github/tinyhoot/SubnauticaRandomiser)

A Subnautica Mod that randomises many aspects of the game for replayability, originally based on [the first Subnautica randomizer](https://github.com/stephenengland/SubnauticaRandomizer). Over time, this project grew to become completely independent and today no longer shares any code with the original. 

On first startup, the mod will randomise using recommended settings. You can either start playing immediately, or customise your experience in the mod options menu. Note that, should you choose to re-randomise from the mod options menu, you must _**restart your game**_ for all changes to properly take effect.

The randomisation persists between play sessions and save games. This means you don't have to play it all through in one sitting, but also that you cannot have different save files with different seeds.

#### This mod randomises:
* Recipes for most craftable things in the game, excluding decorative base pieces and food/water.
* Blueprints found in databoxes
* Fragment spawn rates and locations
* Lifepod spawn location

## Features
- ✔️ No softlocks
- ✔️ Detailed mod options menu
- ✔️ (Probably) Don't spawn in the void
- ✔️ Randomise rewards from scanning a fragment you already know
- ✔️ Include fish, eggs and seeds in recipes
- ✔️ Out-of-order upgrades, so you might acquire e.g. a Seamoth Depth Module 3 long before you ever manage to get Module 1 or 2
- ✔️ Long recipe chains. Do you really need that laser cutter, or do you craft it into Polyaniline for a Rebreather?
- ✔️ Items are balanced on an underlying value logic
   - If you prefer pure chaos, simply turn on Chaotic mode!
- ✔️ Share your seed with friends

## How to Use
1. Install [BepInEx](https://www.nexusmods.com/subnautica/mods/1108)
2. Install [SMLHelper](https://www.nexusmods.com/subnautica/mods/113)
3. Extract this mod into your Subnautica/BepInEx/plugins folder
   * (Optional) Edit the config in the in-game options menu to your liking
   * Press the "Randomise with new seed" button
   * Restart the game to apply your changes
4. Enjoy!

## How to Build
* git clone
* Add a SUBNAUTICA_DIR variable to your PATH pointing to your install directory of Subnautica
* Install BepInEx and SMLHelper
* Copy all dependencies to the project's empty `Dependencies` folder. This includes:
  * BepInEx
  * SMLHelper
  * Several Unity assemblies from the game folder
  * Publicised versions of Subnautica's `Assembly-CSharp.dll`. Start the game once using [the BepinEx publiciser](https://github.com/MrPurple6411/Bepinex-Tools/releases/) to generate them.
* Building in the Release configuration should leave you with a `SubnauticaRandomiser.dll` in `SubnauticaRandomiser/bin/Release/` and automatically update the installed version in `$SUBNAUTICA_DIR/BepInEx/plugins`
