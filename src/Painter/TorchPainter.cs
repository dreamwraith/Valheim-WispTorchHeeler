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

        private static int GetBaseRaycastMask()
        {
            if (s_baseRaycastMask == -1)
            {
                s_baseRaycastMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid");
            }
            return s_baseRaycastMask;
        }

        /// <summary>
        /// Attempts to paint or reset the currently hovered or targeted torch.
        /// Hybrid approach:
        /// 1. Taps directly into Player.m_localPlayer.m_hovering (zero raycast overhead if already looking at piece).
        /// 2. Falls back to a camera raycast up to PainterRaycastDistance if targeting a distant torch beyond interact range.
        /// </summary>
        public static void TryPaintOrReset(bool isReset)
        {
            if (!ModConfig.EnableMod.Value)
            {
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                return;
            }

            WispTorchColorController? targetController = null;

            // Step 1: Check existing hover object from Valheim's native hover loop
            if (localPlayer.m_hovering != null)
            {
                targetController = ResolveControllerFromObject(localPlayer.m_hovering);
                if (targetController != null)
                {
                    Plugin.LogDebug($"[TorchPainter] Targeted torch via native hover: '{targetController.gameObject.name}'.");
                }
            }

            // Step 2: Check hovered piece (active when holding building hammer or aiming at pieces)
            if (targetController == null && localPlayer.m_hoveringPiece != null)
            {
                targetController = ResolveControllerFromObject(localPlayer.m_hoveringPiece.gameObject);
                if (targetController != null)
                {
                    Plugin.LogDebug($"[TorchPainter] Targeted torch via hovering piece: '{targetController.gameObject.name}'.");
                }
            }

            // Step 3: If no hovered torch, fall back to camera raycast and eye raycast up to PainterRaycastDistance
            if (targetController == null)
            {
                float maxDistance = ModConfig.PainterRaycastDistance.Value;
                int layerMask = GetBaseRaycastMask() | localPlayer.m_interactMask;

                Camera? activeCamera = GameCamera.instance != null && GameCamera.instance.m_camera != null
                    ? GameCamera.instance.m_camera
                    : (Camera.main != null ? Camera.main : (GameCamera.instance != null ? GameCamera.instance.GetComponent<Camera>() : null));

                // Try center camera ray first
                if (activeCamera != null)
                {
                    Ray cameraRay = new Ray(activeCamera.transform.position, activeCamera.transform.forward);
                    targetController = RaycastForController(cameraRay, maxDistance, layerMask);
                }

                // Fall back to eye-level look ray
                if (targetController == null)
                {
                    Ray eyeRay = new Ray(localPlayer.GetEyePoint(), localPlayer.GetLookDir());
                    targetController = RaycastForController(eyeRay, maxDistance, layerMask);
                }
            }

            if (targetController == null)
            {
                Plugin.LogDebug("[TorchPainter] No valid wisp torch/lamp targeted.");
                return;
            }

            string prefabName = Utils.GetPrefabName(targetController.gameObject);
            if (prefabName == Pieces.WispPieceManager.NativeMistTorchPrefab && (ModConfig.AffectNativeMistTorches == null || !ModConfig.AffectNativeMistTorches.Value))
            {
                Plugin.LogDebug("[TorchPainter] Painting native mist torches is disabled in config.");
                return;
            }

            // Security Check 1: Master Server Switch
            if (!ModConfig.AllowTorchPainting.Value)
            {
                Plugin.LogDebug("[TorchPainter] Painting is globally disabled by server policy.");
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, ModLocalization.Localize(ModLocalization.TokenMsgCantPaintDisabled));
                }
                return;
            }

            // Security Check 2: Ward Protection
            Vector3 targetPosition = targetController.transform.position;
            if (!ModConfig.AllowPaintingInProtectedWards.Value)
            {
                bool hasAccess = PrivateArea.CheckAccess(targetPosition, 0f, true);
                if (!hasAccess)
                {
                    bool isAdmin = (ModConfig.ConfigSync != null && ModConfig.ConfigSync.IsAdmin)
                        || (SynchronizationManager.Instance != null && SynchronizationManager.Instance.PlayerIsAdmin);

                    if (ModConfig.AdminBypassesWardCheck.Value && isAdmin)
                    {
                        Plugin.LogDebug("[TorchPainter] Ward access denied, but player is admin and AdminBypassesWardCheck is enabled. Access granted.");
                    }
                    else
                    {
                        Plugin.LogDebug("[TorchPainter] Ward access denied. Painting blocked.");
                        if (MessageHud.instance != null)
                        {
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, ModLocalization.Localize(ModLocalization.TokenMsgCantPaintWarded));
                        }
                        return;
                    }
                }
            }

            var netView = targetController.GetComponent<ZNetView>();
            if (netView == null || !netView.IsValid())
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
                bool allowAlpha = ModConfig.EnableAlphaCustomization != null && ModConfig.EnableAlphaCustomization.Value;
                activeColor.a = allowAlpha
                    ? Mathf.Clamp(activeColor.a, WispTorchColorController.MinimumAlphaFloor, 1.0f)
                    : 1.0f;

                hexColor = allowAlpha
                    ? ColorUtility.ToHtmlStringRGBA(activeColor)
                    : ColorUtility.ToHtmlStringRGB(activeColor);

                targetController.ApplyColor(activeColor);
                feedbackMessage = ModLocalization.Localize(ModLocalization.TokenMsgPainted, "#" + hexColor);
            }

            // Sync custom color to ZDO and broadcast to all clients
            netView.ClaimOwnership();
            netView.GetZDO().Set(WispTorchColorController.ColorKeyHash, hexColor);
            netView.InvokeRPC(ZNetView.Everybody, "WTH_SetColor", hexColor);

            if (ModConfig.ShowPainterFeedback.Value && MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, feedbackMessage);
            }

            PlayFeedbackEffect(targetPosition);
            Plugin.LogDebug(isReset
                ? $"[TorchPainter] Reset color on '{targetController.gameObject.name}'."
                : $"[TorchPainter] Applied color #{hexColor} to '{targetController.gameObject.name}'.");
        }

        private static WispTorchColorController? ResolveControllerFromObject(GameObject targetGameObject)
        {
            if (targetGameObject == null) return null;

            return targetGameObject.GetComponent<WispTorchColorController>()
                ?? targetGameObject.GetComponentInParent<WispTorchColorController>()
                ?? targetGameObject.GetComponentInChildren<WispTorchColorController>();
        }

        private static WispTorchColorController? RaycastForController(Ray ray, float maxDistance, int layerMask)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, layerMask);
            if (hits == null || hits.Length == 0) return null;

            Array.Sort(hits, (firstHit, secondHit) => firstHit.distance.CompareTo(secondHit.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                var controller = ResolveControllerFromObject(hit.collider.gameObject);
                if (controller != null)
                {
                    Plugin.LogDebug($"[TorchPainter] Targeted torch via raycast ({hit.distance:F1}m): '{controller.gameObject.name}'.");
                    return controller;
                }
            }
            return null;
        }

        private static void PlayFeedbackEffect(Vector3 position)
        {
            if (ZNetScene.instance == null) return;

            GameObject? feedbackPrefab = ZNetScene.instance.GetPrefab("sfx_wisp_spawn")
                                      ?? ZNetScene.instance.GetPrefab("vfx_wisp_spawn")
                                      ?? ZNetScene.instance.GetPrefab("sfx_build_hammer_metal");
            if (feedbackPrefab != null)
            {
                UnityEngine.Object.Instantiate(feedbackPrefab, position, Quaternion.identity);
            }
        }
    }
}
