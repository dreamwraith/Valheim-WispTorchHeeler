using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
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

            ModConfig.Initialize(Config);
            new ConfigFileWatcher(Config);

            if (!ModConfig.EnableMod.Value)
            {
                Log.LogInfo($"{ModName} is disabled in configuration.");
                return;
            }

            WispPieceManager.Initialize();
            WispPieceRegistration.Initialize();

            // servers only need the pieces and synced settings
            if (GUIManager.IsHeadless())
            {
                return;
            }

            ModLocalization.Initialize();
            ModConfig.RegisterInputButtons();
            gameObject.AddComponent<TorchPainterInput>();

            WispTorchColorController.Initialize();
            WispDemisterColorController.Initialize();
            WispDemisterHipTemplates.Initialize();
            WispDemisterIdleMotion.Initialize();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);

            Log.LogInfo($"{ModName} v{ModVersion} loaded successfully.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal static void LogDebug(string message)
        {
            if (ModConfig.EnableDebugLogs.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }
    }
}
