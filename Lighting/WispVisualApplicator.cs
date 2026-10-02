using System;
using System.Collections.Generic;
using UnityEngine;

namespace WispTorchHeeler.Lighting
{
    /// <summary>
    /// Shared graphics applicator managing point lights, particle systems, demister ball shaders,
    /// and spark particles across torches, lamps, and body-worn demister accessories.
    /// Strictly cosmetic: never modifies physics, force fields, or mist clearance ranges.
    /// </summary>
    public class WispVisualApplicator
    {
        public static readonly Color NativeFallbackColor = new Color(0.706f, 0.941f, 1.0f, 1.0f); // #B4F0FF
        public const float MinimumAlphaFloor = 0.15f;

        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int s_color = Shader.PropertyToID("_Color");
        private static readonly int s_tintColor = Shader.PropertyToID("_TintColor");

        /// <summary>
        /// Exact material names for demister balls, animated wisp swirls, outer glows, and spark particles.
        /// Excludes base structures (wood posts, iron brackets, stone bases).
        /// </summary>
        public static readonly HashSet<string> TargetedMaterialNames = new(StringComparer.OrdinalIgnoreCase)
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

        private Light[] _lights = Array.Empty<Light>();
        private float[] _originalLightIntensities = Array.Empty<float>();
        private Color[] _originalLightColors = Array.Empty<Color>();

        private ParticleSystem[] _particleSystems = Array.Empty<ParticleSystem>();
        private ParticleSystem.MinMaxGradient[] _originalParticleGradients = Array.Empty<ParticleSystem.MinMaxGradient>();
        // Infinite-lifetime particles (e.g. flare halo) require stop+clear+play for color changes to take effect.
        private bool[] _isInfiniteLifetimeParticle = Array.Empty<bool>();

        private Renderer[] _targetRenderers = Array.Empty<Renderer>();
        private Material[] _originalSharedMaterials = Array.Empty<Material>();
        private Color[] _originalEmissionColors = Array.Empty<Color>();
        private Color[] _originalAlbedoColors = Array.Empty<Color>();
        private Color[] _originalTintColors = Array.Empty<Color>();

        public int LightCount => _lights.Length;
        public int ParticleSystemCount => _particleSystems.Length;
        public int TargetRendererCount => _targetRenderers.Length;

        /// <summary>
        /// Discovers and caches all child lights, particle systems, and demister renderers on the target GameObject.
        /// </summary>
        public void Initialize(GameObject rootGameObject)
        {
            if (rootGameObject == null)
            {
                return;
            }

            _lights = rootGameObject.GetComponentsInChildren<Light>(true);
            _originalLightColors = new Color[_lights.Length];
            _originalLightIntensities = new float[_lights.Length];
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                _originalLightColors[lightIndex] = _lights[lightIndex].color;
                _originalLightIntensities[lightIndex] = _lights[lightIndex].intensity;
            }

            _particleSystems = rootGameObject.GetComponentsInChildren<ParticleSystem>(true);
            _originalParticleGradients = new ParticleSystem.MinMaxGradient[_particleSystems.Length];
            _isInfiniteLifetimeParticle = new bool[_particleSystems.Length];
            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                var particleSystem = _particleSystems[particleIndex];
                _originalParticleGradients[particleIndex] = particleSystem.main.startColor;

                // Infinite-lifetime halo defaults to World space, stranding it at the teleport origin.
                // Switch to Local so it follows the orb, and flag it for stop+clear+play on color changes.
                var mainModule = particleSystem.main;
                if (float.IsInfinity(mainModule.startLifetime.constant))
                {
                    mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
                    _isInfiniteLifetimeParticle[particleIndex] = true;
                }
            }

            var validRenderers = new List<Renderer>();
            foreach (var currentRenderer in rootGameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (currentRenderer != null && IsTargetMaterial(currentRenderer.sharedMaterial))
                {
                    validRenderers.Add(currentRenderer);
                }
            }

            _targetRenderers = validRenderers.ToArray();
            _originalSharedMaterials = new Material[_targetRenderers.Length];
            _originalEmissionColors = new Color[_targetRenderers.Length];
            _originalAlbedoColors = new Color[_targetRenderers.Length];
            _originalTintColors = new Color[_targetRenderers.Length];

            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                var material = _targetRenderers[rendererIndex].sharedMaterial;
                _originalSharedMaterials[rendererIndex] = material;
                _originalEmissionColors[rendererIndex] = material.HasProperty(s_emissionColor) ? material.GetColor(s_emissionColor) : Color.white;
                _originalAlbedoColors[rendererIndex] = material.HasProperty(s_color) ? material.GetColor(s_color) : Color.white;
                _originalTintColors[rendererIndex] = material.HasProperty(s_tintColor) ? material.GetColor(s_tintColor) : Color.white;
            }
        }

        /// <summary>
        /// Applies target color, light intensity multiplier, and shader emission multiplier to all cached components.
        /// Preserves native gradient keyframe timing and HDR intensity curves without blowout.
        /// </summary>
        public void ApplyColor(Color targetColor, float? lightIntensityMultiplier = null, float emissionMultiplier = 1.0f, bool allowAlpha = false)
        {
            // 1. Point lights (Unity point lights use RGB)
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                if (_lights[lightIndex] != null)
                {
                    _lights[lightIndex].color = targetColor;
                    if (lightIntensityMultiplier.HasValue && lightIndex < _originalLightIntensities.Length)
                    {
                        _lights[lightIndex].intensity = _originalLightIntensities[lightIndex] * lightIntensityMultiplier.Value;
                    }
                }
            }

            float alphaMultiplier = allowAlpha ? Mathf.Clamp(targetColor.a, MinimumAlphaFloor, 1.0f) : 1.0f;

            // 2. Particle Systems: tint RGB and scale alpha curve while preserving original gradient keyframe timings.
            // Infinite-lifetime particles are stopped and replayed so the new startColor takes effect immediately.
            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                if (_particleSystems[particleIndex] != null && particleIndex < _originalParticleGradients.Length)
                {
                    var mainModule = _particleSystems[particleIndex].main;
                    mainModule.startColor = TintMinMaxGradient(in _originalParticleGradients[particleIndex], targetColor, alphaMultiplier);

                    if (particleIndex < _isInfiniteLifetimeParticle.Length && _isInfiniteLifetimeParticle[particleIndex])
                    {
                        _particleSystems[particleIndex].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        _particleSystems[particleIndex].Play();
                    }
                }
            }

            // 3. Demister Ball, Animated Effects & Particle Spark Materials
            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                var currentRenderer = _targetRenderers[rendererIndex];
                if (currentRenderer == null)
                {
                    continue;
                }

                var runtimeMaterial = currentRenderer.material;
                if (runtimeMaterial == null)
                {
                    continue;
                }

                TintEmissiveMaterialProperty(runtimeMaterial, s_emissionColor, _originalEmissionColors[rendererIndex], targetColor, emissionMultiplier);
                TintStandardMaterialProperty(runtimeMaterial, s_color, _originalAlbedoColors[rendererIndex], targetColor, alphaMultiplier);
                TintStandardMaterialProperty(runtimeMaterial, s_tintColor, _originalTintColors[rendererIndex], targetColor, alphaMultiplier);
            }
        }

        /// <summary>
        /// Restores original light colors, intensities, particle start gradients, and shared materials.
        /// </summary>
        public void RestoreOriginalColors()
        {
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                if (_lights[lightIndex] != null && lightIndex < _originalLightColors.Length && lightIndex < _originalLightIntensities.Length)
                {
                    _lights[lightIndex].color = _originalLightColors[lightIndex];
                    _lights[lightIndex].intensity = _originalLightIntensities[lightIndex];
                }
            }

            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                if (_particleSystems[particleIndex] != null && particleIndex < _originalParticleGradients.Length)
                {
                    var mainModule = _particleSystems[particleIndex].main;
                    mainModule.startColor = _originalParticleGradients[particleIndex];

                    if (particleIndex < _isInfiniteLifetimeParticle.Length && _isInfiniteLifetimeParticle[particleIndex])
                    {
                        _particleSystems[particleIndex].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        _particleSystems[particleIndex].Play();
                    }
                }
            }

            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                if (_targetRenderers[rendererIndex] != null && rendererIndex < _originalSharedMaterials.Length)
                {
                    if (_targetRenderers[rendererIndex].sharedMaterial != _originalSharedMaterials[rendererIndex])
                    {
                        UnityEngine.Object.Destroy(_targetRenderers[rendererIndex].sharedMaterial);
                        _targetRenderers[rendererIndex].sharedMaterial = _originalSharedMaterials[rendererIndex];
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up runtime instanced materials with a delayed destroy to prevent debris glitches and memory leaks.
        /// </summary>
        public void CleanupRuntimeMaterials()
        {
            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                if (_targetRenderers[rendererIndex] != null && rendererIndex < _originalSharedMaterials.Length)
                {
                    var runtimeMaterial = _targetRenderers[rendererIndex].sharedMaterial;
                    if (runtimeMaterial != null && runtimeMaterial != _originalSharedMaterials[rendererIndex])
                    {
                        UnityEngine.Object.Destroy(runtimeMaterial, 10f);
                    }
                }
            }
        }

        private static void TintEmissiveMaterialProperty(Material material, int propertyId, in Color originalColor, Color targetColor, float emissionMultiplier)
        {
            if (!material.HasProperty(propertyId))
            {
                return;
            }

            float baseIntensity = Mathf.Max(originalColor.r, Mathf.Max(originalColor.g, originalColor.b));
            float finalIntensity = baseIntensity * emissionMultiplier;
            material.SetColor(propertyId, new Color(
                targetColor.r * finalIntensity,
                targetColor.g * finalIntensity,
                targetColor.b * finalIntensity,
                originalColor.a));
        }

        private static void TintStandardMaterialProperty(Material material, int propertyId, in Color originalColor, Color targetColor, float alphaMultiplier)
        {
            if (!material.HasProperty(propertyId))
            {
                return;
            }

            float intensity = Mathf.Max(originalColor.r, Mathf.Max(originalColor.g, originalColor.b));
            material.SetColor(propertyId, new Color(
                targetColor.r * intensity,
                targetColor.g * intensity,
                targetColor.b * intensity,
                originalColor.a * alphaMultiplier));
        }

        public static ParticleSystem.MinMaxGradient TintMinMaxGradient(in ParticleSystem.MinMaxGradient originalGradient, Color targetColor, float alphaMultiplier)
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

        public static Gradient TintGradient(Gradient? sourceGradient, Color targetColor, float alphaMultiplier)
        {
            if (sourceGradient == null)
            {
                return new Gradient();
            }

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
        /// Formats a Color as an HTML RGB or RGBA hex string.
        /// </summary>
        public static string FormatColorHex(Color color, bool allowAlpha)
        {
            float alpha = allowAlpha ? Mathf.Clamp(color.a, MinimumAlphaFloor, 1.0f) : 1.0f;
            Color formattedColor = new Color(color.r, color.g, color.b, alpha);
            return allowAlpha
                ? ColorUtility.ToHtmlStringRGBA(formattedColor)
                : ColorUtility.ToHtmlStringRGB(formattedColor);
        }

        /// <summary>
        /// Safely parses an HTML hex string (with or without leading '#') into a Color.
        /// </summary>
        public static bool TryParseColorHex(string? hexString, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(hexString))
            {
                return false;
            }

            string normalizedHex = hexString!.StartsWith("#") ? hexString! : "#" + hexString;
            return ColorUtility.TryParseHtmlString(normalizedHex, out color);
        }

        /// <summary>
        /// Extracts the base material name by stripping any runtime Unity ' (Instance)' suffix.
        /// </summary>
        public static string NormalizeMaterialName(Material? material)
        {
            if (material == null)
            {
                return string.Empty;
            }

            string materialName = material.name;
            int instanceIndex = materialName.IndexOf(" (Instance)", StringComparison.OrdinalIgnoreCase);
            return instanceIndex >= 0 ? materialName.Substring(0, instanceIndex) : materialName;
        }

        /// <summary>
        /// Determines whether a material is a targeted demister ball, spark, or animated wisp material.
        /// </summary>
        public static bool IsTargetMaterial(Material? material)
        {
            string baseName = NormalizeMaterialName(material);
            return !string.IsNullOrEmpty(baseName) && TargetedMaterialNames.Contains(baseName);
        }
    }
}
