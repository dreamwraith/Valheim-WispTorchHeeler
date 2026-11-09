using HarmonyLib;
using WispTorchHeeler.Lighting;

namespace WispTorchHeeler.Patches
{
    public static class DemisterPatches
    {
        [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake))]
        public static class ZNetView_Awake_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ZNetView __instance) => WispDemisterColorController.AttachIfDemisterBall(__instance);
        }

        [HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.Setup))]
        public static class SE_Demister_Setup_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(SE_Demister __instance) => WispDemisterIdleMotion.OnSetup(__instance);
        }

        [HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.UpdateStatusEffect))]
        public static class SE_Demister_UpdateStatusEffect_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(SE_Demister __instance) => WispDemisterColorController.PinBallToHip(__instance);
        }
    }
}
