# Teleporters

A 7 Days to Die (V1.2) mod that adds craftable teleporters. Place them around the world, name them, and teleport instantly between any teleporter from a GUI destination picker.

## Features

- **Craftable** at a workbench (unlocked via Electrician perk, tier 3)
- **Naming** - give each teleporter a custom name (up to 24 characters)
- **Networked** - every teleporter can reach every other teleporter
- **Search & filter** - type to filter the destination list
- **Sortable** - sort by name or distance
- **Paginated** - prev/next navigation for large lists
- **Multiplayer** - teleporter data syncs between server and clients
- **Chunk pre-loading** - destination chunks load before teleporting to prevent falling through terrain

## Recipe

Crafted at a **Workbench** (requires **Electrician** perk tier 3):

| Ingredient | Count |
|---|---|
| Forged Iron | 20 |
| Mechanical Parts | 4 |
| Electric Parts | 8 |
| Scrap Polymers | 5 |

## Installation

1. Download the latest release from [Releases](https://github.com/gorgarp/7day_teleportation_mod/releases)
2. Extract the mod folder into your game's `Mods` directory
3. Launch the game — the mod loads automatically

## Building from Source

Requires .NET SDK 8.0+ (for the Roslyn compiler). The mod folder must be at `<GameDir>/Mods/Teleporters/` so the build script can find game assemblies.

```powershell
cd "7 Days to Die/Mods/Teleporters"
powershell -ExecutionPolicy Bypass -File build.ps1
```

This compiles all `.cs` files in `Scripts/` and `Harmony/` into `Teleporters.dll`.

## Usage

1. Craft a Teleporter at a workbench
2. Place it in the world
3. Press **E** to interact — choose **Edit Name** to name it
4. Place more teleporters elsewhere and name them
5. Press **E** on any teleporter and choose **Teleport** to open the destination picker
6. Use the search bar to filter, click column headers to sort, and click **GO** to teleport
