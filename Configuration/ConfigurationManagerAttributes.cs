using System;
using BepInEx.Configuration;

namespace WispTorchHeeler.Configuration
{
    /// <summary>
    /// Standard BepInEx ConfigurationManager attribute descriptor.
    /// Discovered reflectively by BepInEx ConfigurationManager via <see cref="ConfigDescription.Tags"/>.
    /// </summary>
    public class ConfigurationManagerAttributes
    {
        /// <summary>
        /// Custom IMGUI drawer action invoked by ConfigurationManager to draw the setting control.
        /// </summary>
        public Action<ConfigEntryBase>? CustomDrawer;

        /// <summary>
        /// If true, the setting is hidden from the ConfigurationManager window.
        /// </summary>
        public bool? Browsable;

        /// <summary>
        /// If true, the setting cannot be edited in the GUI.
        /// </summary>
        public bool? ReadOnly;

        /// <summary>
        /// Category override for the setting.
        /// </summary>
        public string? Category;

        /// <summary>
        /// Display order within the category.
        /// </summary>
        public int? Order;
    }
}
