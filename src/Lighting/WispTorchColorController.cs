using System;
using System.Collections.Generic;
using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    /// <summary>
    /// Controls dynamic lighting, demister ball shaders, and particle spark coloring for tiered wisp torches and lamps.
    /// Event-driven: reacts to RPCs, world stream OnEnable(), and config SettingChanged events.
    /// Delegates rendering and material management to WispVisualApplicator.
    /// </summary>
    public class WispTorchColorController : MonoBehaviour, IPlaced
    {
        public static readonly int ColorKeyHash = "wisptorch_light_color".GetStableHashCode();
        public const float MinimumAlphaFloor = WispVisualApplicator.MinimumAlphaFloor;
        public static readonly Color NativeFallbackColor = WispVisualApplicator.NativeFallbackColor;

        public static readonly HashSet<WispTorchColorController> Instances = new();

        private ZNetView? _netView;
        private readonly WispVisualApplicator _visualApplicator = new();

        private CircleProjector? _circleProjector;
        private GameObject? _areaMarkerGameObject;

        private void Awake()
        {
            _netView = GetComponent<ZNetView>();
            _visualApplicator.Initialize(gameObject);

            if (_netView != null)
            {
                _netView.Register<string>("WTH_SetColor", RPC_SetColor);
            }
        }

        private void OnEnable()
        {
            Instances.Add(this);
            EnsureAreaMarker();
            RefreshColor();
            RefreshProperties();
        }

        private void Start()
        {
            if (_netView != null && !_netView.IsValid())
            {
                StartCoroutine(WaitForZDOAndInit());
            }
        }

        private System.Collections.IEnumerator WaitForZDOAndInit()
        {
            int maxFrames = 30;
            while (_netView != null && !_netView.IsValid() && maxFrames-- > 0)
            {
                yield return null;
            }

            if (_netView != null && _netView.IsValid())
            {
                RefreshColor();
            }
        }

        /// <summary>
        /// Native Valheim placement hook invoked immediately on the placing client when the piece is placed with the hammer.
        /// </summary>
        public void OnPlaced()
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.AutoPaintOnPlacement.Value || !ModConfig.AllowTorchPainting.Value)
            {
                return;
            }

            string prefabName = Utils.GetPrefabName(gameObject);
            if (prefabName == Pieces.WispPieceManager.NativeMistTorchPrefab && (ModConfig.AffectNativeMistTorches == null || !ModConfig.AffectNativeMistTorches.Value))
            {
                return;
            }

            if (_netView == null)
            {
                _netView = GetComponent<ZNetView>();
            }

            if (_netView == null || !_netView.IsValid())
            {
                return;
            }

            Color activeColor = ModConfig.PainterColor.Value;
            bool allowAlpha = ModConfig.EnableAlphaCustomization != null && ModConfig.EnableAlphaCustomization.Value;
            string hexColor = WispVisualApplicator.FormatColorHex(activeColor, allowAlpha);

            _netView.ClaimOwnership();
            _netView.GetZDO().Set(ColorKeyHash, hexColor);
            _netView.InvokeRPC(ZNetView.Everybody, "WTH_SetColor", hexColor);

            ApplyColor(activeColor);
            Plugin.LogDebug($"[PiecePlacement] Auto-painted newly placed '{prefabName}' with color #{hexColor}.");
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
            _visualApplicator.CleanupRuntimeMaterials();
        }

        /// <summary>
        /// Remote Procedure Call received when a torch is painted or reset.
        /// </summary>
        private void RPC_SetColor(long sender, string hexColor)
        {
            if (WispVisualApplicator.TryParseColorHex(hexColor, out Color color))
            {
                ApplyColor(color);
            }
            else
            {
                RefreshColor();
            }
        }

        /// <summary>
        /// Evaluates the active color hierarchy and applies it.
        /// Restores original native styling if AffectNativeMistTorches is disabled.
        /// </summary>
        public void RefreshColor()
        {
            if (ModConfig.AffectNativeMistTorches != null && !ModConfig.AffectNativeMistTorches.Value)
            {
                string prefabName = Utils.GetPrefabName(gameObject);
                if (prefabName == Pieces.WispPieceManager.NativeMistTorchPrefab)
                {
                    RestoreOriginalColors();
                    return;
                }
            }

            ApplyColor(ResolveColor());
        }

        public void RestoreOriginalColors()
        {
            _visualApplicator.RestoreOriginalColors();
        }

        public Color ResolveColor()
        {
            // 1. Per-Piece Painted Color from ZDO
            if (_netView != null && _netView.IsValid())
            {
                string zdoColorString = _netView.GetZDO().GetString(ColorKeyHash, string.Empty);
                if (WispVisualApplicator.TryParseColorHex(zdoColorString, out Color paintedColor))
                {
                    return paintedColor;
                }
            }

            // 2. Enforced Server Default Color
            if (ModConfig.EnforceServerDefaultColor != null && ModConfig.EnforceServerDefaultColor.Value)
            {
                return ModConfig.ServerDefaultLightColor != null ? ModConfig.ServerDefaultLightColor.Value : NativeFallbackColor;
            }

            // 3. Client Global Default Color
            if (ModConfig.DefaultLightColor != null)
            {
                return ModConfig.DefaultLightColor.Value;
            }

            // 4. Native Mistlands Fallback
            return NativeFallbackColor;
        }

        /// <summary>
        /// Applies the resolved color to point lights, particles, and demister ball/effect materials.
        /// Preserves native emissive intensity without blowout.
        /// </summary>
        public void ApplyColor(Color color)
        {
            bool allowAlpha = ModConfig.EnableAlphaCustomization != null && ModConfig.EnableAlphaCustomization.Value;
            _visualApplicator.ApplyColor(color, null, 1.0f, allowAlpha);
        }

        /// <summary>
        /// Applies current physical properties (mist clearance force fields, light intensity/range, health).
        /// </summary>
        public void RefreshProperties()
        {
            string prefabName = Utils.GetPrefabName(gameObject);
            Pieces.WispPieceManager.ApplyProperties(gameObject, prefabName);
            UpdateAreaMarkerRadius();
        }

        /// <summary>
        /// Retrieves the active mist suppression outer radius from the force field.
        /// </summary>
        public float GetMistSuppressionRadius()
        {
            var forceField = GetComponentInChildren<ParticleSystemForceField>(true);
            if (forceField != null && forceField.endRange > 0f)
            {
                return forceField.endRange;
            }
            return 0f;
        }

        /// <summary>
        /// Creates a clean GameObject with a pure CircleProjector boundary ring (no EffectArea or Colliders).
        /// </summary>
        public void EnsureAreaMarker()
        {
            if (_areaMarkerGameObject != null) return;
            var template = Pieces.WispPieceManager.AreaMarkerTemplate;
            if (template == null) return;

            var templateProjector = template.GetComponent<CircleProjector>() ?? template.GetComponentInChildren<CircleProjector>();
            if (templateProjector == null || templateProjector.m_prefab == null) return;

            _areaMarkerGameObject = new GameObject("_MistSuppressionMarker");
            _areaMarkerGameObject.transform.SetParent(transform, false);
            _areaMarkerGameObject.transform.localPosition = Vector3.zero;
            _areaMarkerGameObject.transform.localRotation = Quaternion.identity;

            _circleProjector = _areaMarkerGameObject.AddComponent<CircleProjector>();
            _circleProjector.m_prefab = templateProjector.m_prefab;
            _circleProjector.m_mask = templateProjector.m_mask;
            _circleProjector.m_speed = templateProjector.m_speed;
            _circleProjector.m_turns = 1f;

            UpdateAreaMarkerRadius();
        }

        /// <summary>
        /// Updates the CircleProjector radius to match the active mist suppression force field.
        /// </summary>
        public void UpdateAreaMarkerRadius()
        {
            if (_circleProjector == null) return;
            float radius = GetMistSuppressionRadius();
            if (radius <= 0f)
            {
                _areaMarkerGameObject?.SetActive(false);
                return;
            }

            _circleProjector.m_radius = radius;
            _circleProjector.m_nrOfSegments = Mathf.Clamp(Mathf.RoundToInt(radius * 5f), 24, 250);
            _circleProjector.CreateSegments();
            RefreshMarkerVisibility();
        }

        /// <summary>
        /// Shows or hides the boundary ring depending on the ShowMistSuppressionRadius config setting.
        /// </summary>
        public void RefreshMarkerVisibility()
        {
            if (_areaMarkerGameObject != null)
            {
                bool shouldShow = ModConfig.ShowMistSuppressionRadius != null
                               && ModConfig.ShowMistSuppressionRadius.Value
                               && GetMistSuppressionRadius() > 0f;
                _areaMarkerGameObject.SetActive(shouldShow);
            }
        }

        /// <summary>
        /// Refreshes all active area marker rings across the scene when the debug toggle changes.
        /// </summary>
        public static void RefreshAllMarkers()
        {
            foreach (var instance in Instances)
            {
                if (instance != null)
                {
                    instance.EnsureAreaMarker();
                    instance.RefreshMarkerVisibility();
                }
            }
        }

        /// <summary>
        /// Refreshes physical properties on all active instances matching the specified prefab name.
        /// </summary>
        public static void RefreshPropertiesForPrefab(string prefabName)
        {
            foreach (var instance in Instances)
            {
                if (instance != null && Utils.GetPrefabName(instance.gameObject) == prefabName)
                {
                    instance.RefreshProperties();
                }
            }
        }

        /// <summary>
        /// Initializes configuration change event listeners for wisp torches and lamps.
        /// </summary>
        public static void Initialize()
        {
            ModConfig.EnableMod.SettingChanged += (sender, args) => RefreshAll();
            ModConfig.DefaultLightColor.SettingChanged += (sender, args) => RefreshAll();
            ModConfig.ServerDefaultLightColor.SettingChanged += (sender, args) => RefreshAll();
            ModConfig.EnforceServerDefaultColor.SettingChanged += (sender, args) => RefreshAll();
            ModConfig.AffectNativeMistTorches.SettingChanged += (sender, args) => RefreshAll();
            ModConfig.EnableAlphaCustomization.SettingChanged += (sender, args) => RefreshAll();
            ModConfig.ShowMistSuppressionRadius.SettingChanged += (sender, args) => RefreshAllMarkers();
        }

        /// <summary>
        /// Refreshes all active torch instances across the scene when config settings change.
        /// </summary>
        public static void RefreshAll()
        {
            foreach (var instance in Instances)
            {
                if (instance != null)
                {
                    instance.RefreshColor();
                    instance.RefreshProperties();
                }
            }
        }
    }
}
