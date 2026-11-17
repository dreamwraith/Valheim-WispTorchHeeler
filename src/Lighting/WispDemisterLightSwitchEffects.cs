using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Audio;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // plays a base game candle effect when a wisplight light flips, the same one for off and on. nothing goes over the network for it,
    // every client plays its own copy when it sees the light state on the ball zdo change
    public static class WispDemisterLightSwitchEffects
    {
        private const string TemplateName = "wisptorch_light_switch_fx";
        private const string DonorPrefab = "fx_candle_addfuel";
        private const string FlameChild = "pfx";
        private const string SfxMixerGroupName = "SFX";
        private const float SoundVolume = 0.5f;

        private static readonly ParticleSystemCustomData[] s_flameColorStreams = { ParticleSystemCustomData.Custom1, ParticleSystemCustomData.Custom2 };

        private static GameObject? s_template;

        public static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += CreateTemplate;
        }

        private static void CreateTemplate()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= CreateTemplate;
            GameObject template = PrefabManager.Instance.CreateClonedPrefab(TemplateName, DonorPrefab);
            if (!template)
            {
                Plugin.Log.LogWarning($"[LightSwitchEffects] Failed to clone '{DonorPrefab}'. The light switch effect is unavailable.");
                return;
            }

            // local copies only. with no ZNetView the TimedDestruction on the donor falls back to a plain destroy
            Object.DestroyImmediate(template.GetComponent<ZNetView>());

            // the donor keeps its flame 0.308 m above the root, zeroed so the flame sits on the ball
            template.transform.Find(FlameChild).localPosition = Vector3.zero;
            s_template = template;
        }

        public static void Play(Vector3 position, Color wispColor)
        {
            if (!ModConfig.DemisterLightSwitchEffects.Value || !s_template)
            {
                return;
            }

            // the donor sound ships with no mixer group, so it would ignore the game sfx volume
            AudioMixerGroup[] mixerGroups = AudioMan.instance.m_masterMixer.FindMatchingGroups(string.Empty);
            AudioMixerGroup sfxGroup = mixerGroups.First(group => group.name.Equals(SfxMixerGroupName, System.StringComparison.Ordinal));
            GameObject effect = Object.Instantiate(s_template, position, Quaternion.identity);
            ZSFX sound = effect.GetComponentInChildren<ZSFX>();
            sound.m_audioSource.outputAudioMixerGroup = sfxGroup;
            sound.SetVolumeModifier(SoundVolume);

            // the child particle system is white with no texture and never streams its custom data, so only the flame takes a color
            RecolorFlame(effect.transform.Find(FlameChild).GetComponent<ParticleSystem>(), wispColor);
        }

        // the gradient mapped shader blends the grayscale flame texture between the Custom1 and Custom2 colors, those carry all the orange
        private static void RecolorFlame(ParticleSystem flame, Color wispColor)
        {
            ParticleSystem.CustomDataModule customData = flame.customData;
            foreach (ParticleSystemCustomData stream in s_flameColorStreams)
            {
                customData.SetColor(stream, RecolorGradient(customData.GetColor(stream).gradient, wispColor));
            }
        }

        // keeps the brightness of each key so the over bright core and the fade over the particle life stay
        private static Gradient RecolorGradient(Gradient sourceGradient, Color wispColor)
        {
            GradientColorKey[] colorKeys = sourceGradient.colorKeys;
            for (int keyIndex = 0; keyIndex < colorKeys.Length; keyIndex++)
            {
                float intensity = colorKeys[keyIndex].color.maxColorComponent;
                colorKeys[keyIndex].color = new Color(wispColor.r * intensity, wispColor.g * intensity, wispColor.b * intensity);
            }

            Gradient recoloredGradient = new Gradient();
            recoloredGradient.SetKeys(colorKeys, sourceGradient.alphaKeys);
            return recoloredGradient;
        }
    }
}
