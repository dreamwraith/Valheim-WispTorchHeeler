using HarmonyLib;
using WispTorchHeeler.Painter;

namespace WispTorchHeeler.Patches
{
    public static class PainterPatches
    {
        [HarmonyPatch(typeof(HoverText), nameof(HoverText.GetHoverText))]
        public static class HoverText_GetHoverText_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(HoverText __instance, ref string __result) => TorchPainter.AppendPaintPrompt(__instance, ref __result);
        }
    }
}
