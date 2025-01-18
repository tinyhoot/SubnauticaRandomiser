# Subnautica Randomiser

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/tinyhoot/SubnauticaRandomiser)](https://github.com/tinyhoot/SubnauticaRandomiser/releases)
[![GitHub](https://img.shields.io/github/license/tinyhoot/SubnauticaRandomiser)](https://github.com/tinyhoot/SubnauticaRandomiser/blob/master/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/tinyhoot/subnauticarandomiser/badge/master)](https://www.codefactor.io/repository/github/tinyhoot/subnauticarandomiser/overview/master)
[![wakatime](https://wakatime.com/badge/github/tinyhoot/SubnauticaRandomiser.svg)](https://wakatime.com/badge/github/tinyhoot/SubnauticaRandomiser)

A Subnautica Mod that randomises many aspects of the game for replayability, originally based on [the first Subnautica randomizer](https://github.com/stephenengland/SubnauticaRandomizer). Over time, this project grew to become completely independent and today no longer shares any code with the original. 

If you do not change any of the options in the mod options menu the mod will randomise using recommended settings. You can either start playing immediately, or customise your experience in the mod options menu.

The randomisation persists between play sessions and within save games. This means you don't have to play it all through in one sitting, and that you can swap between different save files with different seeds as you like. However, you **cannot** change any options of an ongoing game.

#### This mod randomises:
* Recipes for most craftable things in the game, excluding decorative base pieces and food/water.
* Blueprints found in databoxes
* Fragment spawn rates and locations
* Lifepod spawn location
* Door codes and supply box contents

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
2. Install [Nautilus](https://www.nexusmods.com/subnautica/mods/1262)
3. Extract this mod into your `Subnautica/BepInEx/plugins` folder
   * (Optional) Edit the config in the in-game options menu to your liking
4. Start a new game
5. Enjoy!

## How to Build
* `git clone --recurse-submodules`
* NuGet should automatically download all dependencies for you. If it does not, perform a NuGet restore.
  * (Optional) Create a `GameDirectory.targets` file in the same folder as the freshly cloned `SubnauticaRandomiser.csproj` and add the path to your
    local Subnautica install directory to it. An example file can be found [here](https://github.com/tinyhoot/HootLib-Subnautica/blob/main/HootLib/Example_GameDirectory.targets).
* Building the project will leave you with a `SubnauticaRandomiser.dll` in the default build directory. If you created a 
  `GameDirectory.targets` file it will also automatically copy all necessary mod files to your 
  `Subnautica/BepInEx/plugins` directory for quick testing.

## How Does It Work?
Under the hood, the randomiser creates a new Unity GameObject for storing all randomisation logic. It then attaches Components as needed, depending on which config options are set. Only those Components which are actually needed are attached to the GameObject. Components primarily communicate via events, which means they do not rely on each other and can be individually turned on/off with no repercussions.

There are some basic Components that are responsible for steering the overall logic and are always attached. These are the Core Logic module and the Progression Manager. The core logic is what actually runs the main loop where game entities are randomised one by one, while the manager keeps track of overall game progression and ensures no softlocks. Events are invoked between the two of them as certain milestones are reached.

Here's a rough diagram of the overall structure and execution flow:

![Structure Diagram](https://github.com/tinyhoot/SubnauticaRandomiser/blob/master/StructureDiagram.png)
