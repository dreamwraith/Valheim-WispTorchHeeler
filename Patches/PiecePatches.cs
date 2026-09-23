using HarmonyLib;
using UnityEngine;
using WispTorchHeeler.Configuration;
using WispTorchHeeler.Lighting;
using WispTorchHeeler.Localization;
using WispTorchHeeler.Pieces;

namespace WispTorchHeeler.Patches
{
    public static class PiecePatches
    {
        /// <summary>
        /// Ensures existing world instances and newly placed native mist torches receive
        /// the WispTorchColorController component.
        /// </summary>
        [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
        public static class Piece_Awake_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Piece __instance)
            {
                if (!ModConfig.EnableMod.Value)
                {
                    return;
                }

                if (__instance == null || __instance.gameObject == null)
                {
                    return;
                }

                string prefabName = Utils.GetPrefabName(__instance.gameObject);
                if (prefabName != WispPieceManager.NativeMistTorchPrefab)
                {
                    return;
                }

                if (__instance.gameObject.GetComponent<WispTorchColorController>() == null)
                {
                    __instance.gameObject.AddComponent<WispTorchColorController>();
                    Plugin.LogDebug($"[PiecePatch] Injected WispTorchColorController into spawned '{prefabName}'.");
                }
            }
        }

        /// <summary>
        /// Appends interactive color painting and resetting hotkey prompts to wisp torches and lamps.
        /// Dynamically respects AffectNativeMistTorches and AllowTorchPainting configs without destructive component modification.
        /// </summary>
        [HarmonyPatch(typeof(HoverText), nameof(HoverText.GetHoverText))]
        public static class HoverText_GetHoverText_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(HoverText __instance, ref string __result)
            {
                if (!ModConfig.EnableMod.Value || !ModConfig.AllowTorchPainting.Value)
                {
                    return;
                }

                if (__instance == null)
                {
                    return;
                }

                var controller = __instance.GetComponentInParent<WispTorchColorController>()
                              ?? __instance.GetComponentInChildren<WispTorchColorController>();

                if (controller == null)
                {
                    return;
                }

                string prefabName = Utils.GetPrefabName(controller.gameObject);
                if (prefabName == WispPieceManager.NativeMistTorchPrefab && !ModConfig.AffectNativeMistTorches.Value)
                {
                    return;
                }

                string paintKey = ModConfig.PaintTorchHotkey.Value.ToString();
                string resetKey = ModConfig.ResetTorchHotkey.Value.ToString();
                string brushHexColor = ColorUtility.ToHtmlStringRGB(ModConfig.PainterColor.Value);

                string paintPrompt = ModLocalization.Localize(ModLocalization.TokenPromptPaint);
                string resetPrompt = ModLocalization.Localize(ModLocalization.TokenPromptReset);

                if (string.IsNullOrEmpty(__result))
                {
                    __result = global::Localization.instance != null ? global::Localization.instance.Localize(__instance.m_text) : __instance.m_text;
                }

                __result += $"\n[<color=#{brushHexColor}><b>{paintKey}</b></color>] {paintPrompt}   [<color=yellow><b>{resetKey}</b></color>] {resetPrompt}";
            }
        }
    }
}
