using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using WispTorchHeeler.Lighting;

namespace WispTorchHeeler.Pieces
{
    public static class WispPieceRegistration
    {
        public static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += RegisterCustomPieces;
        }

        private static void RegisterCustomPieces()
        {
            // jotunn raises this on every menu start, unsubscribing first keeps it to one run even if registration throws
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterCustomPieces;

            Plugin.LogDebug("[PieceManager] RegisterCustomPieces triggered.");

            GameObject nativeTorch = PrefabManager.Instance.GetPrefab(WispPieceManager.NativeMistTorchPrefab);
            if (nativeTorch)
            {
                if (!nativeTorch.TryGetComponent<WispTorchColorController>(out _))
                {
                    nativeTorch.AddComponent<WispTorchColorController>();
                    Plugin.LogDebug($"[PieceManager] Injected WispTorchColorController into native prefab '{WispPieceManager.NativeMistTorchPrefab}'.");
                }

                EnsurePieceCollider(nativeTorch, new Vector3(0f, 0.025f, 0f), new Vector3(0.25f, 1.35f, 0.25f));
            }
            else
            {
                Plugin.Log.LogWarning($"[PieceManager] Native prefab '{WispPieceManager.NativeMistTorchPrefab}' not found for controller injection.");
            }

            foreach (WispPieceManager.PieceTier tier in WispPieceManager.Tiers)
            {
                RegisterPiece(tier);
            }
        }

        private static void RegisterPiece(WispPieceManager.PieceTier tier)
        {
            string customPrefabName = tier.PrefabName;
            string recipeString = tier.Recipe.Value;
            string defaultRecipeString = (string)tier.Recipe.DefaultValue;

            // a bad recipe bails before anything gets cloned
            if (!WispPieceManager.TryParseAndValidateRecipe(recipeString, customPrefabName, out RequirementConfig[] requirements))
            {
                Plugin.Log.LogError($"[PieceManager] Config recipe for piece '{customPrefabName}' is invalid ('{recipeString}'). Falling back to default recipe: '{defaultRecipeString}'.");
                if (!WispPieceManager.TryParseAndValidateRecipe(defaultRecipeString, customPrefabName, out requirements))
                {
                    Plugin.Log.LogError($"[PieceManager] Default recipe for piece '{customPrefabName}' is also invalid ('{defaultRecipeString}'). Aborting piece registration.");
                    return;
                }
            }

            GameObject clonedGameObject = PrefabManager.Instance.CreateClonedPrefab(customPrefabName, tier.BasePrefabName);
            if (!clonedGameObject)
            {
                Plugin.Log.LogError($"[PieceManager] Failed to clone prefab '{tier.BasePrefabName}' as '{customPrefabName}'.");
                return;
            }

            // base stats get read from the clone before any multiplier touches it
            ParticleSystemForceField[] rawForceFields = clonedGameObject.GetComponentsInChildren<ParticleSystemForceField>(true);
            Light[] rawLights = clonedGameObject.GetComponentsInChildren<Light>(true);
            WearNTear rawWearNTear = clonedGameObject.GetComponent<WearNTear>();

            WispPieceManager.PieceBaseStats baseStats = new WispPieceManager.PieceBaseStats
            {
                BaseHealth = rawWearNTear ? rawWearNTear.m_health : 100f,
                ForceFieldStartRanges = new float[rawForceFields.Length],
                ForceFieldEndRanges = new float[rawForceFields.Length],
                LightIntensities = new float[rawLights.Length],
                LightRanges = new float[rawLights.Length]
            };

            for (int forceFieldIndex = 0; forceFieldIndex < rawForceFields.Length; forceFieldIndex++)
            {
                baseStats.ForceFieldStartRanges[forceFieldIndex] = rawForceFields[forceFieldIndex].startRange;
                baseStats.ForceFieldEndRanges[forceFieldIndex] = WispPieceManager.NativeMistClearRadius;
            }

            for (int lightIndex = 0; lightIndex < rawLights.Length; lightIndex++)
            {
                // raw donor intensity keeps the base game baseline and any ratio between several lights
                baseStats.LightIntensities[lightIndex] = rawLights[lightIndex].intensity;
                baseStats.LightRanges[lightIndex] = WispPieceManager.NativeLightRange;
            }

            WispPieceManager.BaseStats[customPrefabName] = baseStats;

            if (Mathf.Abs(tier.ScaleFactor - 1.0f) > 0.001f)
            {
                clonedGameObject.transform.localScale = clonedGameObject.transform.localScale * tier.ScaleFactor;
                Plugin.LogDebug($"[PieceManager] Scaled '{customPrefabName}' by {tier.ScaleFactor}x.");
            }

            ZNetView netView = clonedGameObject.GetComponent<ZNetView>();
            if (!netView)
            {
                netView = clonedGameObject.AddComponent<ZNetView>();
                netView.m_syncInitialScale = true;
            }

            Piece piece = clonedGameObject.GetComponent<Piece>();
            if (!piece)
            {
                piece = clonedGameObject.AddComponent<Piece>();
            }
            piece.m_name = tier.NameToken;
            piece.m_description = tier.DescriptionToken;
            piece.m_canBeRemoved = true;
            piece.m_category = Piece.PieceCategory.Misc;

            HoverText hoverText = clonedGameObject.GetComponent<HoverText>();
            if (!hoverText)
            {
                hoverText = clonedGameObject.AddComponent<HoverText>();
            }
            hoverText.m_text = tier.NameToken;

            if (!string.IsNullOrEmpty(tier.PlaceEffectSource))
            {
                GameObject placeSourcePrefab = PrefabManager.Instance.GetPrefab(tier.PlaceEffectSource);
                if (placeSourcePrefab && placeSourcePrefab.TryGetComponent(out Piece placeSourcePiece) && placeSourcePiece.m_placeEffect != null)
                {
                    piece.m_placeEffect = placeSourcePiece.m_placeEffect;
                }
            }

            // donor icon until the render below replaces it
            if (!piece.m_icon)
            {
                GameObject nativeTorch = PrefabManager.Instance.GetPrefab(WispPieceManager.NativeMistTorchPrefab);
                Piece? nativePiece = nativeTorch ? nativeTorch.GetComponent<Piece>() : null;
                if (nativePiece && nativePiece.m_icon)
                {
                    piece.m_icon = nativePiece.m_icon;
                }
            }

            try
            {
                Sprite customIcon = RenderManager.Instance.Render(clonedGameObject);
                if (customIcon)
                {
                    piece.m_icon = customIcon;
                    Plugin.LogDebug($"[PieceManager] Generated custom 3D snapshot icon for '{customPrefabName}'.");
                }
            }
            catch (Exception exception)
            {
                Plugin.Log.LogWarning($"[PieceManager] Failed to render 3D icon for '{customPrefabName}': {exception.Message}");
            }

            WearNTear wearNTear = clonedGameObject.GetComponent<WearNTear>();
            if (!wearNTear)
            {
                wearNTear = clonedGameObject.AddComponent<WearNTear>();
            }
            // why the actual fuck iron gate used this name idk, but m_noRoofWear = false means it wont decay from weather
            // and m_noSupportWear = true means it wont decay from lack of support
            wearNTear.m_noRoofWear = false;
            wearNTear.m_noSupportWear = true;

            if (!string.IsNullOrEmpty(tier.WearEffectSource))
            {
                GameObject wearSourcePrefab = PrefabManager.Instance.GetPrefab(tier.WearEffectSource);
                if (wearSourcePrefab && wearSourcePrefab.TryGetComponent(out WearNTear wearSource))
                {
                    wearNTear.m_hitEffect = wearSource.m_hitEffect;
                    wearNTear.m_destroyedEffect = wearSource.m_destroyedEffect;
                }
            }

            // solid colliders go on the piece layer so the hammer can target, repair and remove the piece. triggers stay put,
            // Plant.HaveGrowSpace overlaps the piece layer and would count a trigger like the PlayerBase sphere as an obstacle
            int pieceLayer = LayerMask.NameToLayer("piece");
            if (pieceLayer >= 0)
            {
                clonedGameObject.layer = pieceLayer;
                foreach (Collider collider in clonedGameObject.GetComponentsInChildren<Collider>(true))
                {
                    if (!collider.isTrigger)
                    {
                        collider.gameObject.layer = pieceLayer;
                    }
                }
            }

            EnsurePieceCollider(clonedGameObject, tier.ColliderCenter, tier.ColliderSize);
            EnsurePlayerBase(clonedGameObject);
            WispPieceManager.ApplyProperties(clonedGameObject, customPrefabName);

            if (!clonedGameObject.TryGetComponent<WispTorchColorController>(out _))
            {
                clonedGameObject.AddComponent<WispTorchColorController>();
            }

            PieceConfig pieceConfig = new PieceConfig
            {
                Name = tier.NameToken,
                Description = tier.DescriptionToken,
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                Usage = new[] { PieceUsages.Lighting, PieceUsages.Misc },
                CraftingStation = tier.CraftingStation.Value,
                Requirements = requirements
            };

            CustomPiece customPiece = new CustomPiece(clonedGameObject, fixReference: true, pieceConfig);
            PieceManager.Instance.AddPiece(customPiece);

            Plugin.LogDebug($"[PieceManager] Successfully registered CustomPiece '{customPrefabName}' in Hammer (Category=Misc, Usages=Lighting,Misc).");
        }

        private static void EnsurePieceCollider(GameObject targetGameObject, in Vector3 center, in Vector3 size)
        {
            BoxCollider boxCollider = targetGameObject.GetComponent<BoxCollider>();
            if (!boxCollider)
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

        // the PlayerBase EffectArea is what stops mob spawns around the piece
        private static void EnsurePlayerBase(GameObject targetGameObject)
        {
            Transform playerBaseTransform = targetGameObject.transform.Find("PlayerBase");
            GameObject playerBaseGo;
            if (playerBaseTransform)
            {
                playerBaseGo = playerBaseTransform.gameObject;
            }
            else
            {
                playerBaseGo = new GameObject("PlayerBase");
                playerBaseGo.transform.SetParent(targetGameObject.transform, false);
                playerBaseGo.transform.localPosition = Vector3.zero;
                playerBaseGo.transform.localRotation = Quaternion.identity;
            }

            int triggerLayer = LayerMask.NameToLayer("character_trigger");
            if (triggerLayer >= 0)
            {
                playerBaseGo.layer = triggerLayer;
            }

            SphereCollider sphereCollider = playerBaseGo.GetComponent<SphereCollider>();
            if (!sphereCollider)
            {
                sphereCollider = playerBaseGo.AddComponent<SphereCollider>();
            }
            sphereCollider.isTrigger = true;
            sphereCollider.center = Vector3.zero;

            EffectArea effectArea = playerBaseGo.GetComponent<EffectArea>();
            if (!effectArea)
            {
                effectArea = playerBaseGo.AddComponent<EffectArea>();
            }
            effectArea.m_type = EffectArea.Type.PlayerBase;

            Plugin.LogDebug($"[PieceManager] Configured PlayerBase on '{targetGameObject.name}' (layer: {playerBaseGo.layer}).");
        }
    }
}
