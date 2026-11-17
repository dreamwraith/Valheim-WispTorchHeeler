using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // a base game style heat area on the ball, like the FireWarmth child on a fire. it leaves out the Fire flag so monsters dont avoid the wearer
    // and is player only, Player is the only character that does anything in OnNearFire
    // the hip styles pin the ball to the hip, so the heat comes from there
    public class WispDemisterHeat
    {
        private const string AreaName = "wisptorch_demister_heat";
        private const string AreaLayerName = "character_trigger";
        private const float AreaRadius = 3.0f;

        private Transform _ballRoot = null!;
        private GameObject? _area;

        public void Initialize(Transform ballRoot) => _ballRoot = ballRoot;

        // removed instead of switched off. EffectArea only forgets a player on trigger exit,
        // and unity doesnt document one when the trigger itself switches off
        public void SetHeating(bool isHeating)
        {
            if (isHeating && !_area)
            {
                _area = CreateArea(_ballRoot);
            }
            else if (!isHeating && _area)
            {
                Object.Destroy(_area);
                _area = null;
            }
        }

        private static GameObject CreateArea(Transform ballRoot)
        {
            // built inactive so EffectArea.Awake already sees the collider and the heat type
            GameObject area = new GameObject(AreaName);
            area.SetActive(false);

            // EffectArea.IsPointInsideArea only searches this layer, thats how beds and eggs find heat
            area.layer = LayerMask.NameToLayer(AreaLayerName);
            area.transform.SetParent(ballRoot, false);

            // the ball moves by transform every frame and unity says a static collider moved that way can fail to wake a rigidbody
            area.AddComponent<Rigidbody>().isKinematic = true;
            area.AddComponent<SphereCollider>().radius = AreaRadius;
            EffectArea effectArea = area.AddComponent<EffectArea>();
            effectArea.m_type = EffectArea.Type.Heat;
            effectArea.m_playerOnly = true;

            area.SetActive(true);
            return area;
        }

        // owner side. the hip copy hangs off the local player, so m_localPlayer is alive whenever isLitOnHip is true
        public static bool CanLocalPlayerHeat(bool isLitOnHip) =>
            isLitOnHip
            && ModConfig.EnableDemisterHeat.Value
            && (!ModConfig.DemisterHeatBlockInColdBiomes.Value || !IsBlockedByCold(Player.m_localPlayer));

        // heat cancels Cold, so it goes off in the cold biomes unless the wearer is covered or already frost resistant
        private static bool IsBlockedByCold(Player wearer)
        {
            Heightmap.Biome biome = wearer.GetCurrentBiome();
            if (biome != Heightmap.Biome.Mountain && biome != Heightmap.Biome.DeepNorth)
            {
                return false;
            }

            if (wearer.InInterior() || wearer.InShelter())
            {
                return false;
            }

            // same three modifiers Player.UpdateEnvStatusEffects checks before it gives Cold
            HitData.DamageModifier frostModifier = wearer.GetDamageModifiers().GetModifier(HitData.DamageType.Frost);
            return frostModifier is not (HitData.DamageModifier.Resistant or HitData.DamageModifier.VeryResistant or HitData.DamageModifier.SlightlyResistant);
        }
    }
}
