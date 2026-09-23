using System;
using System.Collections.Generic;
using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    /// <summary>
    /// Controls dynamic lighting, demister ball shaders, and particle spark coloring for tiered wisp torches and lamps.
    /// Event-driven: reacts to RPCs, world stream OnEnable(), and config SettingChanged events.
    /// </summary>
    public class WispTorchColorController : MonoBehaviour, IPlaced
    {
        public static readonly int ColorKeyHash = "wisptorch_light_color".GetStableHashCode();
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int s_color = Shader.PropertyToID("_Color");
        private static readonly int s_tintColor = Shader.PropertyToID("_TintColor");
        public static readonly Color NativeFallbackColor = new Color(0.706f, 0.941f, 1.0f, 1.0f); // #B4F0FF
        public const float MinimumAlphaFloor = 0.15f;

        /// <summary>
        /// Exact material names for demister balls, animated wisp swirls, and spark particles.
        /// Model bases (wood posts, metal frames, brackets) are excluded.
        /// </summary>
        private static readonly HashSet<string> s_targetedMaterialNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "demister_ball",
            "demister_ball_intense",
            "demister_ball_weak",
            "demister_ball_v_intense",
            "demister",
            "demister_sparcs",
            "wispysmoke",
            "slowwispysmoke"
        };

        public static readonly HashSet<WispTorchColorController> Instances = new();

        private ZNetView? _netView;
        private Light[] _lights = Array.Empty<Light>();
        private ParticleSystem[] _particleSystems = Array.Empty<ParticleSystem>();
        private Color[] _originalLightColors = Array.Empty<Color>();
        private ParticleSystem.MinMaxGradient[] _originalParticleGradients = Array.Empty<ParticleSystem.MinMaxGradient>();

        private Renderer[] _ballRenderers = Array.Empty<Renderer>();
        private Material[] _originalSharedMaterials = Array.Empty<Material>();
        private Color[] _originalEmissionColors = Array.Empty<Color>();
        private Color[] _originalAlbedoColors = Array.Empty<Color>();
        private Color[] _originalTintColors = Array.Empty<Color>();

        private CircleProjector? _circleProjector;
        private GameObject? _areaMarkerGameObject;

        private void Awake()
        {
            _netView = GetComponent<ZNetView>();
            _lights = GetComponentsInChildren<Light>(true);
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            _originalLightColors = new Color[_lights.Length];
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                _originalLightColors[lightIndex] = _lights[lightIndex].color;
            }

            _originalParticleGradients = new ParticleSystem.MinMaxGradient[_particleSystems.Length];
            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                _originalParticleGradients[particleIndex] = _particleSystems[particleIndex].main.startColor;
            }

            // Identify and cache all demister ball, animated effect, and particle spark renderers
            var targetRenderers = new List<Renderer>();
            foreach (var currentRenderer in GetComponentsInChildren<Renderer>(true))
            {
                if (currentRenderer == null || currentRenderer.sharedMaterial == null) continue;
                string materialName = currentRenderer.sharedMaterial.name;
                int instanceIndex = materialName.IndexOf(" (Instance)", StringComparison.OrdinalIgnoreCase);
                if (instanceIndex >= 0)
                {
                    materialName = materialName.Substring(0, instanceIndex);
                }

                if (s_targetedMaterialNames.Contains(materialName))
                {
                    targetRenderers.Add(currentRenderer);
                }
            }

            _ballRenderers = targetRenderers.ToArray();
            _originalSharedMaterials = new Material[_ballRenderers.Length];
            _originalEmissionColors = new Color[_ballRenderers.Length];
            _originalAlbedoColors = new Color[_ballRenderers.Length];
            _originalTintColors = new Color[_ballRenderers.Length];

            for (int rendererIndex = 0; rendererIndex < _ballRenderers.Length; rendererIndex++)
            {
                var material = _ballRenderers[rendererIndex].sharedMaterial;
                _originalSharedMaterials[rendererIndex] = material;
                _originalEmissionColors[rendererIndex] = material.HasProperty(s_emissionColor) ? material.GetColor(s_emissionColor) : Color.white;
                _originalAlbedoColors[rendererIndex] = material.HasProperty(s_color) ? material.GetColor(s_color) : Color.white;
                _originalTintColors[rendererIndex] = material.HasProperty(s_tintColor) ? material.GetColor(s_tintColor) : Color.white;
            }

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
            activeColor.a = allowAlpha
                ? Mathf.Clamp(activeColor.a, MinimumAlphaFloor, 1.0f)
                : 1.0f;

            string hexColor = allowAlpha
                ? ColorUtility.ToHtmlStringRGBA(activeColor)
                : ColorUtility.ToHtmlStringRGB(activeColor);

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
            for (int rendererIndex = 0; rendererIndex < _ballRenderers.Length; rendererIndex++)
            {
                if (_ballRenderers[rendererIndex] != null && rendererIndex < _originalSharedMaterials.Length)
                {
                    var runtimeMaterial = _ballRenderers[rendererIndex].sharedMaterial;
                    if (runtimeMaterial != null && runtimeMaterial != _originalSharedMaterials[rendererIndex])
                    {
                        // Delay destruction so spawned debris fragments (TimedDestruction) render the material before it is freed
                        Destroy(runtimeMaterial, 10f);
                    }
                }
            }
        }

        /// <summary>
        /// Remote Procedure Call received when a torch is painted or reset.
        /// </summary>
        private void RPC_SetColor(long sender, string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor) || !ColorUtility.TryParseHtmlString(hexColor.StartsWith("#") ? hexColor : "#" + hexColor, out Color color))
            {
                RefreshColor();
            }
            else
            {
                ApplyColor(color);
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
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                if (_lights[lightIndex] != null && lightIndex < _originalLightColors.Length)
                {
                    _lights[lightIndex].color = _originalLightColors[lightIndex];
                }
            }
            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                if (_particleSystems[particleIndex] != null && particleIndex < _originalParticleGradients.Length)
                {
                    var mainModule = _particleSystems[particleIndex].main;
                    mainModule.startColor = _originalParticleGradients[particleIndex];
                }
            }
            for (int rendererIndex = 0; rendererIndex < _ballRenderers.Length; rendererIndex++)
            {
                if (_ballRenderers[rendererIndex] != null && rendererIndex < _originalSharedMaterials.Length)
                {
                    if (_ballRenderers[rendererIndex].sharedMaterial != _originalSharedMaterials[rendererIndex])
                    {
                        Destroy(_ballRenderers[rendererIndex].sharedMaterial);
                        _ballRenderers[rendererIndex].sharedMaterial = _originalSharedMaterials[rendererIndex];
                    }
                }
            }
        }

        public Color ResolveColor()
        {
            // 1. Per-Piece Painted Color from ZDO
            if (_netView != null && _netView.IsValid())
            {
                string zdoColorString = _netView.GetZDO().GetString(ColorKeyHash, string.Empty);
                if (!string.IsNullOrEmpty(zdoColorString) && ColorUtility.TryParseHtmlString(zdoColorString.StartsWith("#") ? zdoColorString : "#" + zdoColorString, out Color paintedColor))
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
            // 1. Point lights (Unity point lights strictly use RGB)
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                if (_lights[lightIndex] != null)
                {
                    _lights[lightIndex].color = color;
                }
            }

            bool allowAlpha = ModConfig.EnableAlphaCustomization != null && ModConfig.EnableAlphaCustomization.Value;
            float alphaMultiplier = allowAlpha ? Mathf.Clamp(color.a, MinimumAlphaFloor, 1.0f) : 1.0f;

            // 2. Particle Systems: tint RGB and scale alpha curve while preserving original gradient keyframe timings
            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                if (_particleSystems[particleIndex] != null && particleIndex < _originalParticleGradients.Length)
                {
                    var mainModule = _particleSystems[particleIndex].main;
                    mainModule.startColor = TintMinMaxGradient(in _originalParticleGradients[particleIndex], color, alphaMultiplier);
                }
            }

            // 3. Demister Ball, Animated Effects & Particle Spark Materials
            for (int rendererIndex = 0; rendererIndex < _ballRenderers.Length; rendererIndex++)
            {
                var renderer = _ballRenderers[rendererIndex];
                if (renderer == null) continue;

                var runtimeMaterial = renderer.material;
                if (runtimeMaterial == null) continue;

                TintMaterialProperty(runtimeMaterial, s_emissionColor, _originalEmissionColors[rendererIndex], color, 1.0f);
                TintMaterialProperty(runtimeMaterial, s_color, _originalAlbedoColors[rendererIndex], color, alphaMultiplier);
                TintMaterialProperty(runtimeMaterial, s_tintColor, _originalTintColors[rendererIndex], color, alphaMultiplier);
            }
        }

        private static void TintMaterialProperty(Material material, int propertyId, in Color originalColor, Color targetColor, float alphaMultiplier)
        {
            if (!material.HasProperty(propertyId)) return;
            float intensity = Mathf.Max(originalColor.r, Mathf.Max(originalColor.g, originalColor.b));
            material.SetColor(propertyId, new Color(
                targetColor.r * intensity,
                targetColor.g * intensity,
                targetColor.b * intensity,
                originalColor.a * alphaMultiplier));
        }

        private static ParticleSystem.MinMaxGradient TintMinMaxGradient(in ParticleSystem.MinMaxGradient originalGradient, Color targetColor, float alphaMultiplier)
        {
            switch (originalGradient.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new Color(targetColor.r, targetColor.g, targetColor.b, originalGradient.color.a * alphaMultiplier);

                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        new Color(targetColor.r, targetColor.g, targetColor.b, originalGradient.colorMin.a * alphaMultiplier),
                        new Color(targetColor.r, targetColor.g, targetColor.b, originalGradient.colorMax.a * alphaMultiplier));

                case ParticleSystemGradientMode.Gradient:
                    return TintGradient(originalGradient.gradient, targetColor, alphaMultiplier);

                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        TintGradient(originalGradient.gradientMin, targetColor, alphaMultiplier),
                        TintGradient(originalGradient.gradientMax, targetColor, alphaMultiplier));

                default:
                    return originalGradient;
            }
        }

        private static Gradient TintGradient(Gradient? sourceGradient, Color targetColor, float alphaMultiplier)
        {
            if (sourceGradient == null) return new Gradient();
            var originalColorKeys = sourceGradient.colorKeys;
            var newColorKeys = new GradientColorKey[originalColorKeys.Length];
            for (int keyIndex = 0; keyIndex < originalColorKeys.Length; keyIndex++)
            {
                newColorKeys[keyIndex] = new GradientColorKey(new Color(targetColor.r, targetColor.g, targetColor.b), originalColorKeys[keyIndex].time);
            }

            GradientAlphaKey[] alphaKeys;
            if (alphaMultiplier < 0.999f)
            {
                var originalAlphaKeys = sourceGradient.alphaKeys;
                alphaKeys = new GradientAlphaKey[originalAlphaKeys.Length];
                for (int keyIndex = 0; keyIndex < originalAlphaKeys.Length; keyIndex++)
                {
                    alphaKeys[keyIndex] = new GradientAlphaKey(originalAlphaKeys[keyIndex].alpha * alphaMultiplier, originalAlphaKeys[keyIndex].time);
                }
            }
            else
            {
                alphaKeys = sourceGradient.alphaKeys;
            }

            var tintedGradient = new Gradient();
            tintedGradient.SetKeys(newColorKeys, alphaKeys);
            return tintedGradient;
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
