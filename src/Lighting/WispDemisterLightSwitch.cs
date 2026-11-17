using System.Linq;
using WispTorchHeeler.Configuration;

namespace WispTorchHeeler.Lighting
{
    // the off state lives on the item so it survives unequip and relog. the ball zdo only carries it to other clients,
    // the game destroys the ball on unequip
    public static class WispDemisterLightSwitch
    {
        private const string LightOffDataKey = "wisptorch_demister_light_off";

        public static void Toggle()
        {
            if (!ModConfig.EnableDemisterCustomization.Value)
            {
                return;
            }

            ItemDrop.ItemData? wisplight = FindEquippedWisplight(Player.m_localPlayer);
            if (wisplight == null)
            {
                return;
            }

            if (!wisplight.m_customData.Remove(LightOffDataKey))
            {
                wisplight.m_customData[LightOffDataKey] = "1";
            }

            WispDemisterColorController.SyncLocalPlayerDemisterState();
            WispDemisterColorController.RefreshAll();
        }

        public static bool IsLocalPlayerLightOff()
        {
            // setting changes sync the demister state too and those can fire in the main menu
            if (!Player.m_localPlayer)
            {
                return false;
            }

            ItemDrop.ItemData? wisplight = FindEquippedWisplight(Player.m_localPlayer);
            return wisplight != null && wisplight.m_customData.ContainsKey(LightOffDataKey);
        }

        // by status effect, not slot or prefab name, so a Wisplight in a modded equipment slot still counts
        private static ItemDrop.ItemData? FindEquippedWisplight(Player player) =>
            player.GetInventory().GetEquippedItems().FirstOrDefault(item => item.m_shared.m_equipStatusEffect is SE_Demister);
    }
}
