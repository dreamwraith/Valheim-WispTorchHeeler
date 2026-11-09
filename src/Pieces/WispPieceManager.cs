using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
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

        public const float NativePlayerBaseRadius = 20.0f;
        public const float NativeMistClearRadius = 24.0f;
        public const float NativeLightRange = 10.0f;

        public class PieceBaseStats
        {
            public float BaseHealth;
            public float[] ForceFieldStartRanges = Array.Empty<float>();
            public float[] ForceFieldEndRanges = Array.Empty<float>();
            public float[] LightIntensities = Array.Empty<float>();
            public float[] LightRanges = Array.Empty<float>();
        }

        // everything that differs between the four pieces, the config entries drive the live updates
        public record struct PieceTier(
            string PrefabName,
            string BasePrefabName,
            string NameToken,
            string DescriptionToken,
            float ScaleFactor,
            Vector3 ColliderCenter,
            Vector3 ColliderSize,
            string? PlaceEffectSource,
            string? WearEffectSource,
            ConfigEntry<string> Recipe,
            ConfigEntry<string> CraftingStation,
            ConfigEntry<float> Health,
            ConfigEntry<float> MistClearRangeMultiplier,
            ConfigEntry<float> LightIntensityMultiplier,
            ConfigEntry<float> LightRangeMultiplier);

        public static readonly Dictionary<string, PieceBaseStats> BaseStats = new();

        // the array keeps registration order, the dictionary is for lookups by prefab name
        private static PieceTier[] s_tiers = Array.Empty<PieceTier>();
        private static Dictionary<string, PieceTier> s_tiersByPrefab = new();

        public static IReadOnlyList<PieceTier> Tiers => s_tiers;

        private static GameObject? s_areaMarkerTemplate;
        public static GameObject? AreaMarkerTemplate
        {
            get
            {
                if (!s_areaMarkerTemplate)
                {
                    GameObject guardStone = PrefabManager.Instance.GetPrefab("guard_stone");
                    if (guardStone)
                    {
                        PrivateArea privateArea = guardStone.GetComponent<PrivateArea>();
                        if (privateArea && privateArea.m_areaMarker)
                        {
                            s_areaMarkerTemplate = privateArea.m_areaMarker.gameObject;
                        }
                    }
                    if (!s_areaMarkerTemplate)
                    {
                        GameObject workbench = PrefabManager.Instance.GetPrefab("piece_workbench");
                        if (workbench)
                        {
                            CraftingStation craftingStation = workbench.GetComponent<CraftingStation>();
                            if (craftingStation && craftingStation.m_areaMarker)
                            {
                                s_areaMarkerTemplate = craftingStation.m_areaMarker;
                            }
                        }
                    }
                }
                return s_areaMarkerTemplate;
            }
        }

        // runs after ModConfig.Initialize so the tier table can hold the bound entries
        public static void Initialize()
        {
            s_tiers = CreateTiers();
            s_tiersByPrefab = s_tiers.ToDictionary(tier => tier.PrefabName);

            PieceManager.OnPiecesRegistered += OnPiecesRegistered;

            foreach (PieceTier tier in s_tiers)
            {
                EventHandler tierChangedHandler = (sender, eventArgs) => UpdatePieceConfiguration(tier);
                tier.Recipe.SettingChanged += tierChangedHandler;
                tier.CraftingStation.SettingChanged += tierChangedHandler;
                tier.Health.SettingChanged += tierChangedHandler;
                tier.MistClearRangeMultiplier.SettingChanged += tierChangedHandler;
                tier.LightIntensityMultiplier.SettingChanged += tierChangedHandler;
                tier.LightRangeMultiplier.SettingChanged += tierChangedHandler;
            }

            ModConfig.PlayerBaseMistRadiusBonus.SettingChanged += (sender, eventArgs) => UpdateAllPieceConfigurations();

            Plugin.LogDebug("[PieceManager] Subscribed to piece lifecycle and config property events.");
        }

        private static PieceTier[] CreateTiers() => new[]
        {
            new PieceTier(
                PrefabName: Tier1TorchPrefab,
                BasePrefabName: NativeMistTorchPrefab,
                NameToken: Localization.ModLocalization.TokenPieceTorchLg,
                DescriptionToken: Localization.ModLocalization.TokenPieceTorchLgDesc,
                ScaleFactor: 1.25f,
                ColliderCenter: new Vector3(0f, 0.025f, 0f),
                ColliderSize: new Vector3(0.25f, 1.35f, 0.25f),
                PlaceEffectSource: null,
                WearEffectSource: null,
                Recipe: ModConfig.Torch_Recipe,
                CraftingStation: ModConfig.Torch_CraftingStation,
                Health: ModConfig.Torch_Health,
                MistClearRangeMultiplier: ModConfig.Torch_MistClearRangeMultiplier,
                LightIntensityMultiplier: ModConfig.Torch_LightIntensityMultiplier,
                LightRangeMultiplier: ModConfig.Torch_LightRangeMultiplier),
            new PieceTier(
                PrefabName: Tier2SmallLampPrefab,
                BasePrefabName: NativeDemisterPrefab,
                NameToken: Localization.ModLocalization.TokenPieceSmallLamp,
                DescriptionToken: Localization.ModLocalization.TokenPieceSmallLampDesc,
                ScaleFactor: 1.0f,
                ColliderCenter: new Vector3(0f, -0.10f, 0f),
                ColliderSize: new Vector3(0.26f, 1.10f, 0.26f),
                PlaceEffectSource: NativeWallTorchPrefab,
                WearEffectSource: NativeDvergrLanternPrefab,
                Recipe: ModConfig.SmallLamp_Recipe,
                CraftingStation: ModConfig.SmallLamp_CraftingStation,
                Health: ModConfig.SmallLamp_Health,
                MistClearRangeMultiplier: ModConfig.SmallLamp_MistClearRangeMultiplier,
                LightIntensityMultiplier: ModConfig.SmallLamp_LightIntensityMultiplier,
                LightRangeMultiplier: ModConfig.SmallLamp_LightRangeMultiplier),
            new PieceTier(
                PrefabName: Tier3LargeLampPrefab,
                BasePrefabName: NativeDemisterLargePrefab,
                NameToken: Localization.ModLocalization.TokenPieceLampLg,
                DescriptionToken: Localization.ModLocalization.TokenPieceLampLgDesc,
                ScaleFactor: 1.1f,
                ColliderCenter: new Vector3(0f, -0.18f, 0f),
                ColliderSize: new Vector3(0.32f, 1.45f, 0.32f),
                PlaceEffectSource: NativeWallTorchPrefab,
                WearEffectSource: NativeDvergrLanternPrefab,
                Recipe: ModConfig.LargeLamp_Recipe,
                CraftingStation: ModConfig.LargeLamp_CraftingStation,
                Health: ModConfig.LargeLamp_Health,
                MistClearRangeMultiplier: ModConfig.LargeLamp_MistClearRangeMultiplier,
                LightIntensityMultiplier: ModConfig.LargeLamp_LightIntensityMultiplier,
                LightRangeMultiplier: ModConfig.LargeLamp_LightRangeMultiplier),
            new PieceTier(
                PrefabName: Tier4GrandLampPrefab,
                BasePrefabName: NativeDemisterLargePrefab,
                NameToken: Localization.ModLocalization.TokenPieceLampGrand,
                DescriptionToken: Localization.ModLocalization.TokenPieceLampGrandDesc,
                ScaleFactor: 1.5f,
                ColliderCenter: new Vector3(0f, -0.18f, 0f),
                ColliderSize: new Vector3(0.32f, 1.45f, 0.32f),
                PlaceEffectSource: NativeWallTorchPrefab,
                WearEffectSource: NativeDvergrLanternPrefab,
                Recipe: ModConfig.GrandLamp_Recipe,
                CraftingStation: ModConfig.GrandLamp_CraftingStation,
                Health: ModConfig.GrandLamp_Health,
                MistClearRangeMultiplier: ModConfig.GrandLamp_MistClearRangeMultiplier,
                LightIntensityMultiplier: ModConfig.GrandLamp_LightIntensityMultiplier,
                LightRangeMultiplier: ModConfig.GrandLamp_LightRangeMultiplier)
        };

        private static void OnPiecesRegistered()
        {
            Plugin.LogDebug("[PieceManager] OnPiecesRegistered triggered. Updating piece recipes and stations.");
            UpdateAllPieceConfigurations();
        }

        public static void UpdateAllPieceConfigurations()
        {
            foreach (PieceTier tier in s_tiers)
            {
                UpdatePieceConfiguration(tier);
            }
        }

        public static void UpdatePieceConfiguration(PieceTier tier)
        {
            string prefabName = tier.PrefabName;
            if (SceneManager.GetActiveScene().name != "main")
            {
                Plugin.LogDebug($"[PieceManager] Active scene is '{SceneManager.GetActiveScene().name}'. Deferring piece update for '{prefabName}' until game world loads.");
                return;
            }

            CustomPiece customPiece = PieceManager.Instance.GetPiece(prefabName);
            if (customPiece == null || !customPiece.PiecePrefab)
            {
                Plugin.LogDebug($"[PieceManager] CustomPiece '{prefabName}' not found in PieceManager registry for config update.");
                return;
            }

            Piece piece = customPiece.PiecePrefab.GetComponent<Piece>();

            if (TryParseAndValidateRecipe(tier.Recipe.Value, prefabName, out RequirementConfig[] requirements))
            {
                List<Piece.Requirement> pieceRequirements = new List<Piece.Requirement>();
                foreach (RequirementConfig requirementConfig in requirements)
                {
                    GameObject itemPrefab = PrefabManager.Instance.GetPrefab(requirementConfig.Item);
                    ItemDrop? itemDrop = itemPrefab ? itemPrefab.GetComponent<ItemDrop>() : null;
                    if (itemDrop)
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

                if (piece)
                {
                    piece.m_resources = pieceRequirements.ToArray();
                }
            }
            else
            {
                Plugin.Log.LogError($"[PieceManager] Rejecting recipe update for piece '{prefabName}' due to validation errors. Keeping existing recipe.");
            }

            if (piece)
            {
                string stationName = CraftingStations.GetInternalName(tier.CraftingStation.Value);
                if (string.IsNullOrEmpty(stationName) || stationName.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    piece.m_craftingStation = null;
                }
                else
                {
                    GameObject stationPrefab = PrefabManager.Instance.GetPrefab(stationName);
                    if (stationPrefab)
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

            ApplyProperties(customPiece.PiecePrefab, prefabName);
            Lighting.WispTorchColorController.RefreshPropertiesForPrefab(prefabName);

            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.UpdateKnownRecipesList();
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }

            Plugin.LogDebug($"[PieceManager] Updated '{prefabName}' configuration in real time.");
        }

        public static void ApplyProperties(GameObject targetGameObject, string prefabName)
        {
            if (!BaseStats.TryGetValue(prefabName, out PieceBaseStats stats)) return;

            // BaseStats only ever holds registered tiers
            PieceTier tier = s_tiersByPrefab[prefabName];
            float mistClearRangeMultiplier = tier.MistClearRangeMultiplier.Value;
            float lightIntensityMultiplier = tier.LightIntensityMultiplier.Value;
            float lightRangeMultiplier = tier.LightRangeMultiplier.Value;

            ParticleSystemForceField[] forceFields = targetGameObject.GetComponentsInChildren<ParticleSystemForceField>(true);
            for (int forceFieldIndex = 0; forceFieldIndex < forceFields.Length && forceFieldIndex < stats.ForceFieldStartRanges.Length; forceFieldIndex++)
            {
                forceFields[forceFieldIndex].startRange = stats.ForceFieldStartRanges[forceFieldIndex] * mistClearRangeMultiplier;
                forceFields[forceFieldIndex].endRange = stats.ForceFieldEndRanges[forceFieldIndex] * mistClearRangeMultiplier;
            }

            Light[] lights = targetGameObject.GetComponentsInChildren<Light>(true);
            for (int lightIndex = 0; lightIndex < lights.Length && lightIndex < stats.LightIntensities.Length; lightIndex++)
            {
                WispVisualApplicator.SetLightIntensity(lights[lightIndex], stats.LightIntensities[lightIndex] * lightIntensityMultiplier);
                WispVisualApplicator.SetLightRange(lights[lightIndex], stats.LightRanges[lightIndex] * lightRangeMultiplier);
            }

            WearNTear wearNTear = targetGameObject.GetComponent<WearNTear>();
            if (wearNTear)
            {
                wearNTear.m_health = tier.Health.Value;
            }

            // the trigger radius is local, so the model scale gets divided back out
            Transform playerBaseTransform = targetGameObject.transform.Find("PlayerBase");
            if (playerBaseTransform)
            {
                SphereCollider sphereCollider = playerBaseTransform.GetComponent<SphereCollider>();
                if (sphereCollider)
                {
                    float activeMistRadius = NativeMistClearRadius * mistClearRangeMultiplier;
                    float bonus = Mathf.Clamp01(ModConfig.PlayerBaseMistRadiusBonus.Value);
                    float effectivePlayerBaseRadius = NativePlayerBaseRadius + (activeMistRadius - NativePlayerBaseRadius) * bonus;
                    Vector3 lossyScale = targetGameObject.transform.lossyScale;
                    float scale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
                    sphereCollider.radius = scale > 0.001f ? effectivePlayerBaseRadius / scale : effectivePlayerBaseRadius;
                }
            }
        }

        public static bool IsValidItemPrefab(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return false;

            if (itemName.Equals(RecipeConfigDrawer.DefaultPlaceholderPrefab, StringComparison.OrdinalIgnoreCase))
                return false;

            GameObject prefab = PrefabManager.Instance.GetPrefab(itemName);
            return prefab && prefab.TryGetComponent<ItemDrop>(out _);
        }

        // one bad token or missing item fails the whole recipe
        public static bool TryParseAndValidateRecipe(string recipeString, string pieceName, out RequirementConfig[] requirements)
        {
            requirements = Array.Empty<RequirementConfig>();

            if (string.IsNullOrWhiteSpace(recipeString))
            {
                Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' is empty or whitespace.");
                return false;
            }

            string[] tokens = recipeString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                Plugin.Log.LogError($"[PieceManager] Recipe for piece '{pieceName}' contains no valid tokens ('{recipeString}').");
                return false;
            }

            List<RequirementConfig> parsedRequirements = new List<RequirementConfig>();
            foreach (string token in tokens)
            {
                string[] parts = token.Trim().Split(':');
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
    }
}
