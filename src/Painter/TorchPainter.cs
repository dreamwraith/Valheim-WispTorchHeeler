using System;
using Jotunn.Managers;
using UnityEngine;
using WispTorchHeeler.Configuration;
using WispTorchHeeler.Lighting;
using WispTorchHeeler.Localization;

namespace WispTorchHeeler.Painter
{
    public static class TorchPainter
    {
        private static int s_baseRaycastMask = -1;
        private static readonly string[] s_feedbackPrefabNames = { "sfx_wisp_spawn", "vfx_wisp_spawn", "sfx_build_hammer_metal" };

        private static int GetBaseRaycastMask()
        {
            if (s_baseRaycastMask == -1)
            {
                s_baseRaycastMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid");
            }
            return s_baseRaycastMask;
        }

        // hover targets come free since the game already raycast for them, the extra raycasts only run for torches past interact range
        public static void TryPaintOrReset(bool isReset)
        {
            if (!ModConfig.EnableMod.Value)
            {
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            if (!localPlayer)
            {
                return;
            }

            WispTorchColorController? targetController = null;

            if (localPlayer.m_hovering)
            {
                targetController = ResolveControllerFromObject(localPlayer.m_hovering);
                if (targetController)
                {
                    Plugin.LogDebug($"[TorchPainter] Targeted torch via native hover: '{targetController.gameObject.name}'.");
                }
            }

            if (!targetController && localPlayer.m_hoveringPiece)
            {
                targetController = ResolveControllerFromObject(localPlayer.m_hoveringPiece.gameObject);
                if (targetController)
                {
                    Plugin.LogDebug($"[TorchPainter] Targeted torch via hovering piece: '{targetController.gameObject.name}'.");
                }
            }

            if (!targetController)
            {
                float maxDistance = ModConfig.PainterRaycastDistance.Value;
                int layerMask = GetBaseRaycastMask() | localPlayer.m_interactMask;

                Camera? activeCamera = GameCamera.instance && GameCamera.instance.m_camera
                    ? GameCamera.instance.m_camera
                    : (Camera.main ? Camera.main : (GameCamera.instance ? GameCamera.instance.GetComponent<Camera>() : null));

                if (activeCamera)
                {
                    Ray cameraRay = new Ray(activeCamera.transform.position, activeCamera.transform.forward);
                    targetController = RaycastForController(cameraRay, maxDistance, layerMask);
                }

                if (!targetController)
                {
                    Ray eyeRay = new Ray(localPlayer.GetEyePoint(), localPlayer.GetLookDir());
                    targetController = RaycastForController(eyeRay, maxDistance, layerMask);
                }
            }

            if (!targetController)
            {
                Plugin.LogDebug("[TorchPainter] No valid wisp torch/lamp targeted.");
                return;
            }

            string prefabName = Utils.GetPrefabName(targetController.gameObject);
            if (prefabName == Pieces.WispPieceManager.NativeMistTorchPrefab && !ModConfig.AffectNativeMistTorches.Value)
            {
                Plugin.LogDebug("[TorchPainter] Painting native mist torches is disabled in config.");
                return;
            }

            if (!ModConfig.AllowTorchPainting.Value)
            {
                Plugin.LogDebug("[TorchPainter] Painting is globally disabled by server policy.");
                if (MessageHud.instance)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, ModLocalization.Localize(ModLocalization.TokenMsgCantPaintDisabled));
                }
                return;
            }

            Vector3 targetPosition = targetController.transform.position;
            if (!ModConfig.AllowPaintingInProtectedWards.Value)
            {
                bool hasAccess = PrivateArea.CheckAccess(targetPosition, 0f, true);
                if (!hasAccess)
                {
                    bool isAdmin = ModConfig.ConfigSync.IsAdmin || SynchronizationManager.Instance.PlayerIsAdmin;

                    if (ModConfig.AdminBypassesWardCheck.Value && isAdmin)
                    {
                        Plugin.LogDebug("[TorchPainter] Ward access denied, but player is admin and AdminBypassesWardCheck is enabled. Access granted.");
                    }
                    else
                    {
                        Plugin.LogDebug("[TorchPainter] Ward access denied. Painting blocked.");
                        if (MessageHud.instance)
                        {
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, ModLocalization.Localize(ModLocalization.TokenMsgCantPaintWarded));
                        }
                        return;
                    }
                }
            }

            ZNetView netView = targetController.GetComponent<ZNetView>();
            if (!netView || !netView.IsValid())
            {
                Plugin.Log.LogWarning("[TorchPainter] Target piece has no valid ZNetView.");
                return;
            }

            string hexColor;
            string feedbackMessage;

            if (isReset)
            {
                hexColor = string.Empty;
                targetController.RefreshColor();
                feedbackMessage = ModLocalization.Localize(ModLocalization.TokenMsgReset);
            }
            else
            {
                Color activeColor = ModConfig.PainterColor.Value;
                bool allowAlpha = ModConfig.EnableAlphaCustomization.Value;
                activeColor.a = allowAlpha
                    ? Mathf.Clamp(activeColor.a, WispTorchColorController.MinimumAlphaFloor, 1.0f)
                    : 1.0f;

                hexColor = allowAlpha
                    ? ColorUtility.ToHtmlStringRGBA(activeColor)
                    : ColorUtility.ToHtmlStringRGB(activeColor);

                targetController.ApplyColor(activeColor);
                feedbackMessage = ModLocalization.Localize(ModLocalization.TokenMsgPainted, "#" + hexColor);
            }

            netView.ClaimOwnership();
            netView.GetZDO().Set(WispTorchColorController.ColorKeyHash, hexColor);
            netView.InvokeRPC(ZNetView.Everybody, "WTH_SetColor", hexColor);

            if (ModConfig.ShowPainterFeedback.Value && MessageHud.instance)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, feedbackMessage);
            }

            PlayFeedbackEffect(targetPosition);
            Plugin.LogDebug(isReset
                ? $"[TorchPainter] Reset color on '{targetController.gameObject.name}'."
                : $"[TorchPainter] Applied color #{hexColor} to '{targetController.gameObject.name}'.");
        }

        public static void AppendPaintPrompt(HoverText hoverText, ref string displayText)
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.AllowTorchPainting.Value)
            {
                return;
            }

            WispTorchColorController? controller = ResolveControllerFromObject(hoverText.gameObject);

            if (!controller)
            {
                return;
            }

            string prefabName = Utils.GetPrefabName(controller.gameObject);
            if (prefabName == Pieces.WispPieceManager.NativeMistTorchPrefab && !ModConfig.AffectNativeMistTorches.Value)
            {
                return;
            }

            string paintKey = ModConfig.PaintTorchHotkey.Value.ToString();
            string resetKey = ModConfig.ResetTorchHotkey.Value.ToString();
            string brushHexColor = ColorUtility.ToHtmlStringRGB(ModConfig.PainterColor.Value);

            string paintPrompt = ModLocalization.Localize(ModLocalization.TokenPromptPaint);
            string resetPrompt = ModLocalization.Localize(ModLocalization.TokenPromptReset);

            if (string.IsNullOrEmpty(displayText))
            {
                displayText = global::Localization.instance.Localize(hoverText.m_text);
            }

            displayText += $"\n[<color=#{brushHexColor}><b>{paintKey}</b></color>] {paintPrompt}\n[<color=yellow><b>{resetKey}</b></color>] {resetPrompt}";
        }

        private static WispTorchColorController? ResolveControllerFromObject(GameObject targetGameObject)
        {
            if (!targetGameObject) return null;

            WispTorchColorController parentController = targetGameObject.GetComponentInParent<WispTorchColorController>();
            return parentController ? parentController : targetGameObject.GetComponentInChildren<WispTorchColorController>();
        }

        private static WispTorchColorController? RaycastForController(Ray ray, float maxDistance, int layerMask)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, layerMask);
            if (hits == null || hits.Length == 0) return null;

            Array.Sort(hits, (firstHit, secondHit) => firstHit.distance.CompareTo(secondHit.distance));
            foreach (RaycastHit hit in hits)
            {
                if (!hit.collider) continue;

                WispTorchColorController? controller = ResolveControllerFromObject(hit.collider.gameObject);
                if (controller)
                {
                    Plugin.LogDebug($"[TorchPainter] Targeted torch via raycast ({hit.distance:F1}m): '{controller.gameObject.name}'.");
                    return controller;
                }
            }
            return null;
        }

        private static void PlayFeedbackEffect(Vector3 position)
        {
            foreach (string prefabName in s_feedbackPrefabNames)
            {
                GameObject feedbackPrefab = ZNetScene.instance.GetPrefab(prefabName);
                if (feedbackPrefab)
                {
                    UnityEngine.Object.Instantiate(feedbackPrefab, position, Quaternion.identity);
                    return;
                }
            }
        }
    }
}
