using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using WispTorchHeeler.Configuration;
using WispTorchHeeler.Lighting;

namespace WispTorchHeeler.Pieces
{
    public static class WispPieceManager
    {
        public const string Tier1TorchPrefab = "piece_wisptorch_heeler_lg";
        public const string Tier2SmallLampPrefab = "piece_wisplamp_heeler";
        public const string Tier3LargeLampPrefab = "piece_wisplamp_heeler_lg";
        public const string Tier4GrandLampPrefab = "piece_wisplamp_heeler_grand";
        public const string NativeMistTorchPrefab = "piece_groundtorch_mist";
        public const string NativeWallTorchPrefab = "piece_walltorch";
        public const string NativeDvergrLanternPrefab = "piece_dvergr_lantern";
        public const string NativeDemisterPrefab = "dverger_demister";
        public const string NativeDemisterLargePrefab = "dverger_demister_large";

        private static bool s_piecesRegistered;

        public class PieceBaseStats
        {
            public float BaseHealth;
            public float[] ForceFieldStartRanges = Array.Empty<float>();
            public float[] ForceFieldEndRanges = Array.Empty<float>();
            public float[] LightIntensities = Array.Empty<float>();
            public float[] LightRanges = Array.Empty<float>();
        }

        public static readonly Dictionary<string, PieceBaseStats> BaseStats = new();

        private static GameObject? s_areaMarkerTemplate;
        public static GameObject? AreaMarkerTemplate
        {
            get
            {
                if (s_areaMarkerTemplate == null)
                {
                    var guardStone = PrefabManager.Instance != null ? PrefabManager.Instance.GetPrefab("guard_stone") : (ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("guard_stone") : null);
                    if (guardStone != null)
                    {
                        var privateArea = guardStone.GetComponent<PrivateArea>();
                        if (privateArea != null && privateArea.m_areaMarker != null)
                        {
                            s_areaMarkerTemplate = privateArea.m_areaMarker.gameObject;
                        }
                    }
                    if (s_areaMarkerTemplate == null)
                    {
                        var workbench = PrefabManager.Instance != null ? PrefabManager.Instance.GetPrefab("piece_workbench") : (ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("piece_workbench") : null);
                        if (workbench != null)
                        {
                            var craftingStation = workbench.GetComponent<CraftingStation>();
                            if (craftingStation != null && craftingStation.m_areaMarker != null)
                            {
                                s_areaMarkerTemplate = craftingStation.m_areaMarker;
                            }
                        }
                    }
                }
                return s_areaMarkerTemplate;
            }
        }

        public static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += RegisterCustomPieces;
            PieceManager.OnPiecesRegistered += OnPiecesRegistered;

            // Hook config updates to re-apply recipes, stations, and properties in real time
            ModConfig.Torch_Recipe.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier1TorchPrefab);
            ModConfig.Torch_CraftingStation.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier1TorchPrefab);
            ModConfig.Torch_Health.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier1TorchPrefab);
            ModConfig.Torch_MistClearRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier1TorchPrefab);
            ModConfig.Torch_LightIntensityMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier1TorchPrefab);
            ModConfig.Torch_LightRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier1TorchPrefab);

            ModConfig.SmallLamp_Recipe.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier2SmallLampPrefab);
            ModConfig.SmallLamp_CraftingStation.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier2SmallLampPrefab);
            ModConfig.SmallLamp_Health.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier2SmallLampPrefab);
            ModConfig.SmallLamp_MistClearRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier2SmallLampPrefab);
            ModConfig.SmallLamp_LightIntensityMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier2SmallLampPrefab);
            ModConfig.SmallLamp_LightRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier2SmallLampPrefab);

            ModConfig.LargeLamp_Recipe.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier3LargeLampPrefab);
            ModConfig.LargeLamp_CraftingStation.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier3LargeLampPrefab);
            ModConfig.LargeLamp_Health.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier3LargeLampPrefab);
            ModConfig.LargeLamp_MistClearRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier3LargeLampPrefab);
            ModConfig.LargeLamp_LightIntensityMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier3LargeLampPrefab);
            ModConfig.LargeLamp_LightRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier3LargeLampPrefab);

            ModConfig.GrandLamp_Recipe.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier4GrandLampPrefab);
            ModConfig.GrandLamp_CraftingStation.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier4GrandLampPrefab);
            ModConfig.GrandLamp_Health.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier4GrandLampPrefab);
            ModConfig.GrandLamp_MistClearRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier4GrandLampPrefab);
            ModConfig.GrandLamp_LightIntensityMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier4GrandLampPrefab);
            ModConfig.GrandLamp_LightRangeMultiplier.SettingChanged += (sender, eventArgs) => UpdatePieceConfiguration(Tier4GrandLampPrefab);

            Plugin.LogDebug("[PieceManager] Subscribed to native prefab, piece lifecycle, and config property events.");
        }

        private static void RegisterCustomPieces()
        {
            if (s_piecesRegistered) return;
            s_piecesRegistered = true;

            Plugin.LogDebug("[PieceManager] RegisterCustomPieces triggered.");

            // 1. Inject WispTorchColorController into native mist torch
            var nativeTorch = PrefabManager.Instance.GetPrefab(NativeMistTorchPrefab);
            if (nativeTorch != null)
            {
                if (nativeTorch.GetComponent<WispTorchColorController>() == null)
                {
                    nativeTorch.AddComponent<WispTorchColorController>();
                    Plugin.LogDebug($"[PieceManager] Injected WispTorchColorController into native prefab '{NativeMistTorchPrefab}'.");
                }

                EnsurePieceCollider(nativeTorch, new Vector3(0f, 0.025f, 0f), new Vector3(0.25f, 1.35f, 0.25f));
            }
            else
            {
                Plugin.Log.LogWarning($"[PieceManager] Native prefab '{NativeMistTorchPrefab}' not found for controller injection.");
            }

            // 2. Register Tier 1: Large Wisp Torch (scaled 1.25x by Unity, retains normal torch sounds)
            RegisterPiece(
                customPrefabName: Tier1TorchPrefab,
                basePrefabName: NativeMistTorchPrefab,
                nameToken: Localization.ModLocalization.TokenPieceTorchLg,
                descriptionToken: Localization.ModLocalization.TokenPieceTorchLgDesc,
                scaleFactor: 1.25f,
                recipeString: ModConfig.Torch_Recipe.Value,
                defaultRecipeString: (string)ModConfig.Torch_Recipe.DefaultValue,
                stationString: ModConfig.Torch_CraftingStation.Value,
                colliderCenter: new Vector3(0f, 0.025f, 0f),
                colliderSize: new Vector3(0.25f, 1.35f, 0.25f)
            );

            // 3. Register Tier 2: Small Wisp Lamp (scale 1.0x, walltorch build sound, dvergr lantern destroy/hit sounds)
            RegisterPiece(
                customPrefabName: Tier2SmallLampPrefab,
                basePrefabName: NativeDemisterPrefab,
                nameToken: Localization.ModLocalization.TokenPieceSmallLamp,
                descriptionToken: Localization.ModLocalization.TokenPieceSmallLampDesc,
                scaleFactor: 1.0f,
                recipeString: ModConfig.SmallLamp_Recipe.Value,
                defaultRecipeString: (string)ModConfig.SmallLamp_Recipe.DefaultValue,
                stationString: ModConfig.SmallLamp_CraftingStation.Value,
                colliderCenter: new Vector3(0f, -0.10f, 0f),
                colliderSize: new Vector3(0.26f, 1.10f, 0.26f),
                placeEffectSource: NativeWallTorchPrefab,
                wearEffectSource: NativeDvergrLanternPrefab
            );

            // 4. Register Tier 3: Large Dvergr Lamp (scaled 1.1x by Unity, walltorch build sound, dvergr lantern destroy/hit sounds)
            RegisterPiece(
                customPrefabName: Tier3LargeLampPrefab,
                basePrefabName: NativeDemisterLargePrefab,
                nameToken: Localization.ModLocalization.TokenPieceLampLg,
                descriptionToken: Localization.ModLocalization.TokenPieceLampLgDesc,
                scaleFactor: 1.1f,
                recipeString: ModConfig.LargeLamp_Recipe.Value,
                defaultRecipeString: (string)ModConfig.LargeLamp_Recipe.DefaultValue,
                stationString: ModConfig.LargeLamp_CraftingStation.Value,
                colliderCenter: new Vector3(0f, -0.18f, 0f),
                colliderSize: new Vector3(0.32f, 1.45f, 0.32f),
                placeEffectSource: NativeWallTorchPrefab,
                wearEffectSource: NativeDvergrLanternPrefab
            );

            // 5. Register Tier 4: Grand Dvergr Lamp (scaled 1.5x by Unity, walltorch build sound, dvergr lantern destroy/hit sounds)
            RegisterPiece(
                customPrefabName: Tier4GrandLampPrefab,
                basePrefabName: NativeDemisterLargePrefab,
                nameToken: Localization.ModLocalization.TokenPieceLampGrand,
                descriptionToken: Localization.ModLocalization.TokenPieceLampGrandDesc,
                scaleFactor: 1.5f,
                recipeString: ModConfig.GrandLamp_Recipe.Value,
                defaultRecipeString: (string)ModConfig.GrandLamp_Recipe.DefaultValue,
                stationString: ModConfig.GrandLamp_CraftingStation.Value,
                colliderCenter: new Vector3(0f, -0.18f, 0f),
                colliderSize: new Vector3(0.32f, 1.45f, 0.32f),
                placeEffectSource: NativeWallTorchPrefab,
                wearEffectSource: NativeDvergrLanternPrefab
            );

            // Unsubscribe from PrefabManager event to prevent redundant registrations
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterCustomPieces;
        }

        private static void RegisterPiece(
            string customPrefabName,
            string basePrefabName,
            string nameToken,
            string descriptionToken,
            float scaleFactor,
            string recipeString,
            string defaultRecipeString,
            string stationString,
            in Vector3 colliderCenter,
            in Vector3 colliderSize,
            string? placeEffectSource = null,
            string? wearEffectSource = null)
        {
            // 1. Strict Validation: Validate recipe before instantiating or cloning any prefab
            if (!TryParseAndValidateRecipe(recipeString, customPrefabName, out var requirements))
            {
                Plugin.Log.LogError($"[PieceManager] Config recipe for piece '{customPrefabName}' is invalid ('{recipeString}'). Falling back to default recipe: '{defaultRecipeString}'.");
                if (!TryParseAndValidateRecipe(defaultRecipeString, customPrefabName, out requirements))
                {
                    Plugin.Log.LogError($"[PieceManager] Default recipe for piece '{customPrefabName}' is also invalid ('{defaultRecipeString}'). Aborting piece registration.");
                    return;
                }
            }

            GameObject clonedGameObject = PrefabManager.Instance.CreateClonedPrefab(customPrefabName, basePrefabName);
            if (clonedGameObject == null)
            {
                Plugin.Log.LogError($"[PieceManager] Failed to clone prefab '{basePrefabName}' as '{customPrefabName}'.");
                return;
            }

            // Snapshot unmultiplied base stats from cloned prefab before applying any multipliers
            var rawForceFields = clonedGameObject.GetComponentsInChildren<ParticleSystemForceField>(true);
            var rawLights = clonedGameObject.GetComponentsInChildren<Light>(true);
            var rawWearNTear = clonedGameObject.GetComponent<WearNTear>();

            var baseStats = new PieceBaseStats
            {
                BaseHealth = rawWearNTear != null ? rawWearNTear.m_health : 100f,
                ForceFieldStartRanges = new float[rawForceFields.Length],
                ForceFieldEndRanges = new float[rawForceFields.Length],
                LightIntensities = new float[rawLights.Length],
                LightRanges = new float[rawLights.Length]
            };

            for (int forceFieldIndex = 0; forceFieldIndex < rawForceFields.Length; forceFieldIndex++)
            {
                baseStats.ForceFieldStartRanges[forceFieldIndex] = rawForceFields[forceFieldIndex].startRange;
                baseStats.ForceFieldEndRanges[forceFieldIndex] = rawForceFields[forceFieldIndex].endRange;
            }

            for (int lightIndex = 0; lightIndex < rawLights.Length; lightIndex++)
            {
                baseStats.LightIntensities[lightIndex] = rawLights[lightIndex].intensity;
                baseStats.LightRanges[lightIndex] = rawLights[lightIndex].range;
            }

            BaseStats[customPrefabName] = baseStats;

            // Apply static physical scaling
            if (Mathf.Abs(scaleFactor - 1.0f) > 0.001f)
            {
                clonedGameObject.transform.localScale = clonedGameObject.transform.localScale * scaleFactor;
                Plugin.LogDebug($"[PieceManager] Scaled '{customPrefabName}' by {scaleFactor}x.");
            }

            // Ensure ZNetView exists
            var netView = clonedGameObject.GetComponent<ZNetView>();
            if (netView == null)
            {
                netView = clonedGameObject.AddComponent<ZNetView>();
                netView.m_syncInitialScale = true;
            }

            // Ensure Piece component exists and is configured for building hammer
            var piece = clonedGameObject.GetComponent<Piece>();
            if (piece == null)
            {
                piece = clonedGameObject.AddComponent<Piece>();
            }
            piece.m_name = nameToken;
            piece.m_description = descriptionToken;
            piece.m_canBeRemoved = true;
            piece.m_category = Piece.PieceCategory.Misc;

            // Configure HoverText component so Valheim native hover displays the piece name token
            var hoverText = clonedGameObject.GetComponent<HoverText>();
            if (hoverText == null)
            {
                hoverText = clonedGameObject.AddComponent<HoverText>();
            }
            hoverText.m_text = nameToken;

            // Configure placement sound and VFX if an override source is specified
            if (!string.IsNullOrEmpty(placeEffectSource))
            {
                var placeSourcePiece = PrefabManager.Instance.GetPrefab(placeEffectSource)?.GetComponent<Piece>();
                if (placeSourcePiece != null && placeSourcePiece.m_placeEffect != null)
                {
                    piece.m_placeEffect = placeSourcePiece.m_placeEffect;
                }
            }

            // Initial fallback icon
            if (piece.m_icon == null)
            {
                var nativeTorch = PrefabManager.Instance.GetPrefab(NativeMistTorchPrefab);
                var nativePiece = nativeTorch != null ? nativeTorch.GetComponent<Piece>() : null;
                if (nativePiece != null && nativePiece.m_icon != null)
                {
                    piece.m_icon = nativePiece.m_icon;
                }
            }

            // Generate high-fidelity 3D model snapshot icon via Jotunn RenderManager
            try
            {
                var customIcon = RenderManager.Instance.Render(clonedGameObject);
                if (customIcon != null)
                {
                    piece.m_icon = customIcon;
                    Plugin.LogDebug($"[PieceManager] Generated custom 3D snapshot icon for '{customPrefabName}'.");
                }
            }
            catch (Exception exception)
            {
                Plugin.Log.LogWarning($"[PieceManager] Failed to render 3D icon for '{customPrefabName}': {exception.Message}");
            }

            // Ensure WearNTear component exists with weather decay immunity
            var wearNTear = clonedGameObject.GetComponent<WearNTear>();
            if (wearNTear == null)
            {
                wearNTear = clonedGameObject.AddComponent<WearNTear>();
            }
            wearNTear.m_noRoofWear = true;
            wearNTear.m_noSupportWear = true;

            // Configure hit and destruction sounds if an override source is specified
            if (!string.IsNullOrEmpty(wearEffectSource))
            {
                var wearSource = PrefabManager.Instance.GetPrefab(wearEffectSource)?.GetComponent<WearNTear>();
                if (wearSource != null)
                {
                    wearNTear.m_hitEffect = wearSource.m_hitEffect;
                    wearNTear.m_destroyedEffect = wearSource.m_destroyedEffect;
                }
            }

            // Ensure physical colliders are on the 'piece' layer so the building hammer can target, repair, and deconstruct it.
            // Never promote trigger colliders (such as the native torch's PlayerBase EffectArea SphereCollider),
            // as placing triggers on physical layers causes Plant.HaveGrowSpace() to detect them as physical obstructions.
            int pieceLayer = LayerMask.NameToLayer("piece");
            if (pieceLayer >= 0)
            {
                clonedGameObject.layer = pieceLayer;
                foreach (var collider in clonedGameObject.GetComponentsInChildren<Collider>(true))
                {
                    if (!collider.isTrigger)
                    {
                        collider.gameObject.layer = pieceLayer;
                    }
                }
            }

            // Ensure a right-sized BoxCollider exists on the piece root
            EnsurePieceCollider(clonedGameObject, in colliderCenter, in colliderSize);

            // Apply initial physical properties (force field mist clearance, light intensity/range, health)
            ApplyProperties(clonedGameObject, customPrefabName);

            // Attach dynamic color controller
            if (clonedGameObject.GetComponent<WispTorchColorController>() == null)
            {
                clonedGameObject.AddComponent<WispTorchColorController>();
            }

            // Register CustomPiece via Jotunn with Valheim 1.0 build menu categories & usage tags
            var pieceConfig = new PieceConfig
            {
                Name = nameToken,
                Description = descriptionToken,
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                Usage = new[] { PieceUsages.Lighting, PieceUsages.Misc },
                CraftingStation = stationString,
                Requirements = requirements
            };

            var customPiece = new CustomPiece(clonedGameObject, fixReference: true, pieceConfig);
            PieceManager.Instance.AddPiece(customPiece);

            Plugin.LogDebug($"[PieceManager] Successfully registered CustomPiece '{customPrefabName}' in Hammer (Category=Misc, Usages=Lighting,Misc).");
        }

        private static void OnPiecesRegistered()
        {
            Plugin.LogDebug("[PieceManager] OnPiecesRegistered triggered. Updating piece recipes and stations.");
            UpdateAllPieceConfigurations();
        }

        public static void UpdateAllPieceConfigurations()
        {
            UpdatePieceConfiguration(Tier1TorchPrefab);
            UpdatePieceConfiguration(Tier2SmallLampPrefab);
            UpdatePieceConfiguration(Tier3LargeLampPrefab);
            UpdatePieceConfiguration(Tier4GrandLampPrefab);
        }

        public static void UpdatePieceConfiguration(string prefabName)
        {
            // Do not execute runtime piece updates in the main menu; defer until the game world is loaded
            if (SceneManager.GetActiveScene().name != "main")
            {
                Plugin.LogDebug($"[PieceManager] Active scene is '{SceneManager.GetActiveScene().name}'. Deferring piece update for '{prefabName}' until game world loads.");
                return;
            }

            var customPiece = PieceManager.Instance.GetPiece(prefabName);
            if (customPiece == null || customPiece.PiecePrefab == null)
            {
                Plugin.LogDebug($"[PieceManager] CustomPiece '{prefabName}' not found in PieceManager registry for config update.");
                return;
            }

            if (!TryGetPieceConfig(prefabName, out string recipeString, out string stationString, out _, out _, out _, out _))
            {
                return;
            }

            var piece = customPiece.PiecePrefab.GetComponent<Piece>();

            // 1. Recipe
            if (TryParseAndValidateRecipe(recipeString, prefabName, out var requirements))
            {
                var pieceRequirements = new List<Piece.Requirement>();
                foreach (var requirementConfig in requirements)
                {
                    var itemPrefab = PrefabManager.Instance.GetPrefab(requirementConfig.Item);
                    var itemDrop = itemPrefab != null ? itemPrefab.GetComponent<ItemDrop>() : null;
                    if (itemDrop != null)
                    {
                        pieceRequirements.Add(new Piece.Requirement
                        {
                            m_resItem = itemDrop,
                            m_amount = requirementConfig.Amount,
                            m_amountPerLevel = requirementConfig.AmountPerLevel,
                            m_recover = requirementConfig.Recover
                        });
                    }
                    else
                    {
                        Plugin.Log.LogError($"[PieceManager] Could not resolve ItemDrop for requirement '{requirementConfig.Item}' on piece '{prefabName}'.");
                    }
                }

                if (piece != null)
                {
                    piece.m_resources = pieceRequirements.ToArray();
                }
            }
            else
            {
                Plugin.Log.LogError($"[PieceManager] Rejecting recipe update for piece '{prefabName}' due to validation errors. Keeping existing recipe.");
            }

            // 2. Direct CraftingStation assignment
            if (piece != null)
            {
                string stationName = CraftingStations.GetInternalName(stationString);
                if (string.IsNullOrEmpty(stationName) || stationName.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    piece.m_craftingStation = null;
                }
                else
                {
                    var stationPrefab = PrefabManager.Instance.GetPrefab(stationName);
                    if (stationPrefab != null)
                    {
                        piece.m_craftingStation = stationPrefab.GetComponent<CraftingStation>();
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[PieceManager] Crafting station prefab '{stationName}' not found for piece '{prefabName}'.");
                        piece.m_craftingStation = null;
                    }
                }
            }

            // 3. Update Prefab physical properties (force fields, lights, health)
            ApplyProperties(customPiece.PiecePrefab, prefabName);

            // 4. Update all live instances in the world
            Lighting.WispTorchColorController.RefreshPropertiesForPrefab(prefabName);

            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.UpdateKnownRecipesList();
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }

            Plugin.LogDebug($"[PieceManager] Updated '{prefabName}' configuration in real time.");
        }

        public static bool TryGetPieceConfig(
            string prefabName,
            out string recipeString,
            out string stationString,
            out float health,
            out float mistClearRangeMultiplier,
            out float lightIntensityMultiplier,
            out float lightRangeMultiplier)
        {
            if (prefabName == Tier1TorchPrefab)
            {
                recipeString = ModConfig.Torch_Recipe.Value;
                stationString = ModConfig.Torch_CraftingStation.Value;
                health = ModConfig.Torch_Health.Value;
                mistClearRangeMultiplier = ModConfig.Torch_MistClearRangeMultiplier.Value;
                lightIntensityMultiplier = ModConfig.Torch_LightIntensityMultiplier.Value;
                lightRangeMultiplier = ModConfig.Torch_LightRangeMultiplier.Value;
                return true;
            }
            if (prefabName == Tier2SmallLampPrefab)
            {
                recipeString = ModConfig.SmallLamp_Recipe.Value;
                stationString = ModConfig.SmallLamp_CraftingStation.Value;
                health = ModConfig.SmallLamp_Health.Value;
                mistClearRangeMultiplier = ModConfig.SmallLamp_MistClearRangeMultiplier.Value;
                lightIntensityMultiplier = ModConfig.SmallLamp_LightIntensityMultiplier.Value;
                lightRangeMultiplier = ModConfig.SmallLamp_LightRangeMultiplier.Value;
                return true;
            }
            if (prefabName == Tier3LargeLampPrefab)
            {
                recipeString = ModConfig.LargeLamp_Recipe.Value;
                stationString = ModConfig.LargeLamp_CraftingStation.Value;
                health = ModConfig.LargeLamp_Health.Value;
                mistClearRangeMultiplier = ModConfig.LargeLamp_MistClearRangeMultiplier.Value;
                lightIntensityMultiplier = ModConfig.LargeLamp_LightIntensityMultiplier.Value;
                lightRangeMultiplier = ModConfig.LargeLamp_LightRangeMultiplier.Value;
                return true;
            }
            if (prefabName == Tier4GrandLampPrefab)
            {
                recipeString = ModConfig.GrandLamp_Recipe.Value;
                stationString = ModConfig.GrandLamp_CraftingStation.Value;
                health = ModConfig.GrandLamp_Health.Value;
                mistClearRangeMultiplier = ModConfig.GrandLamp_MistClearRangeMultiplier.Value;
                lightIntensityMultiplier = ModConfig.GrandLamp_LightIntensityMultiplier.Value;
                lightRangeMultiplier = ModConfig.GrandLamp_LightRangeMultiplier.Value;
                return true;
            }

            recipeString = string.Empty;
            stationString = string.Empty;
            health = 100f;
            mistClearRangeMultiplier = 1f;
            lightIntensityMultiplier = 1f;
            lightRangeMultiplier = 1f;
            return false;
        }

        public static void ApplyProperties(GameObject targetGameObject, string prefabName)
        {
            if (targetGameObject == null) return;
            if (!BaseStats.TryGetValue(prefabName, out var stats)) return;
            if (!TryGetPieceConfig(prefabName, out _, out _, out float health, out float mistClearRangeMultiplier, out float lightIntensityMultiplier, out float lightRangeMultiplier)) return;

            var forceFields = targetGameObject.GetComponentsInChildren<ParticleSystemForceField>(true);
            for (int forceFieldIndex = 0; forceFieldIndex < forceFields.Length && forceFieldIndex < stats.ForceFieldStartRanges.Length; forceFieldIndex++)
            {
                forceFields[forceFieldIndex].startRange = stats.ForceFieldStartRanges[forceFieldIndex] * mistClearRangeMultiplier;
                forceFields[forceFieldIndex].endRange = stats.ForceFieldEndRanges[forceFieldIndex] * mistClearRangeMultiplier;
            }

            var lights = targetGameObject.GetComponentsInChildren<Light>(true);
            for (int lightIndex = 0; lightIndex < lights.Length && lightIndex < stats.LightIntensities.Length; lightIndex++)
            {
                lights[lightIndex].intensity = stats.LightIntensities[lightIndex] * lightIntensityMultiplier;
                lights[lightIndex].range = stats.LightRanges[lightIndex] * lightRangeMultiplier;
            }

            var wearNTear = targetGameObject.GetComponent<WearNTear>();
            if (wearNTear != null)
            {
                wearNTear.m_health = health;
            }
        }

        /// <summary>
        /// Validates that an item prefab exists in the game and has an ItemDrop component attached.
        /// Rejects empty names, placeholders like &lt;PrefabName&gt;, typos, or non-item prefabs.
        /// No data scrubbing or alias guessing is performed.
        /// </summary>
        public static bool IsValidItemPrefab(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return false;

            if (itemName.Equals(RecipeConfigDrawer.DefaultPlaceholderPrefab, StringComparison.OrdinalIgnoreCase))
                return false;

            // PrefabManager queries Jotunn custom prefabs, ZNetScene, ObjectDB, and native cache
            var prefab = PrefabManager.Instance.GetPrefab(itemName);
            return prefab != null && prefab.GetComponent<ItemDrop>() != null;
        }

        /// <summary>
        /// Strictly parses and validates a recipe string.
        /// If any token is malformed, amount &lt;= 0, or any item prefab does not exist in the game,
        /// logs an explicit error and returns false.
        /// </summary>
        public static bool TryParseAndValidateRecipe(string recipeString, string pieceName, out RequirementConfig[] requirements)
        {
            requirements = Array.Empty<RequirementConfig>();

            if (string.IsNullOrWhiteSpace(recipeString))
            {
                Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' is empty or whitespace.");
                return false;
            }

            var tokens = recipeString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' contains no valid tokens ('{recipeString}').");
                return false;
            }

            var parsedRequirements = new List<RequirementConfig>();
            foreach (var token in tokens)
            {
                var parts = token.Trim().Split(':');
                if (parts.Length != 2)
                {
                    Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' contains malformed token '{token}'. Expected 'ItemPrefab:Amount'.");
                    return false;
                }

                string itemName = parts[0].Trim();
                if (string.IsNullOrEmpty(itemName))
                {
                    Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' contains an empty item prefab name in token '{token}'.");
                    return false;
                }

                if (!int.TryParse(parts[1].Trim(), out int amount) || amount <= 0)
                {
                    Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' contains invalid amount '{parts[1]}' in token '{token}'. Amount must be a positive integer.");
                    return false;
                }

                if (!IsValidItemPrefab(itemName))
                {
                    Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' requires item '{itemName}' which does not exist in the game!");
                    return false;
                }

                parsedRequirements.Add(new RequirementConfig(itemName, amount, 0, recover: true));
            }

            requirements = parsedRequirements.ToArray();
            return true;
        }

        /// <summary>
        /// Ensures a piece root possesses a BoxCollider on the 'piece' layer right-sized to
        /// the specified center and dimensions, avoiding unnecessary bounding overlaps.
        /// </summary>
        private static void EnsurePieceCollider(GameObject targetGameObject, in Vector3 center, in Vector3 size)
        {
            if (targetGameObject == null) return;

            var boxCollider = targetGameObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = targetGameObject.AddComponent<BoxCollider>();
            }

            int pieceLayer = LayerMask.NameToLayer("piece");
            if (pieceLayer >= 0)
            {
                boxCollider.gameObject.layer = pieceLayer;
            }
            boxCollider.isTrigger = false;
            boxCollider.center = center;
            boxCollider.size = size;
        }
    }
}
