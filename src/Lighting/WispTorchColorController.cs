using System;
using System.Collections.Generic;
using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    public class WispTorchColorController : MonoBehaviour, IPlaced
    {
        public static readonly int ColorKeyHash = "wisptorch_light_color".GetStableHashCode();
        public const float MinimumAlphaFloor = WispVisualApplicator.MinimumAlphaFloor;

        public static readonly HashSet<WispTorchColorController> Instances = new();

        private ZNetView? _netView;
        private readonly WispVisualApplicator _visualApplicator = new();

        private CircleProjector? _circleProjector;
        private GameObject? _areaMarkerGameObject;

        private void Awake()
        {
            _netView = GetComponent<ZNetView>();
            _visualApplicator.Initialize(gameObject);

            if (_netView)
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
            if (_netView && !_netView.IsValid())
            {
                StartCoroutine(WaitForZDOAndInit());
            }
        }

        private System.Collections.IEnumerator WaitForZDOAndInit()
        {
            int maxFrames = 30;
            while (_netView && !_netView.IsValid() && maxFrames-- > 0)
            {
                yield return null;
            }

            if (_netView && _netView.IsValid())
            {
                RefreshColor();
            }
        }

        public void OnPlaced()
        {
            if (!ModConfig.EnableMod.Value || !ModConfig.AutoPaintOnPlacement.Value || !ModConfig.AllowTorchPainting.Value)
            {
                return;
            }

            string prefabName = Utils.GetPrefabName(gameObject);
            if (prefabName == Pieces.WispPieceManager.NativeMistTorchPrefab && !ModConfig.AffectNativeMistTorches.Value)
            {
                return;
            }

            if (!_netView)
            {
                _netView = GetComponent<ZNetView>();
            }

            if (!_netView || !_netView.IsValid())
            {
                return;
            }

            Color activeColor = ModConfig.PainterColor.Value;
            bool allowAlpha = ModConfig.EnableAlphaCustomization.Value;
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

        public void RefreshColor()
        {
            if (!ModConfig.AffectNativeMistTorches.Value)
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

        // painted zdo color wins, then the enforced server default, then the client default
        public Color ResolveColor()
        {
            if (_netView && _netView.IsValid())
            {
                string zdoColorString = _netView.GetZDO().GetString(ColorKeyHash, string.Empty);
                if (WispVisualApplicator.TryParseColorHex(zdoColorString, out Color paintedColor))
                {
                    return paintedColor;
                }
            }

            return ModConfig.EnforceServerDefaultColor.Value
                ? ModConfig.ServerDefaultLightColor.Value
                : ModConfig.DefaultLightColor.Value;
        }

        public void ApplyColor(Color color)
        {
            bool allowAlpha = ModConfig.EnableAlphaCustomization.Value;
            _visualApplicator.ApplyColor(color, null, null, 1.0f, allowAlpha);
        }

        public void RefreshProperties()
        {
            string prefabName = Utils.GetPrefabName(gameObject);
            Pieces.WispPieceManager.ApplyProperties(gameObject, prefabName);
            UpdateAreaMarkerRadius();
        }

        public float GetMistSuppressionRadius()
        {
            ParticleSystemForceField forceField = GetComponentInChildren<ParticleSystemForceField>(true);
            if (forceField && forceField.endRange > 0f)
            {
                return forceField.endRange;
            }
            return 0f;
        }

        // builds a bare CircleProjector instead of cloning the template marker, so only the ring comes along
        public void EnsureAreaMarker()
        {
            if (_areaMarkerGameObject) return;
            GameObject? template = Pieces.WispPieceManager.AreaMarkerTemplate;
            if (!template) return;

            if (!template.TryGetComponent(out CircleProjector templateProjector))
            {
                templateProjector = template.GetComponentInChildren<CircleProjector>();
            }
            if (!templateProjector || !templateProjector.m_prefab) return;

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

        public void UpdateAreaMarkerRadius()
        {
            if (!_circleProjector) return;
            float radius = GetMistSuppressionRadius();
            if (radius <= 0f)
            {
                _circleProjector.gameObject.SetActive(false);
                return;
            }

            _circleProjector.m_radius = radius;
            _circleProjector.m_nrOfSegments = Mathf.Clamp(Mathf.RoundToInt(radius * 5f), 24, 250);
            _circleProjector.CreateSegments();
            RefreshMarkerVisibility();
        }

        public void RefreshMarkerVisibility()
        {
            if (_areaMarkerGameObject)
            {
                bool shouldShow = ModConfig.ShowMistSuppressionRadius.Value && GetMistSuppressionRadius() > 0f;
                _areaMarkerGameObject.SetActive(shouldShow);
            }
        }

        public static void RefreshAllMarkers()
        {
            foreach (WispTorchColorController instance in Instances)
            {
                if (instance)
                {
                    instance.EnsureAreaMarker();
                    instance.RefreshMarkerVisibility();
                }
            }
        }

        public static void RefreshPropertiesForPrefab(string prefabName)
        {
            foreach (WispTorchColorController instance in Instances)
            {
                if (instance && Utils.GetPrefabName(instance.gameObject) == prefabName)
                {
                    instance.RefreshProperties();
                }
            }
        }

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

        public static void RefreshAll()
        {
            foreach (WispTorchColorController instance in Instances)
            {
                if (instance)
                {
                    instance.RefreshColor();
                    instance.RefreshProperties();
                }
            }
        }
    }
}
