using System;
using System.Collections.Generic;
using UnityEngine;

namespace WispTorchHeeler.Lighting
{
    // shared by torches, lamps and the worn demister. only touches lights, particles and materials, never force fields or mist ranges
    public class WispVisualApplicator
    {
        public static readonly Color NativeFallbackColor = new Color(0.706f, 0.941f, 1.0f, 1.0f); // #B4F0FF
        public const float MinimumAlphaFloor = 0.15f;

        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int s_color = Shader.PropertyToID("_Color");
        private static readonly int s_tintColor = Shader.PropertyToID("_TintColor");

        // wisp and demister materials only, so wood posts, iron brackets and stone bases keep their look
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

        // these only get the emission tint, filled at runtime with mod materials like the hip cage lantern
        public static readonly HashSet<string> EmissionOnlyMaterialNames = new(StringComparer.OrdinalIgnoreCase);

        private Light[] _lights = Array.Empty<Light>();
        private float[] _originalLightIntensities = Array.Empty<float>();
        private float[] _originalLightRanges = Array.Empty<float>();
        private Color[] _originalLightColors = Array.Empty<Color>();

        private ParticleSystem[] _particleSystems = Array.Empty<ParticleSystem>();
        private ParticleSystem.MinMaxGradient[] _originalParticleGradients = Array.Empty<ParticleSystem.MinMaxGradient>();
        // infinite lifetime particles like the flare halo never re-emit, so a new start color only shows after a stop, clear and play
        private bool[] _isInfiniteLifetimeParticle = Array.Empty<bool>();

        private Renderer[] _targetRenderers = Array.Empty<Renderer>();
        private Material[] _originalSharedMaterials = Array.Empty<Material>();
        private Color[] _originalEmissionColors = Array.Empty<Color>();
        private Color[] _originalAlbedoColors = Array.Empty<Color>();
        private Color[] _originalTintColors = Array.Empty<Color>();
        private bool[] _isEmissionOnlyRenderer = Array.Empty<bool>();

        public int LightCount => _lights.Length;
        public int ParticleSystemCount => _particleSystems.Length;
        public int TargetRendererCount => _targetRenderers.Length;

        public void Initialize(GameObject rootGameObject)
        {
            if (!rootGameObject)
            {
                return;
            }

            _lights = rootGameObject.GetComponentsInChildren<Light>(true);
            _originalLightColors = new Color[_lights.Length];
            _originalLightIntensities = new float[_lights.Length];
            _originalLightRanges = new float[_lights.Length];
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                Light light = _lights[lightIndex];
                _originalLightColors[lightIndex] = light.color;

                // LightFlicker and LightLod only fill m_light in their Awake. before that their base fields are still initializers,
                // after it light.intensity and light.range are whatever the flicker and the lod ramp left there, so read whichever holds the real value
                _originalLightIntensities[lightIndex] = light.TryGetComponent(out LightFlicker lightFlicker) && lightFlicker.m_light
                    ? lightFlicker.m_baseIntensity
                    : light.intensity;
                _originalLightRanges[lightIndex] = light.TryGetComponent(out LightLod lightLod) && lightLod.m_light
                    ? lightLod.m_baseRange
                    : light.range;
            }

            _particleSystems = rootGameObject.GetComponentsInChildren<ParticleSystem>(true);
            _originalParticleGradients = new ParticleSystem.MinMaxGradient[_particleSystems.Length];
            _isInfiniteLifetimeParticle = new bool[_particleSystems.Length];
            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                ParticleSystem particleSystem = _particleSystems[particleIndex];
                _originalParticleGradients[particleIndex] = particleSystem.main.startColor;
                _isInfiniteLifetimeParticle[particleIndex] = float.IsInfinity(particleSystem.main.startLifetime.constant);
            }

            List<Renderer> validRenderers = new List<Renderer>();
            foreach (Renderer currentRenderer in rootGameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (currentRenderer && (IsTargetMaterial(currentRenderer.sharedMaterial) || IsEmissionOnlyMaterial(currentRenderer.sharedMaterial)))
                {
                    validRenderers.Add(currentRenderer);
                }
            }

            _targetRenderers = validRenderers.ToArray();
            _originalSharedMaterials = new Material[_targetRenderers.Length];
            _originalEmissionColors = new Color[_targetRenderers.Length];
            _originalAlbedoColors = new Color[_targetRenderers.Length];
            _originalTintColors = new Color[_targetRenderers.Length];
            _isEmissionOnlyRenderer = new bool[_targetRenderers.Length];

            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                Material material = _targetRenderers[rendererIndex].sharedMaterial;
                _originalSharedMaterials[rendererIndex] = material;
                _isEmissionOnlyRenderer[rendererIndex] = IsEmissionOnlyMaterial(material);
                _originalEmissionColors[rendererIndex] = material.HasProperty(s_emissionColor) ? material.GetColor(s_emissionColor) : Color.white;
                _originalAlbedoColors[rendererIndex] = material.HasProperty(s_color) ? material.GetColor(s_color) : Color.white;
                _originalTintColors[rendererIndex] = material.HasProperty(s_tintColor) ? material.GetColor(s_tintColor) : Color.white;
            }
        }

        public void ApplyColor(Color targetColor, float? lightIntensityMultiplier = null, float? lightRangeMultiplier = null, float emissionMultiplier = 1.0f, bool allowAlpha = false)
        {
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                if (_lights[lightIndex])
                {
                    _lights[lightIndex].color = targetColor;
                    if (lightIntensityMultiplier.HasValue && lightIndex < _originalLightIntensities.Length)
                    {
                        SetLightIntensity(_lights[lightIndex], _originalLightIntensities[lightIndex] * lightIntensityMultiplier.Value);
                    }

                    if (lightRangeMultiplier.HasValue)
                    {
                        SetLightRange(_lights[lightIndex], _originalLightRanges[lightIndex] * lightRangeMultiplier.Value);
                    }
                }
            }

            float alphaMultiplier = allowAlpha ? Mathf.Clamp(targetColor.a, MinimumAlphaFloor, 1.0f) : 1.0f;

            // gradient key timings stay, only the colors and alpha change
            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                if (_particleSystems[particleIndex] && particleIndex < _originalParticleGradients.Length)
                {
                    ParticleSystem.MainModule mainModule = _particleSystems[particleIndex].main;
                    mainModule.startColor = TintMinMaxGradient(in _originalParticleGradients[particleIndex], targetColor, alphaMultiplier);

                    if (particleIndex < _isInfiniteLifetimeParticle.Length && _isInfiniteLifetimeParticle[particleIndex])
                    {
                        _particleSystems[particleIndex].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        _particleSystems[particleIndex].Play();
                    }
                }
            }

            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                Renderer currentRenderer = _targetRenderers[rendererIndex];
                if (!currentRenderer)
                {
                    continue;
                }

                Material runtimeMaterial = currentRenderer.material;
                if (!runtimeMaterial)
                {
                    continue;
                }

                TintEmissiveMaterialProperty(runtimeMaterial, s_emissionColor, _originalEmissionColors[rendererIndex], targetColor, emissionMultiplier);
                if (_isEmissionOnlyRenderer[rendererIndex])
                {
                    continue;
                }

                TintStandardMaterialProperty(runtimeMaterial, s_color, _originalAlbedoColors[rendererIndex], targetColor, alphaMultiplier);
                TintStandardMaterialProperty(runtimeMaterial, s_tintColor, _originalTintColors[rendererIndex], targetColor, alphaMultiplier);
            }
        }

        public void RestoreOriginalColors()
        {
            for (int lightIndex = 0; lightIndex < _lights.Length; lightIndex++)
            {
                if (_lights[lightIndex] && lightIndex < _originalLightColors.Length && lightIndex < _originalLightIntensities.Length)
                {
                    _lights[lightIndex].color = _originalLightColors[lightIndex];
                    SetLightIntensity(_lights[lightIndex], _originalLightIntensities[lightIndex]);
                    SetLightRange(_lights[lightIndex], _originalLightRanges[lightIndex]);
                }
            }

            for (int particleIndex = 0; particleIndex < _particleSystems.Length; particleIndex++)
            {
                if (_particleSystems[particleIndex] && particleIndex < _originalParticleGradients.Length)
                {
                    ParticleSystem.MainModule mainModule = _particleSystems[particleIndex].main;
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
                if (_targetRenderers[rendererIndex] && rendererIndex < _originalSharedMaterials.Length)
                {
                    Material runtimeMaterial = _targetRenderers[rendererIndex].sharedMaterial;
                    if (runtimeMaterial && runtimeMaterial != _originalSharedMaterials[rendererIndex])
                    {
                        UnityEngine.Object.Destroy(runtimeMaterial);
                        _targetRenderers[rendererIndex].sharedMaterial = _originalSharedMaterials[rendererIndex];
                    }
                }
            }
        }

        // renderer.material copies dont get freed with their renderer
        public void CleanupRuntimeMaterials()
        {
            for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
            {
                if (_targetRenderers[rendererIndex] && rendererIndex < _originalSharedMaterials.Length)
                {
                    Material runtimeMaterial = _targetRenderers[rendererIndex].sharedMaterial;
                    if (runtimeMaterial && runtimeMaterial != _originalSharedMaterials[rendererIndex])
                    {
                        UnityEngine.Object.Destroy(runtimeMaterial, 10f);
                    }
                }
            }
        }

        // LightFlicker rewrites light.intensity every frame from m_baseIntensity so the base has to move with it
        // light.intensity still gets set too, a flicker whose Awake has not run yet copies it into m_baseIntensity
        public static void SetLightIntensity(Light light, float intensity)
        {
            light.intensity = intensity;
            if (light.TryGetComponent(out LightFlicker lightFlicker))
            {
                lightFlicker.m_baseIntensity = intensity;
            }
        }

        // LightLod ramps light.range back to m_baseRange whenever its once a second check finds the light in lod distance, so the base has to move with it
        // light.range still gets set too, a LightLod whose Awake has not run yet copies it into m_baseRange
        public static void SetLightRange(Light light, float range)
        {
            light.range = range;
            if (light.TryGetComponent(out LightLod lightLod))
            {
                lightLod.m_baseRange = range;
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

            GradientColorKey[] originalColorKeys = sourceGradient.colorKeys;
            GradientColorKey[] newColorKeys = new GradientColorKey[originalColorKeys.Length];
            for (int keyIndex = 0; keyIndex < originalColorKeys.Length; keyIndex++)
            {
                newColorKeys[keyIndex] = new GradientColorKey(new Color(targetColor.r, targetColor.g, targetColor.b), originalColorKeys[keyIndex].time);
            }

            GradientAlphaKey[] alphaKeys;
            if (alphaMultiplier < 0.999f)
            {
                GradientAlphaKey[] originalAlphaKeys = sourceGradient.alphaKeys;
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

            Gradient tintedGradient = new Gradient();
            tintedGradient.SetKeys(newColorKeys, alphaKeys);
            return tintedGradient;
        }

        public static string FormatColorHex(Color color, bool allowAlpha)
        {
            float alpha = allowAlpha ? Mathf.Clamp(color.a, MinimumAlphaFloor, 1.0f) : 1.0f;
            Color formattedColor = new Color(color.r, color.g, color.b, alpha);
            return allowAlpha
                ? ColorUtility.ToHtmlStringRGBA(formattedColor)
                : ColorUtility.ToHtmlStringRGB(formattedColor);
        }

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

        public static string NormalizeMaterialName(Material? material)
        {
            if (!material)
            {
                return string.Empty;
            }

            string materialName = material.name;
            int instanceIndex = materialName.IndexOf(" (Instance)", StringComparison.OrdinalIgnoreCase);
            return instanceIndex >= 0 ? materialName.Substring(0, instanceIndex) : materialName;
        }

        public static bool IsTargetMaterial(Material? material)
        {
            string baseName = NormalizeMaterialName(material);
            return !string.IsNullOrEmpty(baseName) && TargetedMaterialNames.Contains(baseName);
        }

        public static bool IsEmissionOnlyMaterial(Material? material)
        {
            string baseName = NormalizeMaterialName(material);
            return !string.IsNullOrEmpty(baseName) && EmissionOnlyMaterialNames.Contains(baseName);
        }
    }
}
