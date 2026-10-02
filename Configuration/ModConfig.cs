using System;
using BepInEx.Configuration;
using UnityEngine;

namespace WispTorchHeeler.Configuration
{
    public static class ModConfig
    {
        public static ConditionalConfigSync.ConfigSync ConfigSync = null!;

        // 1 - General
        public static ConfigEntry<bool> EnableMod = null!;
        public static ConfigEntry<bool> AffectNativeMistTorches = null!;
        public static ConfigEntry<float> PlayerBaseMistRadiusBonus = null!;

        // 2 - Large Torch
        public static ConfigEntry<float> Torch_MistClearRangeMultiplier = null!;
        public static ConfigEntry<float> Torch_LightIntensityMultiplier = null!;
        public static ConfigEntry<float> Torch_LightRangeMultiplier = null!;
        public static ConfigEntry<float> Torch_Health = null!;
        public static ConfigEntry<string> Torch_CraftingStation = null!;
        public static ConfigEntry<string> Torch_Recipe = null!;

        // 3 - Small Lamp
        public static ConfigEntry<float> SmallLamp_MistClearRangeMultiplier = null!;
        public static ConfigEntry<float> SmallLamp_LightIntensityMultiplier = null!;
        public static ConfigEntry<float> SmallLamp_LightRangeMultiplier = null!;
        public static ConfigEntry<float> SmallLamp_Health = null!;
        public static ConfigEntry<string> SmallLamp_CraftingStation = null!;
        public static ConfigEntry<string> SmallLamp_Recipe = null!;

        // 4 - Large Lamp
        public static ConfigEntry<float> LargeLamp_MistClearRangeMultiplier = null!;
        public static ConfigEntry<float> LargeLamp_LightIntensityMultiplier = null!;
        public static ConfigEntry<float> LargeLamp_LightRangeMultiplier = null!;
        public static ConfigEntry<float> LargeLamp_Health = null!;
        public static ConfigEntry<string> LargeLamp_CraftingStation = null!;
        public static ConfigEntry<string> LargeLamp_Recipe = null!;

        // 5 - Grand Lamp
        public static ConfigEntry<float> GrandLamp_MistClearRangeMultiplier = null!;
        public static ConfigEntry<float> GrandLamp_LightIntensityMultiplier = null!;
        public static ConfigEntry<float> GrandLamp_LightRangeMultiplier = null!;
        public static ConfigEntry<float> GrandLamp_Health = null!;
        public static ConfigEntry<string> GrandLamp_CraftingStation = null!;
        public static ConfigEntry<string> GrandLamp_Recipe = null!;

        // 6 - Lighting Policy
        public static ConfigEntry<bool> EnforceServerDefaultColor = null!;
        public static ConfigEntry<Color> ServerDefaultLightColor = null!;
        public static ConfigEntry<bool> AllowTorchPainting = null!;
        public static ConfigEntry<bool> AllowPaintingInProtectedWards = null!;
        public static ConfigEntry<bool> AdminBypassesWardCheck = null!;
        public static ConfigEntry<bool> EnableAlphaCustomization = null!;

        // 7 - Client Lighting
        public static ConfigEntry<Color> DefaultLightColor = null!;

        // 8 - Torch Painter
        public static ConfigEntry<Color> PainterColor = null!;
        public static ConfigEntry<KeyboardShortcut> PaintTorchHotkey = null!;
        public static ConfigEntry<KeyboardShortcut> ResetTorchHotkey = null!;
        public static Jotunn.Configs.ButtonConfig? ButtonPaintConfig;
        public static Jotunn.Configs.ButtonConfig? ButtonResetConfig;
        public static ConfigEntry<float> PainterRaycastDistance = null!;
        public static ConfigEntry<bool> ShowPainterFeedback = null!;
        public static ConfigEntry<bool> AutoPaintOnPlacement = null!;

        // 9 - Worn Demister
        public static ConfigEntry<bool> EnableDemisterCustomization = null!;
        public static ConfigEntry<bool> DemisterSyncMultiplayer = null!;
        public static ConfigEntry<Color> DemisterColor = null!;
        public static ConfigEntry<float> DemisterLightIntensityMultiplier = null!;
        public static ConfigEntry<float> DemisterEmissionIntensityMultiplier = null!;
        public static ConfigEntry<bool> DemisterEnableAlpha = null!;

        // 10 - Debug
        public static ConfigEntry<bool> EnableDebugLogs = null!;
        public static ConfigEntry<bool> ShowMistSuppressionRadius = null!;

        // Standard cyan/white Mistlands fallback: #B4F0FF -> RGBA(0.706, 0.941, 1.0, 1.0)
        private static readonly Color s_defaultCyan = new Color(0.706f, 0.941f, 1.0f, 1.0f);
        // Standard painter orange: #FF8800 -> RGBA(1.0, 0.533, 0.0, 1.0)
        private static readonly Color s_defaultOrange = new Color(1.0f, 0.533f, 0.0f, 1.0f);

        public static void Initialize(ConfigFile config)
        {
            ConfigSync = new ConditionalConfigSync.ConfigSync(Plugin.ModGUID)
            {
                DisplayName = Plugin.ModName,
                CurrentVersion = Plugin.ModVersion,
                MinimumRequiredVersion = Plugin.ModVersion,
                ModRequired = true
            };

            // 1 - General
            EnableMod = BindSynced(config, "1 - General", "EnableMod (Requires Restart)", true,
                "Master switch to enable or disable the functionality of this mod. (Requires Restart)");

            AffectNativeMistTorches = BindSynced(config, "1 - General", "AffectNativeMistTorches", true,
                "Allows painting and color defaults on standard native wisp torches.");

            PlayerBaseMistRadiusBonus = BindSynced(config, "1 - General", "PlayerBaseMistRadiusBonus", 0.0f,
                "Additive bonus percentage (0.0 to 1.0) scaling PlayerBase spawn suppression from a baseline of 20.0m toward active mist clearance radius. At 0.0, all custom pieces use the 20.0m baseline. At 1.0, the suppression radius matches the active mist reveal boundary. Note: higher values reduce native spawn balance in favor of mist sanctuary.",
                new AcceptableValueRange<float>(0.0f, 1.0f));

            // 2 - Large Torch
            Torch_MistClearRangeMultiplier = BindSynced(config, "2 - Large Torch", "Torch_MistClearRangeMultiplier", 1.5f,
                "Large torch mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 10.0).",
                new AcceptableValueRange<float>(1.0f, 10.0f));

            Torch_LightIntensityMultiplier = BindSynced(config, "2 - Large Torch", "Torch_LightIntensityMultiplier", 1.33f,
                "Large torch light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 5.0).",
                new AcceptableValueRange<float>(0.5f, 5.0f));

            Torch_LightRangeMultiplier = BindSynced(config, "2 - Large Torch", "Torch_LightRangeMultiplier", 1.25f,
                "Large torch light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 5.0).",
                new AcceptableValueRange<float>(0.5f, 5.0f));

            Torch_Health = BindSynced(config, "2 - Large Torch", "Torch_Health", 200.0f,
                "Large torch structural health points (1.0 - 5000.0).",
                new AcceptableValueRange<float>(1.0f, 5000.0f));

            Torch_CraftingStation = BindSynced(config, "2 - Large Torch", "Torch_CraftingStation", "",
                "Required station formatted as StationPrefab (e.g. piece_workbench, piece_forge, or \"\" for freeform hammer placement).");

            Torch_Recipe = BindSynced(config, "2 - Large Torch", "Torch_Recipe", "Wisp:8,YggdrasilWood:5,Sap:1",
                "Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB.",
                customDrawer: RecipeConfigDrawer.Draw);

            // 3 - Small Lamp
            SmallLamp_MistClearRangeMultiplier = BindSynced(config, "3 - Small Lamp", "SmallLamp_MistClearRangeMultiplier", 2.25f,
                "Tier 2 small lamp mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 10.0).",
                new AcceptableValueRange<float>(1.0f, 10.0f));

            SmallLamp_LightIntensityMultiplier = BindSynced(config, "3 - Small Lamp", "SmallLamp_LightIntensityMultiplier", 1.5f,
                "Tier 2 small lamp light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 5.0).",
                new AcceptableValueRange<float>(0.5f, 5.0f));

            SmallLamp_LightRangeMultiplier = BindSynced(config, "3 - Small Lamp", "SmallLamp_LightRangeMultiplier", 1.5f,
                "Tier 2 small lamp light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 5.0).",
                new AcceptableValueRange<float>(0.5f, 5.0f));

            SmallLamp_Health = BindSynced(config, "3 - Small Lamp", "SmallLamp_Health", 250.0f,
                "Tier 2 small lamp structural health points (1.0 - 5000.0).",
                new AcceptableValueRange<float>(1.0f, 5000.0f));

            SmallLamp_CraftingStation = BindSynced(config, "3 - Small Lamp", "SmallLamp_CraftingStation", "forge",
                "Required station formatted as StationPrefab or Jotunn name (defaults to 'forge').");

            SmallLamp_Recipe = BindSynced(config, "3 - Small Lamp", "SmallLamp_Recipe", "Wisp:15,Copper:5,Iron:2,BlackMarble:2,Sap:2",
                "Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB.",
                customDrawer: RecipeConfigDrawer.Draw);

            // 4 - Large Lamp
            LargeLamp_MistClearRangeMultiplier = BindSynced(config, "4 - Large Lamp", "LargeLamp_MistClearRangeMultiplier", 3.0f,
                "Tier 3 lamp mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 15.0).",
                new AcceptableValueRange<float>(1.0f, 15.0f));

            LargeLamp_LightIntensityMultiplier = BindSynced(config, "4 - Large Lamp", "LargeLamp_LightIntensityMultiplier", 2.25f,
                "Tier 3 lamp light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 8.0).",
                new AcceptableValueRange<float>(0.5f, 8.0f));

            LargeLamp_LightRangeMultiplier = BindSynced(config, "4 - Large Lamp", "LargeLamp_LightRangeMultiplier", 1.75f,
                "Tier 3 lamp light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 5.0).",
                new AcceptableValueRange<float>(0.5f, 5.0f));

            LargeLamp_Health = BindSynced(config, "4 - Large Lamp", "LargeLamp_Health", 500.0f,
                "Tier 3 lamp structural health points (1.0 - 5000.0).",
                new AcceptableValueRange<float>(1.0f, 5000.0f));

            LargeLamp_CraftingStation = BindSynced(config, "4 - Large Lamp", "LargeLamp_CraftingStation", "blackforge",
                "Required station formatted as StationPrefab or Jotunn name (defaults to 'blackforge').");

            LargeLamp_Recipe = BindSynced(config, "4 - Large Lamp", "LargeLamp_Recipe", "Wisp:22,Copper:8,Iron:3,Eitr:2,BlackMarble:5",
                "Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB.",
                customDrawer: RecipeConfigDrawer.Draw);

            // 5 - Grand Lamp
            GrandLamp_MistClearRangeMultiplier = BindSynced(config, "5 - Grand Lamp", "GrandLamp_MistClearRangeMultiplier", 3.75f,
                "Tier 4 grand lamp mist clearance radius multiplier (relative to 24.0m baseline, range: 1.0 - 15.0).",
                new AcceptableValueRange<float>(1.0f, 15.0f));

            GrandLamp_LightIntensityMultiplier = BindSynced(config, "5 - Grand Lamp", "GrandLamp_LightIntensityMultiplier", 2.5f,
                "Tier 4 grand lamp light emission intensity multiplier (relative to 1.0 baseline, range: 0.5 - 8.0).",
                new AcceptableValueRange<float>(0.5f, 8.0f));

            GrandLamp_LightRangeMultiplier = BindSynced(config, "5 - Grand Lamp", "GrandLamp_LightRangeMultiplier", 2.0f,
                "Tier 4 grand lamp light emission range radius multiplier (relative to 10.0m baseline, range: 0.5 - 8.0).",
                new AcceptableValueRange<float>(0.5f, 8.0f));

            GrandLamp_Health = BindSynced(config, "5 - Grand Lamp", "GrandLamp_Health", 750.0f,
                "Tier 4 grand lamp structural health points (1.0 - 5000.0).",
                new AcceptableValueRange<float>(1.0f, 5000.0f));

            GrandLamp_CraftingStation = BindSynced(config, "5 - Grand Lamp", "GrandLamp_CraftingStation", "blackforge",
                "Required station formatted as StationPrefab or Jotunn name (defaults to 'blackforge').");

            GrandLamp_Recipe = BindSynced(config, "5 - Grand Lamp", "GrandLamp_Recipe", "Wisp:30,Copper:10,Iron:4,Eitr:4,BlackMarble:10",
                "Crafting recipe string formatted as ItemPrefab:Amount,... parsed against ObjectDB.",
                customDrawer: RecipeConfigDrawer.Draw);

            // 6 - Lighting Policy
            EnforceServerDefaultColor = BindSynced(config, "6 - Lighting Policy", "EnforceServerDefaultColor", false,
                "If true, all clients must render unpainted pieces with the server's default color.");

            ServerDefaultLightColor = BindSynced(config, "6 - Lighting Policy", "ServerDefaultLightColor", s_defaultCyan,
                "Server-wide default color for unpainted pieces when enforced.");

            AllowTorchPainting = BindSynced(config, "6 - Lighting Policy", "AllowTorchPainting", true,
                "Server master toggle allowing players to paint individual pieces.");

            AllowPaintingInProtectedWards = BindSynced(config, "6 - Lighting Policy", "AllowPaintingInProtectedWards", false,
                "If false, players cannot paint pieces inside wards they lack access to.");

            AdminBypassesWardCheck = BindSynced(config, "6 - Lighting Policy", "AdminBypassesWardCheck", true,
                "If true, verified server admins bypass ward restrictions when painting.");

            EnableAlphaCustomization = BindSynced(config, "6 - Lighting Policy", "EnableAlphaCustomization", false,
                "Allows customizing light and particle transparency using the alpha slider in the color picker. When disabled, native opacity is strictly preserved. Alpha is safely clamped to a minimum of 15% to prevent pieces from becoming invisible.");

            // 7 - Client Lighting
            DefaultLightColor = BindClient(config, "7 - Client Lighting", "DefaultLightColor", s_defaultCyan,
                "Client's personal default color for unpainted pieces (active when server does not enforce).");

            // 8 - Torch Painter
            PainterColor = BindClient(config, "8 - Torch Painter", "PainterColor", s_defaultOrange,
                "Active paint color selected by player via UI color picker drawer.");

            PaintTorchHotkey = BindClient(config, "8 - Torch Painter", "PaintTorchHotkey", new KeyboardShortcut(KeyCode.P),
                "Hotkey pressed while aiming at a torch/lamp to apply PainterColor.");

            ResetTorchHotkey = BindClient(config, "8 - Torch Painter", "ResetTorchHotkey", new KeyboardShortcut(KeyCode.P, KeyCode.LeftShift),
                "Hotkey pressed while aiming at a torch/lamp to clear custom color.");

            PainterRaycastDistance = BindClient(config, "8 - Torch Painter", "PainterRaycastDistance", 10.0f,
                "Maximum distance in meters to aim and paint a piece (2.0 - 25.0).",
                new AcceptableValueRange<float>(2.0f, 25.0f));

            ShowPainterFeedback = BindClient(config, "8 - Torch Painter", "ShowPainterFeedback", true,
                "Shows on-screen HUD text confirmations upon painting/resetting.");

            AutoPaintOnPlacement = BindClient(config, "8 - Torch Painter", "AutoPaintOnPlacement", false,
                "If true, newly placed torches and lamps are automatically painted with your active PainterColor upon placement.");

            // Register buttons into Valheim's native ZInput via Jotunn InputManager
            RegisterInputButtons();

            // 9 - Worn Demister
            EnableDemisterCustomization = BindSynced(config, "9 - Worn Demister", "EnableDemisterCustomization", true,
                "Enables cosmetic customization of color, lighting, and emissive properties for your body-worn Demister wisplight accessory.");

            DemisterSyncMultiplayer = BindSynced(config, "9 - Worn Demister", "DemisterSyncMultiplayer", true,
                "Broadcasts your demister color to your wisplight follower orb ZDO so other players with this mod see your custom demister color.");

            DemisterColor = BindClient(config, "9 - Worn Demister", "DemisterColor", s_defaultCyan,
                "Active cosmetic color for your body-worn Demister light, particles, and emissive glow.");

            DemisterLightIntensityMultiplier = BindClient(config, "9 - Worn Demister", "DemisterLightIntensityMultiplier", 1.0f,
                "Multiplier scaling the brightness of the point light emitted by your body-worn Demister (0.1 - 5.0).",
                new AcceptableValueRange<float>(0.1f, 5.0f));

            DemisterEmissionIntensityMultiplier = BindClient(config, "9 - Worn Demister", "DemisterEmissionIntensityMultiplier", 1.0f,
                "Multiplier scaling the shader emission brightness of your body-worn Demister core ball and glow (0.1 - 5.0).",
                new AcceptableValueRange<float>(0.1f, 5.0f));

            DemisterEnableAlpha = BindClient(config, "9 - Worn Demister", "DemisterEnableAlpha", false,
                "Allows the alpha channel of DemisterColor to customize particle and material transparency (safely clamped to 15% floor to prevent invisibility).");

            // 10 - Debug
            EnableDebugLogs = BindClient(config, "10 - Debug", "EnableDebugLogs", false,
                "Enables verbose diagnostic logging in the BepInEx console.");

            ShowMistSuppressionRadius = BindClient(config, "10 - Debug", "ShowMistSuppressionRadius", false,
                "Projects a ground boundary ring around wisp torches and lamps showing their active mist suppression radius.");

            Plugin.LogDebug("Configuration initialized and bound.");
        }

        public const string ButtonPaint = "WTH_Paint";
        public const string ButtonReset = "WTH_Reset";

        private static void RegisterInputButtons()
        {
            try
            {
                ButtonPaintConfig = new Jotunn.Configs.ButtonConfig
                {
                    Name = ButtonPaint,
                    ShortcutConfig = PaintTorchHotkey,
                    ActiveInGUI = false,
                    HintToken = Localization.ModLocalization.TokenPromptPaint
                };
                Jotunn.Managers.InputManager.Instance.AddButton(Plugin.ModGUID, ButtonPaintConfig);

                ButtonResetConfig = new Jotunn.Configs.ButtonConfig
                {
                    Name = ButtonReset,
                    ShortcutConfig = ResetTorchHotkey,
                    ActiveInGUI = false,
                    HintToken = Localization.ModLocalization.TokenPromptReset
                };
                Jotunn.Managers.InputManager.Instance.AddButton(Plugin.ModGUID, ButtonResetConfig);

                Plugin.LogDebug("[ModConfig] Registered input buttons 'WTH_Paint' and 'WTH_Reset' via Jotunn InputManager.");
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"[ModConfig] Failed to register Jotunn input buttons: {exception.Message}");
            }
        }

        private static ConfigEntry<T> BindSynced<T>(ConfigFile config, string section, string key, T defaultValue, string description, AcceptableValueBase? acceptableValues = null, Action<ConfigEntryBase>? customDrawer = null)
        {
            var configDescription = customDrawer != null
                ? new ConfigDescription(description, acceptableValues, new ConfigurationManagerAttributes { CustomDrawer = customDrawer })
                : new ConfigDescription(description, acceptableValues);
            var entry = config.Bind(section, key, defaultValue, configDescription);
            ConfigSync.AddConfigEntry(entry);
            return entry;
        }

        private static ConfigEntry<T> BindClient<T>(ConfigFile config, string section, string key, T defaultValue, string description, AcceptableValueBase? acceptableValues = null, Action<ConfigEntryBase>? customDrawer = null)
        {
            var configDescription = customDrawer != null
                ? new ConfigDescription(description, acceptableValues, new ConfigurationManagerAttributes { CustomDrawer = customDrawer })
                : new ConfigDescription(description, acceptableValues);
            return config.Bind(section, key, defaultValue, configDescription);
        }
    }
}
