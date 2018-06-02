# SubnauticaRandomizer
A Subnautica Mod that randomizes recipes and changes Blueprint Fragment requirements for replayability

This mod is not released yet.

## Blueprint Fragment Requirements
This will change the number of fragments required in order to add a blueprint. So if you do this to the `config.json` in the mod directory:
```
{
	"Blueprints": {
		"Seamoth": 5,
		"Seaglide": 5
	}
}
```
You will need 5 Seamoth fragments to earn the Seamoth Blueprint and 5 to earn the glider.

If you want a much longer first half of the game, this Blueprints fragment configuration will require a very thorough playthorough:

```
{
	"Blueprints": {
		"BatteryCharger": 5,
		"Beacon": 5,
		"Bioreactor": 10,
		"CyclopsBridge": 5,
		"CyclopsEngine": 3,
		"CyclopsHull": 7,
		"GravTrap": 5,
		"LaserCutter": 5,
		"MobileVehicleBay": 12,
		"ModificationBay": 5,
		"Moonpool": 10,
		"NuclearReactor": 5,
		"PowerCellCharger": 5,
		"PowerTransmitter": 3,
		"Prawn": 5,
		"PrawnDrillArm": 2,
		"PrawnGrapplingArm": 2,
		"PrawnPropulsionCannon": 3,
		"PrawnTorpedoArm": 2,
		"PropulsionCannon": 4,
		"ScannerRoom": 15,
		"Seamoth": 10,
		"Seaglide": 6,
		"StasisRifle": 3,
		"ThermalPlant": 3
	}
}
```

## How to develop/build

- Install QMod
- Git clone
- Add an environment variable to your machine, SUBNAUTICA_DIR, with the location of your subnautica folder.
- Visual Studio's build in the Debug configuration should automatically install the mod in the QMod folder.
