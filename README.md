# WispTorchHeeler

[![GitHub Release](https://img.shields.io/github/v/release/dreamwraith/Valheim-WispTorchHeeler?logo=github&color=1081c2)](https://github.com/dreamwraith/Valheim-WispTorchHeeler/releases)
[![GitHub Downloads](https://img.shields.io/github/downloads/dreamwraith/Valheim-WispTorchHeeler/total?logo=github&color=1081c2)](https://github.com/dreamwraith/Valheim-WispTorchHeeler/releases)
[![Last Commit](https://img.shields.io/github/last-commit/dreamwraith/Valheim-WispTorchHeeler?logo=git)](https://github.com/dreamwraith/Valheim-WispTorchHeeler/commits/main)
[![Publish Status](https://img.shields.io/github/actions/workflow/status/dreamwraith/Valheim-WispTorchHeeler/publish.yml?label=Publish%20Portals&logo=githubactions)](https://github.com/dreamwraith/Valheim-WispTorchHeeler/actions)
[![Thunderstore](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fexperimental%2Fpackage%2FDreamWraith%2FWispTorchHeeler%2F&query=%24.latest.version_number&label=Thunderstore&logo=thunderstore&color=2980b9)](https://thunderstore.io/c/valheim/p/DreamWraith/WispTorchHeeler/)
[![Hexium](https://img.shields.io/badge/Hexium-WispTorchHeeler-6c5ce7)](https://valheim.hexium.gg/mods/DreamWraith/WispTorchHeeler)
[![Nexus Mods](https://img.shields.io/badge/Nexus_Mods-4331-da8e35?logo=nexusmods&logoColor=white)](https://www.nexusmods.com/valheim/mods/4331)
[![Game: Valheim](https://img.shields.io/badge/Valheim-Deep_North_%2F_1.x-1b2838?logo=steam&logoColor=white)](https://store.steampowered.com/app/892970/Valheim/)
[![BepInEx Pack](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fexperimental%2Fpackage%2Fdenikson%2FBepInExPack_Valheim%2F&query=%24.latest.version_number&label=BepInEx&color=5B57E7)](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
[![Target: .NET 4.8](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?logo=dotnet)](WispTorchHeeler.csproj)
[![Client & Server](https://img.shields.io/badge/Type-Client%20%26%20Server-blue)](README.md#the-solution)
[![License](https://img.shields.io/github/license/dreamwraith/Valheim-WispTorchHeeler?color=blue)](LICENSE.md)
[![AI Philosophy](https://img.shields.io/badge/AI%20Philosophy-Software%20Craft-2ea44f?logo=github)](https://gist.github.com/dreamwraith/77c91d656c842611bf8c40febf8056f2)

Tiered Mistlands wisp torches and demister lamps with expanded mist clearance, interactive torch painting, body-worn Wisplight customization, and multiplayer color synchronization.

> [!NOTE]
> **Client & Server Compatibility:** This mod is safe to install on local clients and runs cleanly on dedicated servers with zero desyncs and server-enforced policies. For custom pieces in multiplayer, both client and server require the mod.

---

## The Problem

In native Valheim, clearing the thick mist of the Mistlands requires placing dozens of small, fragile native wisp torches in close proximity. This creates:

- **Severe Visual Clutter & Construction Fatigue**: Expansive Mistlands bases, farms, and transit paths quickly devolve into chaotic thickets of sticks.
- **Performance Overhead**: Dozens of overlapping particle system force fields, dynamic point lights, and particle emitters ticking simultaneously cause noticeable framerate drops.
- **Rigid Aesthetics**: Builders are locked into a single fixed cyan/white color palette that cannot be customized to fit different build themes, architectural styles, or interior moods.
- **Fragility**: Native torches possess only 100 HP, breaking easily from collateral combat damage, seeker attacks, or stray dvergr fire.

---

## The Solution

**WispTorchHeeler** introduces four tiered, late-game Mistlands lighting installations with progressively larger mist-clearance radii alongside an interactive, crosshair-driven "Torch Painter" system and multiplayer color synchronization via Valheim's native Zero Data Object (ZDO) architecture:

- **Tiered Mist Clearance**: Four distinct lighting fixtures ranging from reinforced large torches to colossal dwarven monuments, clearing up to **3.75×** the fog radius (~14× coverage area) to drastically reduce piece density and visual clutter.
- **Interactive Torch Painter**: Aim at any placed torch or lamp and press **`P`** to instantly paint its light and particle effects with your chosen color, or **`LeftShift + P`** to reset it.
- **Auto-Paint on Placement**: Newly placed torches and lamps can automatically inherit your active painter color the instant you build them.
- **Body-Worn Wisplight Customization**: Full cosmetic customization for your equipped Wisplight accessory (the floating `demister_ball` follower orb), including custom color, light and emission intensity multipliers, alpha transparency, and multiplayer color sync.
- **Multiplayer & Server-Synchronized**: Stamped directly into each piece's `ZDO` at creation with zero desync. Unpainted pieces can inherit client-chosen defaults or server-enforced color themes, with ward-protection permissions and admin overrides.
- **Zero Asset Bundles**: Cloned cleanly from native Valheim prefabs at runtime using Jötunn, ensuring instant load times and lightweight installation.

---

## Features

### Tiered Lighting Installations

1. **Tier 1 — Large Wisp Torch (`piece_wisptorch_heeler_lg`)**:
   - Cloned from the native wooden wisp torch and scaled physically by **`1.25x`**.
   - **Mist Clearance**: **1.50×** radius (relative to 24.0m baseline: **36.0m** radius, ~2.25× coverage area).
   - **Light**: **1.33×** intensity and **1.25×** range (**12.5m** light radius).
   - **PlayerBase**: **20.0m** base spawn suppression (scales with `PlayerBaseMistRadiusBonus`).
   - **Durability**: **200 HP** and immune to weather decay.
   - **Crafting**: Placed freeform with the building hammer (`Wisp:8,YggdrasilWood:5,Sap:1`).

2. **Tier 2 — Small Lamp (`piece_wisplamp_heeler`)**:
   - In-game Name: *Dvergr Wisp Lamp*
   - Adapts the native Dvergr Demister prop at natural **`1.0x`** scale.
   - **Mist Clearance**: **2.25×** radius (relative to 24.0m baseline: **54.0m** radius, ~5.0× coverage area).
   - **Light**: **1.50×** intensity and **1.50×** range (**15.0m** light radius).
   - **PlayerBase**: **20.0m** base spawn suppression (scales with `PlayerBaseMistRadiusBonus`).
   - **Durability**: **250 HP** and immune to weather decay.
   - **Crafting**: Requires a **Forge** (`Wisp:15,Copper:5,Iron:2,BlackMarble:2,Sap:2`).

3. **Tier 3 — Large Lamp (`piece_wisplamp_heeler_lg`)**:
   - In-game Name: *Large Dvergr Wisp Lamp*
   - Cloned from the large Dvergr demister model and scaled up by **`1.1x`**.
   - **Mist Clearance**: **3.00×** radius (relative to 24.0m baseline: **72.0m** radius, 9.0× coverage area).
   - **Light**: **2.25×** intensity and **1.75×** range (**17.5m** light radius).
   - **PlayerBase**: **20.0m** base spawn suppression (scales with `PlayerBaseMistRadiusBonus`).
   - **Durability**: **500 HP** and immune to weather decay.
   - **Crafting**: Requires a **Black Forge** (`Wisp:22,Copper:8,Iron:3,Eitr:2,BlackMarble:5`).

4. **Tier 4 — Grand Lamp (`piece_wisplamp_heeler_grand`)**:
   - In-game Name: *Grand Dvergr Wisp Lamp*
   - Cloned from the large Dvergr demister model and scaled up by **`1.5x`**.
   - **Mist Clearance**: **3.75×** radius (relative to 24.0m baseline: **90.0m** radius, ~14.0× coverage area).
   - **Light**: Tuned **2.50×** intensity and **2.00×** range (**20.0m** light radius).
   - **PlayerBase**: **20.0m** base spawn suppression (scales with `PlayerBaseMistRadiusBonus`).
   - **Durability**: Colossal **750 HP** and immune to weather decay.
   - **Crafting**: Requires a **Black Forge** (`Wisp:30,Copper:10,Iron:4,Eitr:4,BlackMarble:10`).

### Interactive "Torch Painter" System

* **Choose Color**: Select your desired paintbrush color (`PainterColor`) using the in-game Configuration Manager UI (F1) color picker or config file.
* **Auto-Paint on Placement**: Enable `AutoPaintOnPlacement` in config to automatically paint newly placed torches and lamps with your active color upon hammer placement.
* **Paint in World**: Aim your crosshair at any wisp torch or lamp within reach and press **`P`** (configurable via `PaintTorchHotkey`). An on-screen prompt displays when in range.
* **Reset Piece**: Aim at a painted piece and press **`LeftShift + P`** (configurable via `ResetTorchHotkey`) to revert to default color.
* **Ward Protection & Security**: Non-permitted players cannot paint pieces inside active wards unless permitted by server policy (`AllowPaintingInProtectedWards`) or admin status (`AdminBypassesWardCheck`).
* **Multiplayer Synchronization**: Painted colors synchronize across all clients via ZDO and persistent world storage without requiring external asset bundles.
* **Visual Mist Boundary**: Enable `ShowMistSuppressionRadius` in debug config to project a real-time ground ring showing the active mist suppression radius of all wisp pieces.

### Body-Worn Wisplight Customization

Cosmetic customization for the player-equipped **Wisplight** utility accessory (the flying follower orb, referenced internally and in config as `Demister` / `demister_ball`):

* **Custom Color**: Choose your Wisplight color via `DemisterColor` in the Configuration Manager (F1) or config file.
* **Point Light & Emission Multipliers**: Scale the Wisplight's point light intensity (`DemisterLightIntensityMultiplier`, 0.1×–5.0×) and core shader emission brightness (`DemisterEmissionIntensityMultiplier`, 0.1×–5.0×) independently.
* **Alpha Transparency**: Optional transparency for your Wisplight via `DemisterEnableAlpha`. Clamped to a 15% minimum to prevent invisibility.
* **Idle Hover Speed & Radius Multipliers**: Calms and smooths the Wisplight's idle animation (`DemisterIdleSpeedMultiplier`, 0.0–2.0) and wander radius (`DemisterIdleRadiusMultiplier`, 0.0–2.0) to eliminate rapid fly-like buzzing and jittering around your head.
* **Multiplayer Color Sync**: When enabled (`DemisterSyncMultiplayer`), your Wisplight color is written to the follower orb's ZDO (`wisptorch_demister_color`) and visible to other players running the mod.
* **Intensity Multipliers Are Client-Local**: Your light and emission intensity settings stay strictly local—other players' Wisplights always render on your screen at default calibrated brightness.
* **Strictly Cosmetic**: Does not alter mist suppression radius, clearance force fields, or Wisplight equipment stats.

---

## Configuration

Settings can be customized directly in-game using the BepInEx **Configuration Manager** (F1) or by editing `BepInEx/config/dreamwraith.WispTorchHeeler.cfg`:

| Section | Key | Type | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `1 - General` | `EnableMod (Requires Restart)` | `bool` | `true` | Master switch to enable or disable the functionality of this mod. (Requires Restart) |
| `1 - General` | `AffectNativeMistTorches` | `bool` | `true` | Allows painting and color defaults on standard native wisp torches. |
| `1 - General` | `PlayerBaseMistRadiusBonus` | `float` | `0.0` | Additive bonus percentage (0.0 to 1.0) scaling PlayerBase spawn suppression from a baseline of 20.0m toward active mist clearance radius. At 0.0, all custom pieces use the 20.0m baseline. At 1.0, the suppression radius matches the active mist reveal boundary. |
| `2 - Large Torch` | `Torch_MistClearRangeMultiplier` | `float` | `1.5` | Large torch mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 10.0). |
| `2 - Large Torch` | `Torch_LightIntensityMultiplier` | `float` | `1.33` | Large torch light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 5.0). |
| `2 - Large Torch` | `Torch_LightRangeMultiplier` | `float` | `1.25` | Large torch light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 5.0). |
| `2 - Large Torch` | `Torch_Health` | `float` | `200.0` | Large torch structural health points (1.0 - 5000.0). |
| `2 - Large Torch` | `Torch_CraftingStation` | `string` | `""` | Required station formatted as StationPrefab (e.g. piece_workbench, piece_forge, or `""` for freeform hammer placement). |
| `2 - Large Torch` | `Torch_Recipe` | `string` | `Wisp:8,YggdrasilWood:5,Sap:1` | Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB. |
| `3 - Small Lamp` | `SmallLamp_MistClearRangeMultiplier` | `float` | `2.25` | Tier 2 small lamp mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 10.0). |
| `3 - Small Lamp` | `SmallLamp_LightIntensityMultiplier` | `float` | `1.5` | Tier 2 small lamp light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 5.0). |
| `3 - Small Lamp` | `SmallLamp_LightRangeMultiplier` | `float` | `1.5` | Tier 2 small lamp light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 5.0). |
| `3 - Small Lamp` | `SmallLamp_Health` | `float` | `250.0` | Tier 2 small lamp structural health points (1.0 - 5000.0). |
| `3 - Small Lamp` | `SmallLamp_CraftingStation` | `string` | `forge` | Required station formatted as StationPrefab or Jotunn name (defaults to 'forge'). |
| `3 - Small Lamp` | `SmallLamp_Recipe` | `string` | `Wisp:15,Copper:5,Iron:2,BlackMarble:2,Sap:2` | Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB. |
| `4 - Large Lamp` | `LargeLamp_MistClearRangeMultiplier` | `float` | `3.0` | Tier 3 lamp mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 15.0). |
| `4 - Large Lamp` | `LargeLamp_LightIntensityMultiplier` | `float` | `2.25` | Tier 3 lamp light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 8.0). |
| `4 - Large Lamp` | `LargeLamp_LightRangeMultiplier` | `float` | `1.75` | Tier 3 lamp light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 5.0). |
| `4 - Large Lamp` | `LargeLamp_Health` | `float` | `500.0` | Tier 3 lamp structural health points (1.0 - 5000.0). |
| `4 - Large Lamp` | `LargeLamp_CraftingStation` | `string` | `blackforge` | Required station formatted as StationPrefab or Jotunn name (defaults to 'blackforge'). |
| `4 - Large Lamp` | `LargeLamp_Recipe` | `string` | `Wisp:22,Copper:8,Iron:3,Eitr:2,BlackMarble:5` | Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB. |
| `5 - Grand Lamp` | `GrandLamp_MistClearRangeMultiplier` | `float` | `3.75` | Tier 4 grand lamp mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 15.0). |
| `5 - Grand Lamp` | `GrandLamp_LightIntensityMultiplier` | `float` | `2.5` | Tier 4 grand lamp light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 8.0). |
| `5 - Grand Lamp` | `GrandLamp_LightRangeMultiplier` | `float` | `2.0` | Tier 4 grand lamp light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 8.0). |
| `5 - Grand Lamp` | `GrandLamp_Health` | `float` | `750.0` | Tier 4 grand lamp structural health points (1.0 - 5000.0). |
| `5 - Grand Lamp` | `GrandLamp_CraftingStation` | `string` | `blackforge` | Required station formatted as StationPrefab or Jotunn name (defaults to 'blackforge'). |
| `5 - Grand Lamp` | `GrandLamp_Recipe` | `string` | `Wisp:30,Copper:10,Iron:4,Eitr:4,BlackMarble:10` | Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB. |
| `6 - Lighting Policy` | `EnforceServerDefaultColor` | `bool` | `false` | If true, all clients must render unpainted pieces with the server's default color. |
| `6 - Lighting Policy` | `ServerDefaultLightColor` | `Color` | `#B4F0FF` | Server-wide default color for unpainted pieces when enforced. |
| `6 - Lighting Policy` | `AllowTorchPainting` | `bool` | `true` | Server master toggle allowing players to paint individual pieces. |
| `6 - Lighting Policy` | `AllowPaintingInProtectedWards` | `bool` | `false` | If false, players cannot paint pieces inside wards they lack access to. |
| `6 - Lighting Policy` | `AdminBypassesWardCheck` | `bool` | `true` | If true, verified server admins bypass ward restrictions when painting. |
| `6 - Lighting Policy` | `EnableAlphaCustomization` | `bool` | `false` | Allows customizing light and particle transparency using the alpha slider in the color picker (clamped to min 15%). |
| `7 - Client Lighting` | `DefaultLightColor` | `Color` | `#B4F0FF` | Client's personal default color for unpainted pieces (active when server does not enforce). |
| `8 - Torch Painter` | `PainterColor` | `Color` | `#FF8800` | Active paint color selected by player via UI color picker drawer. |
| `8 - Torch Painter` | `PaintTorchHotkey` | `KeyboardShortcut` | `KeyCode.P` | Hotkey pressed while aiming at a torch/lamp to apply PainterColor. |
| `8 - Torch Painter` | `ResetTorchHotkey` | `KeyboardShortcut` | `KeyCode.P + LeftShift` | Hotkey pressed while aiming at a torch/lamp to clear custom color. |
| `8 - Torch Painter` | `PainterRaycastDistance` | `float` | `10.0` | Maximum distance in meters to aim and paint a piece (2.0 - 25.0). |
| `8 - Torch Painter` | `ShowPainterFeedback` | `bool` | `true` | Shows on-screen HUD text confirmations upon painting/resetting. |
| `8 - Torch Painter` | `AutoPaintOnPlacement` | `bool` | `false` | If true, newly placed torches and lamps are automatically painted with your active PainterColor upon placement. |
| `9 - Worn Demister` | `EnableDemisterCustomization` | `bool` | `true` | Enables cosmetic customization of color, lighting, and emissive properties for your equipped Wisplight accessory. |
| `9 - Worn Demister` | `DemisterSyncMultiplayer` | `bool` | `true` | Broadcasts your Wisplight color to your follower orb ZDO so other players with this mod see your custom color. |
| `9 - Worn Demister` | `DemisterColor` | `Color` | `#B4F0FF` | Active cosmetic color for your Wisplight, particles, and emissive glow. |
| `9 - Worn Demister` | `DemisterLightIntensityMultiplier` | `float` | `1.0` | Multiplier scaling the brightness of the point light emitted by your Wisplight (0.1 - 5.0). |
| `9 - Worn Demister` | `DemisterEmissionIntensityMultiplier` | `float` | `1.0` | Multiplier scaling the shader emission brightness of your Wisplight core ball and glow (0.1 - 5.0). |
| `9 - Worn Demister` | `DemisterEnableAlpha` | `bool` | `false` | Allows the alpha channel of DemisterColor to customize Wisplight particle and material transparency (safely clamped to 15% floor to prevent invisibility). |
| `9 - Worn Demister` | `DemisterIdleSpeedMultiplier` | `float` | `1.0` | Multiplier scaling the idle bobbing and fluttering speed of your equipped Wisplight follower orb (0.0 - 2.0; 0.3 is calm, 1.0 is native Valheim speed, 0.0 freezes idle motion). |
| `9 - Worn Demister` | `DemisterIdleRadiusMultiplier` | `float` | `1.0` | Multiplier scaling the hover and jitter distance of your equipped Wisplight follower orb around your head (0.0 - 2.0; 1.0 is native Valheim distance). |
| `10 - Debug` | `EnableDebugLogs` | `bool` | `false` | Enables verbose diagnostic logging in the BepInEx console. |
| `10 - Debug` | `ShowMistSuppressionRadius` | `bool` | `false` | Projects a ground boundary ring around wisp torches and lamps showing their active mist suppression radius. |

---

## Compatibility

- **Dedicated Servers**: Fully compatible with dedicated servers and singleplayer worlds. Server-synced balance and policy settings are enforced on connecting clients via `ConditionalConfigSync`.
- **Native Wisp Torches**: Works with native Mistlands wisp torches (`piece_wisptorch`) when `AffectNativeMistTorches` is enabled.
- **Ward Protection**: Integrates with native Valheim wards (`PrivateArea`) to prevent unauthorized painting within protected territories.
- **Zero Asset Bundles**: Requires no external Unity assets or bundle files, eliminating bundle desyncs and memory bloat.
- **Mod Managers**: Fully compatible with **Gale**, **Thunderstore Mod Manager**, and **r2modman**.

---

## Installation

> [!TIP]
> **Mod Manager Recommended:** Using a modern mod manager like **Gale** or **Thunderstore** makes installation and automatic dependency updates effortless.

### Mod Manager (Recommended)
1. Install via **Gale**, **Thunderstore Mod Manager**, or **r2modman**.
2. Launch Valheim and enjoy!

### Manual Installation
1. Ensure **BepInExPack Valheim** and **Jötunn (the Valheim Modding Library)** are installed.
2. Download and extract the latest release archive.
3. Place `WispTorchHeeler.dll` into your `Valheim/BepInEx/plugins/` directory.
4. Launch Valheim.

---

## Building from Source

The project uses a portable MSBuild configuration that auto-detects standard Steam paths.

```bash
dotnet build -c Release
```

The compiled assembly will be placed in `bin/Release/net48/WispTorchHeeler.dll`. Building in `Release` configuration also automatically packages the distribution ZIP to `bin/Publish/WispTorchHeeler-<Version>.zip`.

### Custom & CI Paths
For non-standard Steam library locations or mod manager profiles, copy `WispTorchHeeler.csproj.user.example` to `WispTorchHeeler.csproj.user` in the project root (this file is git-ignored and automatically loaded by MSBuild):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <GamePath>D:\SteamLibrary\steamapps\common\Valheim</GamePath>
    <BepInExCorePath>$(UserProfile)\AppData\Roaming\com.kesomannen.gale\valheim\profiles\<ProfileName>\BepInEx\core</BepInExCorePath>
  </PropertyGroup>
</Project>
```

---

## Packaging, Publishing & Releases

All developer automation tools for release management, packaging, and publishing to **Thunderstore**, **Hexium**, and **Nexus Mods** are consolidated into the single PowerShell 7 entrypoint [.scripts/modtools.ps1](.scripts/modtools.ps1), powered by the shared [**DW.ValheimModTools**](https://github.com/DreamWraith/DW-ValheimModTools) module:

- **Release Management**: pwsh ./.scripts/modtools.ps1 release compiles in Release, creates mod & source archives, extracts changelog notes, and publishes GitHub Releases (Draft by default, or published with -Publish) using the gh CLI.
- **Packaging & Version Bumping**: pwsh ./.scripts/modtools.ps1 package -Bump Patch increments SemVer in WispTorchHeeler.csproj, updates manifest.json, and bundles distribution archives.
- **Portal Publishing**: pwsh ./.scripts/modtools.ps1 publish -Target All uploads directly to Thunderstore, Hexium, and Nexus Mods APIs.
- **CI/CD Workflow**: [.github/workflows/publish.yml](.github/workflows/publish.yml) provides an automated GitHub Actions workflow to publish to Thunderstore, Hexium, and Nexus Mods whenever a GitHub Release is published.

For detailed documentation on flags, workflows, and secret configuration, see [.scripts/README.md](.scripts/README.md).

---

## License, Author Notes
- This project is licensed under the GNU General Public License v3.0 - see the [LICENSE.md](LICENSE.md) file for details.
- [**A Small Note from Me about Valheim Modding Specifically**](https://gist.github.com/dreamwraith/98564f8441dc234bfadd7e2b605c694c) - Thoughts on open-source modding, community inclusivity, and anti-gatekeeping.
- [**A Note on AI, Software Craft, and Why This Code Exists**](https://gist.github.com/dreamwraith/77c91d656c842611bf8c40febf8056f2) - Personal essay on software craft, human agency, and engineering responsibility.

