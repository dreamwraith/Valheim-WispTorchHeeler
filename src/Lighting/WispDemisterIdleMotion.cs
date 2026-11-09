using System.Collections.Generic;
using System.Linq;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // scales the follower orb hover from the SE_Demister asset values, once at Setup and again on a setting change, never per tick
    public static class WispDemisterIdleMotion
    {
        private record struct IdleMotion(float NoiseSpeed, float RotationSpeed, float NoiseDistance, float NoiseDistanceInterior);

        private static readonly Dictionary<int, IdleMotion> s_baseMotion = new();

        public static void Initialize()
        {
            ModConfig.EnableMod.SettingChanged += (sender, args) => ApplyToLocalPlayer();
            ModConfig.EnableDemisterCustomization.SettingChanged += (sender, args) => ApplyToLocalPlayer();
            ModConfig.DemisterIdleSpeedMultiplier.SettingChanged += (sender, args) => ApplyToLocalPlayer();
            ModConfig.DemisterIdleRadiusMultiplier.SettingChanged += (sender, args) => ApplyToLocalPlayer();
        }

        public static void OnSetup(SE_Demister demisterEffect)
        {
            int nameHash = demisterEffect.NameHash();
            if (!s_baseMotion.TryGetValue(nameHash, out IdleMotion baseMotion))
            {
                // SEMan clones the asset right before Setup so the clone still holds the asset values
                baseMotion = new IdleMotion(demisterEffect.m_noiseSpeed, demisterEffect.m_rotationSpeed,
                    demisterEffect.m_noiseDistance, demisterEffect.m_noiseDistanceInterior);
                s_baseMotion[nameHash] = baseMotion;
            }

            Apply(demisterEffect, baseMotion);
        }

        // only the owner SE_Demister moves the ball, so local player effects are the only ones worth updating
        private static void ApplyToLocalPlayer()
        {
            if (!Player.m_localPlayer)
            {
                return;
            }

            foreach (SE_Demister demisterEffect in Player.m_localPlayer.m_seman.m_statusEffects.OfType<SE_Demister>())
            {
                if (s_baseMotion.TryGetValue(demisterEffect.NameHash(), out IdleMotion baseMotion))
                {
                    Apply(demisterEffect, baseMotion);
                }
            }
        }

        private static void Apply(SE_Demister demisterEffect, IdleMotion baseMotion)
        {
            bool isCustomized = ModConfig.EnableMod.Value && ModConfig.EnableDemisterCustomization.Value;
            float speedMultiplier = isCustomized ? ModConfig.DemisterIdleSpeedMultiplier.Value : 1f;
            float radiusMultiplier = isCustomized ? ModConfig.DemisterIdleRadiusMultiplier.Value : 1f;

            demisterEffect.m_noiseSpeed = baseMotion.NoiseSpeed * speedMultiplier;
            demisterEffect.m_rotationSpeed = baseMotion.RotationSpeed * speedMultiplier;
            demisterEffect.m_noiseDistance = baseMotion.NoiseDistance * radiusMultiplier;
            demisterEffect.m_noiseDistanceInterior = baseMotion.NoiseDistanceInterior * radiusMultiplier;
        }
    }
}
