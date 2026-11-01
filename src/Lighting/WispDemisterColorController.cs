using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    /// <summary>
    /// Controls dynamic cosmetic lighting, demister ball shaders, and particle spark coloring
    /// for the flying Wisplight follower orb (SE_Demister).
    /// Strictly cosmetic: delegates rendering and material management to WispVisualApplicator,
    /// leaving mist clearance force fields and equipment stats completely untouched.
    /// </summary>
    public class WispDemisterColorController : MonoBehaviour
    {
        public static readonly int DemisterColorHash = "wisptorch_demister_color".GetStableHashCode();
        public static readonly HashSet<WispDemisterColorController> Instances = new();

        private ZNetView? _networkView;
        private readonly WispVisualApplicator _visualApplicator = new();
        private Coroutine? _remoteSyncCoroutine;
        private string _lastRemoteHexColor = string.Empty;

        public ZNetView? NetView => _networkView;

        private void Awake()
        {
            _networkView = GetComponent<ZNetView>();
            _visualApplicator.Initialize(gameObject);
        }

        private void OnEnable()
        {
            Instances.Add(this);
            if (_networkView != null && _networkView.IsValid() && _networkView.IsOwner())
            {
                SyncLocalPlayerDemisterColor();
            }
            RefreshColor();
        }

        private void OnDisable()
        {
            Instances.Remove(this);
            if (_remoteSyncCoroutine != null)
            {
                StopCoroutine(_remoteSyncCoroutine);
                _remoteSyncCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
            if (_remoteSyncCoroutine != null)
            {
                StopCoroutine(_remoteSyncCoroutine);
                _remoteSyncCoroutine = null;
            }
            _visualApplicator.CleanupRuntimeMaterials();
        }

        public bool IsRemoteWisplight()
        {
            return _networkView != null && _networkView.IsValid() && !_networkView.IsOwner();
        }

        private void EnsureRemoteSyncCoroutine()
        {
            if (_remoteSyncCoroutine == null && IsRemoteWisplight())
            {
                _remoteSyncCoroutine = StartCoroutine(MonitorRemoteZDOColor());
            }
        }

        /// <summary>
        /// Periodically checks the replicated orb ZDO for color updates.
        /// Consumes zero network bandwidth (reads local ZDO table maintained by native engine replication).
        /// </summary>
        private IEnumerator MonitorRemoteZDOColor()
        {
            var waitInterval = new WaitForSeconds(1.5f);

            while (IsRemoteWisplight() && ModConfig.EnableMod.Value && ModConfig.EnableDemisterCustomization.Value)
            {
                string currentZdoColor = QueryRemoteHexColor();
                if (currentZdoColor != _lastRemoteHexColor)
                {
                    _lastRemoteHexColor = currentZdoColor;
                    RefreshColor();
                }

                yield return waitInterval;
            }

            _remoteSyncCoroutine = null;
        }

        private string QueryRemoteHexColor()
        {
            // Strictly authoritative: read from the orb's own replicated ZDO
            if (_networkView != null && _networkView.IsValid())
            {
                return _networkView.GetZDO().GetString(DemisterColorHash, string.Empty);
            }

            return string.Empty;
        }

        /// <summary>
        /// Initializes configuration change event listeners for the flying Wisplight follower (SE_Demister).
        /// </summary>
        public static void Initialize()
        {
            Action syncAndRefresh = () =>
            {
                RefreshAll();
                SyncLocalPlayerDemisterColor();
            };

            // Networked color changes
            ModConfig.EnableMod.SettingChanged += (sender, args) => syncAndRefresh();
            ModConfig.EnableDemisterCustomization.SettingChanged += (sender, args) => syncAndRefresh();
            ModConfig.DemisterColor.SettingChanged += (sender, args) => syncAndRefresh();
            ModConfig.DemisterEnableAlpha.SettingChanged += (sender, args) => syncAndRefresh();
            ModConfig.DemisterSyncMultiplayer.SettingChanged += (sender, args) => syncAndRefresh();

            // Local sliders: ONLY refresh visuals locally (ZERO network ZDO traffic)
            ModConfig.DemisterLightIntensityMultiplier.SettingChanged += (sender, args) => RefreshAll();
            ModConfig.DemisterEmissionIntensityMultiplier.SettingChanged += (sender, args) => RefreshAll();
        }

        /// <summary>
        /// Evaluates active color and intensity settings, then applies them via WispVisualApplicator.
        /// Restores native original visuals when customization or mod is disabled.
        /// </summary>
        public void RefreshColor()
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.EnableDemisterCustomization.Value)
            {
                if (_remoteSyncCoroutine != null)
                {
                    StopCoroutine(_remoteSyncCoroutine);
                    _remoteSyncCoroutine = null;
                }
                _visualApplicator.RestoreOriginalColors();
                return;
            }

            EnsureRemoteSyncCoroutine();

            if (!TryResolveColor(out Color targetColor))
            {
                _visualApplicator.RestoreOriginalColors();
                return;
            }

            bool isRemote = IsRemoteWisplight();
            float lightIntensityMultiplier = (!isRemote && ModConfig.DemisterLightIntensityMultiplier != null)
                ? ModConfig.DemisterLightIntensityMultiplier.Value
                : 1.0f;
            float emissionMultiplier = (!isRemote && ModConfig.DemisterEmissionIntensityMultiplier != null)
                ? ModConfig.DemisterEmissionIntensityMultiplier.Value
                : 1.0f;
            bool allowAlpha = isRemote || (ModConfig.DemisterEnableAlpha != null && ModConfig.DemisterEnableAlpha.Value);

            _visualApplicator.ApplyColor(targetColor, lightIntensityMultiplier, emissionMultiplier, allowAlpha);
        }

        /// <summary>
        /// Resolves target color: uses remote orb ZDO color for other players in multiplayer,
        /// or client configuration for the local player. Returns false if no custom color is active.
        /// </summary>
        public bool TryResolveColor(out Color targetColor)
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.EnableDemisterCustomization.Value)
            {
                targetColor = WispVisualApplicator.NativeFallbackColor;
                return false;
            }

            if (IsRemoteWisplight())
            {
                string remoteHexColor = QueryRemoteHexColor();
                _lastRemoteHexColor = remoteHexColor;
                return WispVisualApplicator.TryParseColorHex(remoteHexColor, out targetColor);
            }

            if (ModConfig.DemisterColor != null)
            {
                targetColor = ModConfig.DemisterColor.Value;
                return true;
            }

            targetColor = WispVisualApplicator.NativeFallbackColor;
            return false;
        }

        /// <summary>
        /// Resolves target color with native fallback.
        /// </summary>
        public Color ResolveColor()
        {
            return TryResolveColor(out Color targetColor) ? targetColor : WispVisualApplicator.NativeFallbackColor;
        }

        /// <summary>
        /// Broadcasts local player's demister color setting to any active owned wisplight
        /// follower orbs for multiplayer visibility.
        /// </summary>
        public static void SyncLocalPlayerDemisterColor()
        {
            string targetHexColor = !ModConfig.EnableMod.Value || !ModConfig.EnableDemisterCustomization.Value || !ModConfig.DemisterSyncMultiplayer.Value
                ? string.Empty
                : WispVisualApplicator.FormatColorHex(ModConfig.DemisterColor.Value, ModConfig.DemisterEnableAlpha.Value);

            foreach (var instance in Instances)
            {
                if (instance != null)
                {
                    instance.SyncOwnedOrbColor(targetHexColor);
                }
            }
        }

        private void SyncOwnedOrbColor(string targetHexColor)
        {
            if (_networkView == null || !_networkView.IsValid() || !_networkView.IsOwner())
            {
                return;
            }

            var zdo = _networkView.GetZDO();
            if (zdo.GetString(DemisterColorHash, string.Empty) != targetHexColor)
            {
                zdo.Set(DemisterColorHash, targetHexColor);
                Plugin.LogDebug($"[DemisterSync] Synced demister color #{targetHexColor} to ball ZDO.");
            }
        }

        /// <summary>
        /// Refreshes all active worn demister instances across the scene when config settings change.
        /// </summary>
        public static void RefreshAll()
        {
            foreach (var instance in Instances)
            {
                if (instance != null)
                {
                    instance.RefreshColor();
                }
            }
        }
    }
}
