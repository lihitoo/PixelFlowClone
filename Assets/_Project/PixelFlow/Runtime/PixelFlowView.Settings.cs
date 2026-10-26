using Dreamy.Feature.Settings.Integration;

namespace PixelFlow
{
    public sealed partial class PixelFlowView
    {
        private SettingsPanel settingsView;
        public SettingsPanel Settings => settingsView;
        public void BindSettings(SettingsPanel panel) => settingsView = panel;
        public void PrepareSettings() { HidePages(); gameplay.HideResult(); }
    }
}
