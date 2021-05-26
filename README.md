# SubnauticaRandomiser
A Subnautica Mod that randomises recipes for replayability.

This can make the game considerably more difficult, and the varying availability of ingredients may also make it harder to collect enough of what you need.

The randomisation persists between play sessions and save games. If you decide to stop playing for the day, everything will remain randomised as it was. In case you need it, there is a mod menu option for randomising everything from scratch again.

#### This mod randomises:
* All recipes for basic and advanced materials
* All recipes for tools, equipment, and vehicles
* Most ingredients required for base building
  * Decorative pieces like chairs or beds are unaffected

## How to Use
1. Install [QModManager](https://www.nexusmods.com/subnautica/mods/201)
2. Install [SMLHelper](https://www.nexusmods.com/subnautica/mods/113)
3. Extract this mod into your Subnautica/QMods folder
4. (Optional) Edit the config in the in-game options menu to your liking
   * Press the "Randomise Again" button twice
5. Enjoy!

## How to Build
* Install QModManager and SMLHelper
* git clone
* In Visual Studio, update the project's assembly references to point to the correct locations on your computer.
  * For more information, see [QMod Wiki](https://github.com/SubnauticaModding/QModManager/wiki/Libraries)
* Building in the Release configuration should leave you with a `SubnauticaRandomiser.dll` in `SubnauticaRandomiser/bin/Release/`
