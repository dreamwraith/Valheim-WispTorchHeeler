# Changelog

All notable changes to **WispTorchHeeler** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-10-02

### Added
- **Body-Worn Wisplight Customization**: Cosmetic customization for the equipped Wisplight follower orb (the floating `demister_ball` accessory), including color, point light intensity (0.1×–5.0×), and emissive glow multiplier (0.1×–5.0×).
  - *note: Your wisplight now uses your color setup instead of being stuck at stock cyan.*
- **Wisplight Multiplayer Color Sync**: Replicates follower orb color to other clients via ZDO string key `wisptorch_demister_color` with local polling. Light and emission intensity multipliers are client-local and do not replicate.
  - *note: Other players with the mod see your wisplight color. Light and emission multipliers stay local so nobody's 5.0× intensity setting affects your screen.*
- **Wisplight Alpha Transparency**: Optional alpha transparency (`DemisterEnableAlpha`) clamped to a 15% minimum to prevent invisibility.
  - *note: You can tone the orb down, but it's clamped at 15% so you don't accidentally make it invisible.*
- **Configuration Section `9 - Worn Demister`**: Added 6 config entries for Wisplight customization, lighting, and multiplayer sync. `EnableDemisterCustomization` and `DemisterSyncMultiplayer` support server/client sync toggles via `ConditionalConfigSync`.
  - *note: Server admins can toggle server control per-setting if they want to enforce stock wisplights or let players do their thing.*
- **`DemisterPatches`**: `ZNetView.Awake` postfix attaching `WispDemisterColorController` to demister ball objects. Prefab hash pre-filter keeps per-object overhead negligible.

### Changed
- **`WispVisualApplicator`**: Extracted shared rendering logic (lights, particle gradient tinting, material instancing, shader properties) into a standalone class used by both `WispTorchColorController` and `WispDemisterColorController`, removing duplicated code.
- **Config event wiring moved to `Initialize()` methods**: `Plugin.cs` now calls `WispTorchColorController.Initialize()` and `WispDemisterColorController.Initialize()` rather than wiring events inline.

## [1.1.0] - 2026-09-29

### Added
- **PlayerBase Support on Lamps**: Added `PlayerBase` components and trigger colliders to all custom Dvergr lamps on the `character_trigger` layer so they suppress enemy spawns and count toward player base structure recognition.
  - *note: I managed to release custom lamps that didn't actually count as base structures. You can now build them without enemies spawning all up in your base.*
- **Dynamic Spawn Suppression Slider**: Added the `PlayerBaseMistRadiusBonus` server-synced config setting, allowing spawn suppression to scale from the native 20m base up to matching the full mist clearance radius.
  - *note: If you want your safe zone to match your clear view, now it can. If you leave it at 0, it stays at the native 20m default so the game balance still balances... mostly.*
- **Normalized Native Baselines**: Standardized internal base values across all tiers to 24m mist clearance, 10m light range, 1.0 light intensity, and 20m PlayerBase suppression radius.
  - *note: Each fixture originally inherited whatever numbers Iron Gate gave its donor prefab. I should have looked way closer at the default values on the prefabs I was using, as this made the configuration confusing as all hell. Now everything is normalized on the same base values, so the math can math much mathier.*

### Changed
- **Linear Tier Scaling Toward Grand Demister**: Re-adjusted default mist clearance multipliers to scale in clean +0.75x (+18m) steps up to the 90m Grand Demister default:
  - Tier 1 (Large Torch): 1.50x (36m)
  - Tier 2 (Small Lamp): 2.25x (54m)
  - Tier 3 (Large Lamp): 3.00x (72m)
  - Tier 4 (Grand Lamp): 3.75x (90m)
  - *note: The earlier progression was uneven because I was eyeballing multiplier jumps. It now steps by an even 18 meters per tier.*
- **Normalized Light Scaling**: Light ranges and intensities now scale off common 10m and 1.0 baselines across all tiers.
  - *note: The large demister prop had a native 20m light range that doubled on top of my multipliers and created a miniature sun. It now scales from 10m like everything else.*
- **Clean Config Descriptions**: Rewrote config descriptions to state final numbers relative to stated baselines instead of referencing internal prefab mechanics.
  - *note: All my homies reading config files didn't have a clue what I was rambling about. It now just tells you what baseline number you are multiplying.*
- **Terminology Cleanup**: Replaced occurrences of "vanilla" with "native" across code comments and documentation.
  - *note: I know the community loves the term vanilla, but I differ, and prefer native. Nonetheless... I went against my own principles in a few places originally, and have now cleaned those up.*

### Fixed
- **Crop Growth Blocked by Lamps (Commit `39add34`)**: Fixed piece collider setup in `WispPieceManager` to skip trigger colliders when assigning objects to the `piece` layer. Triggers remain on `character_trigger` (Layer 14).
  - *note: Yes, my torches were accidentally suffocating your cheesy poofs. Unity triggers placed on physical layers make the crop spacing check think there's a solid rock in the dirt (or above the cheesy poofs knob). This was just me using a blunt instrument to solve a collider problem for the build hammer. No more!*

  
## [1.0.0] - 2026-09-24

### Added
- **Initial Release of WispTorchHeeler**.
- **Tiered Mistlands Lighting Installations**:
  - **Tier 1 — Large Wisp Torch (`piece_wisptorch_heeler_lg`)**: Cloned from native wooden wisp torch and scaled physically by 1.25x. Clears 1.5× mist radius (9m, ~2.25× coverage area), 200 HP (double native torch), weather decay immune, placed freeform with building hammer.
  - **Tier 2 — Small Lamp / Dvergr Wisp Lamp (`piece_wisplamp_heeler`)**: Adapts native Dvergr Demister prop at 1.0x scale. Clears 2.0× mist radius (12m, 4× coverage area), 250 HP, weather decay immune, crafted at Forge.
  - **Tier 3 — Large Lamp (`piece_wisplamp_heeler_lg`)**: Cloned from large Dvergr demister model and scaled up by 1.1x. Clears 2.5× mist radius (15m, ~6.25× coverage area), 500 HP, weather decay immune, crafted at Black Forge.
  - **Tier 4 — Grand Lamp (`piece_wisplamp_heeler_grand`)**: Cloned from large Dvergr demister model and scaled up by 1.5x as a colossal monument. Clears 3.0× mist radius (18m, 9× coverage area), 750 HP, weather decay immune, crafted at Black Forge.
- **Interactive "Torch Painter" System**:
  - Real-time in-world piece painting (`P`) and color reset (`LeftShift + P`) hotkeys via native `ZInput` buttons registered with Jötunn.
  - In-game Configuration Manager GUI drawer with RGB/Hex color picker and optional alpha transparency customization (safely clamped to minimum 15% opacity to prevent invisible pieces).
  - **Auto-Paint on Placement**: Automatically stamps newly placed torches and lamps with your active `PainterColor` upon placement via native `IPlaced.OnPlaced()`.
  - In-game HUD text feedback confirmations and crosshair hover prompts (`Paint Light`, `Reset Color`).
- **Multiplayer & Server Synchronization**:
  - Native `ZDO` persistence (`WTH_Color`) synchronizing colors across all clients and saving cleanly with world data with zero desync.
  - Server-enforced lighting policy synchronization via `ConditionalConfigSync` (`EnforceServerDefaultColor`, `ServerDefaultLightColor`, `AllowTorchPainting`).
  - Native Valheim ward (`PrivateArea`) integration preventing unauthorized painting within protected territories, with server admin overrides (`AdminBypassesWardCheck`).
- **Native Wisp Torch Support**:
  - Configurable support (`AffectNativeMistTorches`) allowing painting and custom color defaults on standard native wooden wisp torches (`piece_wisptorch`).
- **Visual Mist Boundary Debugger**:
  - Optional `ShowMistSuppressionRadius` debug setting projecting a real-time ground ring indicator showing the active mist suppression radius of all wisp pieces.
- **Zero Asset Bundles**:
  - Clones and configures native Valheim game prefabs at runtime using Jötunn, ensuring instant loading, zero asset bloat, and full compatibility.
- **Build & Distribution Automation**:
  - Portable MSBuild configuration with automated Thunderstore/Hexium release ZIP packaging.
