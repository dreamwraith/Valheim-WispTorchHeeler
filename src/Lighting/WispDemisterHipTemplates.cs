using System.IO;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // templates sit in the jotunn prefab container for the whole session and never get registered with ZNetScene
    public static class WispDemisterHipTemplates
    {
        private const string OrbTemplateName = "wisptorch_hip_orb";
        private const string CageTemplateName = "wisptorch_hip_cage";
        private const string DemisterBallPrefab = "demister_ball";
        private const string OrbModelChild = "demister_ball";
        private const string OrbEffectsChild = "effects";
        private const string LanternItemPrefab = "Lantern";
        private const string HolsterVisualChild = "attach_back";
        private const string LanternModelChild = "default";
        private const string CageMaterialName = "HipWispLantern_mat";
        private const string CageAlbedoResource = "HipWispLantern_d.png";
        private const string CageEmissionResource = "HipWispLantern_e.png";

        private static readonly int s_mainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int s_emissionMapId = Shader.PropertyToID("_EmissionMap");

        // meters along the m_backTool joint axes, Attach puts the template root on the joint at world scale 1
        // the lantern pivot is the bottom of the model, which rises up and toward the body from there
        private static readonly Vector3 s_cageOffset = new Vector3(-0.22f, -0.1f, 0.1f);

        // middle of the hanging lantern model so the orb sits where the cage body does
        private static readonly Vector3 s_orbOffset = new Vector3(-0.18f, -0.12f, 0.03f);

        private static readonly Quaternion s_cageRotation = Quaternion.Euler(306.55f, 215.5f, 117.64f);
        private const float CageScale = 0.25f;

        // the floating orb hovers 3.5 m up (Wisplight m_offset) so a hip light hits the ground at a flatter angle and looks dimmer
        // both donor lights are 1.5, this puts every hip light at 1.75 before the intensity slider
        private const float HipLightIntensityBoost = 1.75f / 1.5f;

        private static WispDemisterHipVisual? s_orbTemplate;
        private static WispDemisterHipVisual? s_cageTemplate;

        public static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += CreateTemplates;
        }

        public static WispDemisterHipVisual? GetTemplate(WornDemisterMount mount) => mount switch
        {
            WornDemisterMount.HipOrb => s_orbTemplate,
            WornDemisterMount.HipCage => s_cageTemplate,
            _ => null
        };

        private static void CreateTemplates()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= CreateTemplates;
            s_orbTemplate = CreateOrbTemplate();
            s_cageTemplate = CreateCageTemplate();
        }

        private static WispDemisterHipVisual? CreateOrbTemplate()
        {
            GameObject orbTemplate = PrefabManager.Instance.CreateClonedPrefab(OrbTemplateName, DemisterBallPrefab);
            if (!orbTemplate)
            {
                Plugin.Log.LogWarning($"[HipTemplates] Failed to clone '{DemisterBallPrefab}'. The HipOrb mount style is unavailable.");
                return null;
            }

            // the networked ball stays pinned at the hip and keeps clearing mist so this copy is only for looks
            DestroyComponents<Demister>(orbTemplate);
            DestroyComponents<ParticleSystemForceField>(orbTemplate);
            DestroyComponents<ZSyncTransform>(orbTemplate);
            DestroyComponents<ZNetView>(orbTemplate);

            Transform orbModel = orbTemplate.transform.Find(OrbModelChild);
            orbModel.localPosition = s_orbOffset;
            orbTemplate.transform.Find(OrbEffectsChild).localPosition = s_orbOffset;

            WispDemisterHipVisual orbVisual = FinishTemplate(orbTemplate, orbModel);
            Plugin.LogDebug($"[HipTemplates] Created hip orb template '{OrbTemplateName}'.");
            return orbVisual;
        }

        private static WispDemisterHipVisual? CreateCageTemplate()
        {
            GameObject lanternItem = PrefabManager.Instance.GetPrefab(LanternItemPrefab);
            Transform? holsterVisual = lanternItem ? lanternItem.transform.Find(HolsterVisualChild) : null;
            GameObject? cageTemplate = holsterVisual ? PrefabManager.Instance.CreateClonedPrefab(CageTemplateName, holsterVisual.gameObject) : null;
            if (!cageTemplate)
            {
                Plugin.Log.LogWarning($"[HipTemplates] Failed to clone '{LanternItemPrefab}/{HolsterVisualChild}'. The HipCage mount style is unavailable.");
                return null;
            }

            // base game emission map has orange baked into the window panes so a tint on top never hid it
            // the repainted panes are neutral, and the cage still only takes the demister color through emission
            foreach (MeshRenderer cageRenderer in cageTemplate.GetComponentsInChildren<MeshRenderer>(true))
            {
                cageRenderer.sharedMaterial = CreateCageMaterial(cageRenderer.sharedMaterial);
            }

            WispVisualApplicator.EmissionOnlyMaterialNames.Add(CageMaterialName);

            Transform lanternModel = cageTemplate.transform.Find(LanternModelChild);
            lanternModel.localPosition = s_cageOffset;
            lanternModel.localRotation = s_cageRotation;
            lanternModel.localScale = Vector3.one * CageScale;

            WispDemisterHipVisual cageVisual = FinishTemplate(cageTemplate, lanternModel);
            Plugin.LogDebug($"[HipTemplates] Created hip cage template '{CageTemplateName}'.");
            return cageVisual;
        }

        // a copy so every other renderer on the base game lantern material stays untouched
        private static Material CreateCageMaterial(Material donorMaterial)
        {
            Material cageMaterial = new Material(donorMaterial) { name = CageMaterialName };
            cageMaterial.SetTexture(s_mainTexId, LoadEmbeddedTexture(CageAlbedoResource, donorMaterial.GetTexture(s_mainTexId)));
            cageMaterial.SetTexture(s_emissionMapId, LoadEmbeddedTexture(CageEmissionResource, donorMaterial.GetTexture(s_emissionMapId)));
            return cageMaterial;
        }

        // sampler settings come from the base game texture being replaced, its albedo uses point filtering and a png loads as bilinear
        private static Texture2D LoadEmbeddedTexture(string resourceName, Texture donorTexture)
        {
            using Stream resourceStream = typeof(WispDemisterHipTemplates).Assembly.GetManifestResourceStream(resourceName);
            using MemoryStream imageBytes = new MemoryStream();
            resourceStream.CopyTo(imageBytes);

            Texture2D texture = AssetUtils.LoadImage(imageBytes.ToArray());
            texture.name = Path.GetFileNameWithoutExtension(resourceName);
            texture.filterMode = donorTexture.filterMode;
            texture.wrapMode = donorTexture.wrapMode;
            texture.anisoLevel = donorTexture.anisoLevel;
            return texture;
        }

        private static WispDemisterHipVisual FinishTemplate(GameObject template, Transform anchor)
        {
            // copies stay silent, the hidden ball already plays its sounds from the hip
            DestroyComponents<AudioSource>(template);

            // baked into the template so every copy and its restore baseline start boosted
            foreach (Light templateLight in template.GetComponentsInChildren<Light>(true))
            {
                templateLight.intensity *= HipLightIntensityBoost;
            }

            WispDemisterHipVisual hipVisual = template.AddComponent<WispDemisterHipVisual>();
            hipVisual.SetAnchor(anchor);
            return hipVisual;
        }

        // templates have no live copies yet so DestroyImmediate is fine, later setup in this frame never sees the removed parts
        private static void DestroyComponents<T>(GameObject root) where T : Component
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                Object.DestroyImmediate(component);
            }
        }
    }
}
