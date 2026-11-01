using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using WispTorchHeeler.Configuration;
using WispTorchHeeler.Lighting;
using WispTorchHeeler.Localization;
using WispTorchHeeler.Painter;
using WispTorchHeeler.Pieces;

namespace WispTorchHeeler
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("_shudnal.ConditionalConfigSync", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "dreamwraith.WispTorchHeeler";
        public const string ModName = "WispTorchHeeler";
        public const string ModVersion = VersionInfo.Version;

        internal static Plugin? Instance { get; private set; }
        internal static ManualLogSource Log = null!;
        private Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // 1. Initialize configuration and ConditionalConfigSync
            ModConfig.Initialize(Config);

            if (!ModConfig.EnableMod.Value)
            {
                Log.LogInfo($"{ModName} is disabled in configuration.");
                return;
            }

            // 2. Register localizations via standalone module
            ModLocalization.Initialize();

            // 3. Initialize custom pieces and prefabs
            WispPieceManager.Initialize();

            // 4. Initialize lighting and demister controllers
            WispTorchColorController.Initialize();
            WispDemisterColorController.Initialize();

            // 5. Apply Harmony patches
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);

            Log.LogInfo($"{ModName} v{ModVersion} loaded successfully.");
        }

        private void Update()
        {
            if (!ModConfig.EnableMod.Value || Player.m_localPlayer == null)
            {
                return;
            }

            // Check Jotunn-registered buttons in ZInput (Jotunn handles ActiveInGUI suppression automatically)
            if (ModConfig.ButtonResetConfig != null && ZInput.GetButtonDown(ModConfig.ButtonResetConfig.Name))
            {
                TorchPainter.TryPaintOrReset(isReset: true);
            }
            else if (ModConfig.ButtonPaintConfig != null && ZInput.GetButtonDown(ModConfig.ButtonPaintConfig.Name))
            {
                TorchPainter.TryPaintOrReset(isReset: false);
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal static void LogDebug(string message)
        {
            if (ModConfig.EnableDebugLogs != null && ModConfig.EnableDebugLogs.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }
    }
}
