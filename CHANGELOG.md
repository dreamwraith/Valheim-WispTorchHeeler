# Changelog

All notable changes to **WispTorchHeeler** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
