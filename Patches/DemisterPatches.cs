using System;
using HarmonyLib;
using WispTorchHeeler.Configuration;
using WispTorchHeeler.Lighting;

namespace WispTorchHeeler.Patches
{
    public static class DemisterPatches
    {
        private static readonly int s_demisterBallHash = "demister_ball".GetStableHashCode();

        /// <summary>
        /// Attaches WispDemisterColorController to demister balls on all clients (local and remote)
        /// when their ZNetView is initialized.
        /// </summary>
        [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake))]
        public static class ZNetView_Awake_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ZNetView __instance)
            {
                if (!ModConfig.EnableMod.Value || __instance == null || !__instance.IsValid())
                {
                    return;
                }

                // Zero-allocation integer hash check filters out 99.99% of network entities instantly.
                // Guard with GetComponentInChildren<Demister> ensures only true demister balls are modified.
                if (__instance.GetZDO().GetPrefab() != s_demisterBallHash || __instance.GetComponentInChildren<Demister>(true) == null)
                {
                    return;
                }

                if (__instance.GetComponent<WispDemisterColorController>() == null)
                {
                    __instance.gameObject.AddComponent<WispDemisterColorController>();
                    Plugin.LogDebug($"[DemisterPatch] Attached WispDemisterColorController to networked Wisplight '{__instance.name}'.");
                }
            }
        }

        /// <summary>
        /// Smooths and calms the equipped Wisplight (SE_Demister) idle hover animation and wander radius.
        /// Eliminates rapid fly-like buzzing and jittering around the player's head.
        /// </summary>
        [HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.UpdateStatusEffect))]
        public static class SE_Demister_UpdateStatusEffect_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(SE_Demister __instance)
            {
                if (!ModConfig.EnableMod.Value || !ModConfig.EnableDemisterCustomization.Value)
                {
                    __instance.m_noiseSpeed = 1f;
                    __instance.m_rotationSpeed = 1f;
                    __instance.m_noiseDistance = 1f;
                    __instance.m_noiseDistanceInterior = 0.2f;
                    return;
                }

                float speedMultiplier = ModConfig.DemisterIdleSpeedMultiplier.Value;
                float radiusMultiplier = ModConfig.DemisterIdleRadiusMultiplier.Value;

                __instance.m_noiseSpeed = 1f * speedMultiplier;
                __instance.m_rotationSpeed = 1f * speedMultiplier;
                __instance.m_noiseDistance = 1f * radiusMultiplier;
                __instance.m_noiseDistanceInterior = 0.2f * radiusMultiplier;
            }
        }
    }
}
