using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // colors the Wisplight follower ball and carries it for the hip styles. force field ranges stay untouched,
    // but a hip style pins the ball to the hip and the mist clearance center goes with it
    public class WispDemisterColorController : MonoBehaviour
    {
        public static readonly int DemisterColorHash = "wisptorch_demister_color".GetStableHashCode();
        public static readonly int DemisterMountHash = "wisptorch_demister_mount".GetStableHashCode();
        public static readonly int DemisterLightOffHash = "wisptorch_demister_light_off".GetStableHashCode();
        public static readonly KeyValuePair<int, int> DemisterWearerHash = ZDO.GetHashZDOID("wisptorch_demister_wearer");
        private static readonly int s_demisterBallHash = "demister_ball".GetStableHashCode();
        private static readonly int s_demisterHeatHash = "wisptorch_demister_heat".GetStableHashCode();
        private static readonly WaitForSeconds s_remoteCheckInterval = new WaitForSeconds(1.5f);
        private static readonly WaitForSeconds s_heatCheckInterval = new WaitForSeconds(1.0f);
        public static readonly HashSet<WispDemisterColorController> Instances = new();

        private ZNetView _networkView = null!;
        private readonly WispVisualApplicator _visualApplicator = new();
        private readonly WispDemisterHipMount _hipMount = new();
        private readonly WispDemisterHeat _heat = new();
        private Coroutine? _remoteSyncCoroutine;
        private Coroutine _heatCoroutine = null!;
        private string _lastRemoteHexColor = string.Empty;
        private bool _hasResolvedLight;

        private void Awake()
        {
            _networkView = GetComponent<ZNetView>();
            _visualApplicator.Initialize(gameObject);
            _hipMount.Initialize(gameObject);
            _heat.Initialize(transform);
        }

        private void OnEnable()
        {
            Instances.Add(this);
            if (_networkView.IsValid() && _networkView.IsOwner())
            {
                // outside the spawn console command, owned balls only come from the local players SE_Demister
                _networkView.GetZDO().Set(DemisterWearerHash, Player.m_localPlayer.GetZDOID());
                SyncLocalPlayerDemisterState();
            }
            RefreshMount();
            RefreshColor();
            _heatCoroutine = StartCoroutine(MonitorHeat());
        }

        private void OnDisable()
        {
            Instances.Remove(this);
            if (_remoteSyncCoroutine != null)
            {
                StopCoroutine(_remoteSyncCoroutine);
                _remoteSyncCoroutine = null;
            }

            StopCoroutine(_heatCoroutine);
            _heat.SetHeating(false);
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
            _hipMount.DestroyVisual();
        }

        public bool IsRemoteWisplight()
        {
            return _networkView.IsValid() && !_networkView.IsOwner();
        }

        private void EnsureRemoteSyncCoroutine()
        {
            if (_remoteSyncCoroutine == null && IsRemoteWisplight())
            {
                _remoteSyncCoroutine = StartCoroutine(MonitorRemoteZDOState());
            }
        }

        // remote balls only. reads the replicated zdo locally so it costs no traffic, and catches the wearer once they load in
        private IEnumerator MonitorRemoteZDOState()
        {
            while (IsRemoteWisplight() && ModConfig.EnableMod.Value && ModConfig.EnableDemisterCustomization.Value)
            {
                string currentZdoColor = QueryRemoteHexColor();
                bool mountChanged = RefreshMount();
                if (mountChanged || currentZdoColor != _lastRemoteHexColor)
                {
                    _lastRemoteHexColor = currentZdoColor;
                    RefreshColor();
                }

                yield return s_remoteCheckInterval;
            }

            _remoteSyncCoroutine = null;
        }

        // every ball runs this, owned or remote, even with customization off. heat is gameplay, not cosmetics
        private IEnumerator MonitorHeat()
        {
            while (true)
            {
                RefreshHeat();
                yield return s_heatCheckInterval;
            }
        }

        // the owner is the wearer, so it decides from its own player. everyone else just reads the zdo
        private void RefreshHeat()
        {
            if (!_networkView.IsValid())
            {
                return;
            }

            ZDO zdo = _networkView.GetZDO();
            if (_networkView.IsOwner())
            {
                bool isHeating = WispDemisterHeat.CanLocalPlayerHeat(_hipMount.IsLitOnHip);
                if (zdo.GetBool(s_demisterHeatHash) != isHeating)
                {
                    zdo.Set(s_demisterHeatHash, isHeating);
                    Plugin.LogDebug($"[DemisterSync] Synced demister heat {isHeating} to ball ZDO.");
                }
            }

            _heat.SetHeating(zdo.GetBool(s_demisterHeatHash));
        }

        private string QueryRemoteHexColor()
        {
            if (_networkView.IsValid())
            {
                return _networkView.GetZDO().GetString(DemisterColorHash, string.Empty);
            }

            return string.Empty;
        }

        public static void AttachIfDemisterBall(ZNetView netView)
        {
            if (!ModConfig.EnableMod.Value || !netView.IsValid())
            {
                return;
            }

            if (netView.GetZDO().GetPrefab() != s_demisterBallHash)
            {
                return;
            }

            if (!netView.TryGetComponent<WispDemisterColorController>(out _))
            {
                netView.gameObject.AddComponent<WispDemisterColorController>();
                Plugin.LogDebug($"[DemisterPatch] Attached WispDemisterColorController to networked Wisplight '{netView.name}'.");
            }
        }

        public static void Initialize()
        {
            Action syncAndRefresh = () =>
            {
                // the mount style is read back from the ball zdo so it has to be written before the refresh
                SyncLocalPlayerDemisterState();
                RefreshAll();
            };

            // these change what goes on the ball zdo
            ModConfig.EnableMod.SettingChanged += (sender, eventArgs) => syncAndRefresh();
            ModConfig.EnableDemisterCustomization.SettingChanged += (sender, eventArgs) => syncAndRefresh();
            ModConfig.DemisterColor.SettingChanged += (sender, eventArgs) => syncAndRefresh();
            ModConfig.DemisterEnableAlpha.SettingChanged += (sender, eventArgs) => syncAndRefresh();
            ModConfig.DemisterSyncMultiplayer.SettingChanged += (sender, eventArgs) => syncAndRefresh();
            ModConfig.DemisterMountStyle.SettingChanged += (sender, eventArgs) => syncAndRefresh();

            // local only sliders, nothing to write
            ModConfig.DemisterLightIntensityMultiplier.SettingChanged += (sender, eventArgs) => RefreshAll();
            ModConfig.DemisterLightRangeMultiplier.SettingChanged += (sender, eventArgs) => RefreshAll();
            ModConfig.DemisterEmissionIntensityMultiplier.SettingChanged += (sender, eventArgs) => RefreshAll();
        }

        public void RefreshColor()
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.EnableDemisterCustomization.Value)
            {
                if (_remoteSyncCoroutine != null)
                {
                    StopCoroutine(_remoteSyncCoroutine);
                    _remoteSyncCoroutine = null;
                }
                RestoreOriginalVisuals();
                return;
            }

            EnsureRemoteSyncCoroutine();

            WispDemisterHipVisual? hipVisual = _hipMount.Visual;
            if (TryResolveColor(out Color targetColor))
            {
                bool isRemote = IsRemoteWisplight();
                float lightIntensityMultiplier = !isRemote ? ModConfig.DemisterLightIntensityMultiplier.Value : 1.0f;
                float lightRangeMultiplier = !isRemote ? ModConfig.DemisterLightRangeMultiplier.Value : 1.0f;
                float emissionMultiplier = !isRemote ? ModConfig.DemisterEmissionIntensityMultiplier.Value : 1.0f;
                bool allowAlpha = isRemote || ModConfig.DemisterEnableAlpha.Value;

                _visualApplicator.ApplyColor(targetColor, lightIntensityMultiplier, lightRangeMultiplier, emissionMultiplier, allowAlpha);
                if (hipVisual)
                {
                    hipVisual.Applicator.ApplyColor(targetColor, lightIntensityMultiplier, lightRangeMultiplier, emissionMultiplier, allowAlpha);
                }
            }
            else
            {
                RestoreOriginalVisuals();
            }

            // the cage stays on show with the light off and both color paths above relight its glow
            if (hipVisual && !_hipMount.IsLit)
            {
                hipVisual.Applicator.DarkenEmission();
            }
        }

        private void RestoreOriginalVisuals()
        {
            _visualApplicator.RestoreOriginalColors();
            WispDemisterHipVisual? hipVisual = _hipMount.Visual;
            if (hipVisual)
            {
                hipVisual.Applicator.RestoreOriginalColors();
            }
        }

        // the owner writes its own style and light state to the zdo before refreshing, so local and remote balls share this path
        private (WornDemisterMount Mount, Player? Wearer, bool IsLit) ResolveMount()
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.EnableDemisterCustomization.Value || !_networkView.IsValid())
            {
                return (WornDemisterMount.Floating, null, true);
            }

            ZDO zdo = _networkView.GetZDO();
            GameObject wearerObject = ZNetScene.instance.FindInstance(zdo.GetZDOID(DemisterWearerHash));
            Player? wearer = wearerObject && wearerObject.TryGetComponent(out Player player) ? player : null;
            return ((WornDemisterMount)zdo.GetInt(DemisterMountHash), wearer, !zdo.GetBool(DemisterLightOffHash));
        }

        // true when the visuals changed and need a color refresh
        // the light state goes first so a new hip copy or the returning ball comes up already switched
        private bool RefreshMount()
        {
            (WornDemisterMount mount, Player? wearer, bool isLit) = ResolveMount();
            bool lightChanged = _hipMount.SetLit(isLit);

            // the first pass only picks up the state the ball spawned with, nobody pressed anything
            if (lightChanged && _hasResolvedLight)
            {
                WispDemisterLightSwitchEffects.Play(transform.position, ResolveColor());
            }

            _hasResolvedLight = true;
            bool mountChanged = mount != WornDemisterMount.Floating && wearer
                ? _hipMount.Attach(mount, wearer)
                : _hipMount.Detach();
            return lightChanged || mountChanged;
        }

        // mist clears around the ball, so pinning it to the hip copy moves the clearance there too
        public static void PinBallToHip(SE_Demister demisterEffect)
        {
            GameObject ball = demisterEffect.m_ballInstance;
            if (!ball.TryGetComponent(out WispDemisterColorController controller))
            {
                return;
            }

            WispDemisterHipVisual? hipVisual = controller._hipMount.Visual;
            if (!hipVisual)
            {
                return;
            }

            ball.transform.position = hipVisual.Anchor.position;
            demisterEffect.m_ballVel = Vector3.zero;
        }

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

            targetColor = ModConfig.DemisterColor.Value;
            return true;
        }

        public Color ResolveColor()
        {
            return TryResolveColor(out Color targetColor) ? targetColor : WispVisualApplicator.NativeFallbackColor;
        }

        // the mount and light state always get written even with color sync off, other clients need them to hide the ball and draw the hip copy
        public static void SyncLocalPlayerDemisterState()
        {
            bool isCustomized = ModConfig.EnableMod.Value && ModConfig.EnableDemisterCustomization.Value;
            string targetHexColor = !isCustomized || !ModConfig.DemisterSyncMultiplayer.Value
                ? string.Empty
                : WispVisualApplicator.FormatColorHex(ModConfig.DemisterColor.Value, ModConfig.DemisterEnableAlpha.Value);
            WornDemisterMount targetMount = isCustomized ? ModConfig.DemisterMountStyle.Value : WornDemisterMount.Floating;
            bool targetLightOff = isCustomized && WispDemisterLightSwitch.IsLocalPlayerLightOff();

            foreach (WispDemisterColorController instance in Instances)
            {
                if (instance)
                {
                    instance.SyncOwnedOrbState(targetHexColor, targetMount, targetLightOff);
                }
            }
        }

        private void SyncOwnedOrbState(string targetHexColor, WornDemisterMount targetMount, bool targetLightOff)
        {
            if (!_networkView.IsValid() || !_networkView.IsOwner())
            {
                return;
            }

            ZDO zdo = _networkView.GetZDO();
            if (zdo.GetString(DemisterColorHash, string.Empty) != targetHexColor)
            {
                zdo.Set(DemisterColorHash, targetHexColor);
                Plugin.LogDebug($"[DemisterSync] Synced demister color #{targetHexColor} to ball ZDO.");
            }

            if (zdo.GetInt(DemisterMountHash) != (int)targetMount)
            {
                zdo.Set(DemisterMountHash, (int)targetMount);
                Plugin.LogDebug($"[DemisterSync] Synced demister mount style {targetMount} to ball ZDO.");
            }

            if (zdo.GetBool(DemisterLightOffHash) != targetLightOff)
            {
                zdo.Set(DemisterLightOffHash, targetLightOff);
                Plugin.LogDebug($"[DemisterSync] Synced demister light off {targetLightOff} to ball ZDO.");
            }
        }

        public static void RefreshAll()
        {
            foreach (WispDemisterColorController instance in Instances)
            {
                if (instance)
                {
                    instance.RefreshMount();
                    instance.RefreshColor();
                }
            }
        }
    }
}
