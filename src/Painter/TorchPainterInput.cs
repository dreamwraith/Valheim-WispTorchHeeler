using UnityEngine;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Painter
{
    // added to the plugin object on clients, after the painter buttons are registered
    public class TorchPainterInput : MonoBehaviour
    {
        private void Update()
        {
            if (!ModConfig.EnableMod.Value || !Player.m_localPlayer)
            {
                return;
            }

            if (ModConfig.ButtonResetConfig != null && ZInput.GetButtonDown(ModConfig.ButtonResetConfig.Name))
            {
                TorchPainter.TryPaintOrReset(isReset: true);
            }
            else if (ModConfig.ButtonPaintConfig != null && ZInput.GetButtonDown(ModConfig.ButtonPaintConfig.Name))
            {
                TorchPainter.TryPaintOrReset(isReset: false);
            }
        }
    }
}
