using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // added to the plugin object on clients, after the demister light button is registered
    public class WispDemisterLightInput : MonoBehaviour
    {
        private void Update()
        {
            // jotunn lets the button through with no local player, so the main menu has to be ruled out here
            if (!ModConfig.EnableMod.Value || !Player.m_localPlayer)
            {
                return;
            }

            if (ModConfig.ButtonToggleDemisterLightConfig != null && ZInput.GetButtonDown(ModConfig.ButtonToggleDemisterLightConfig.Name))
            {
                WispDemisterLightSwitch.Toggle();
            }
        }
    }
}
