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
    }
}
