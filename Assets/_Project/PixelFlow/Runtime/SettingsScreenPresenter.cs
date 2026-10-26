using Dreamy.Feature.Settings.Integration;
using Dreamy.Settings;
using Dreamy.UI;

namespace PixelFlow
{
    /// <summary>Projects the existing progress owner into the source-specific Settings controls.</summary>
    public sealed class SettingsScreenPresenter : IPanelPresenter
    {
        private readonly ProgressStore progress;
        private readonly SettingsPanel panel;
        private readonly PixelFlowApp app;
        private bool bound;
        public SettingsScreenPresenter(ProgressStore progress, SettingsPanel panel, PixelFlowApp app)
        { this.progress = progress; this.panel = panel; this.app = app; }
        public void Show()
        {
            if (bound) return;
            bound = true;
            panel.MusicVolumeChanged += Music;
            panel.SfxVolumeChanged += Sound;
            panel.HapticsEnabledChanged += Haptics;
            panel.NotificationsEnabledChanged += Notifications;
            panel.SmoothMotionChanged += SmoothMotion;
            panel.CommandRequested += app.SettingsCommandRequested;
            panel.CloseRequested += app.CloseSettings;
            progress.Changed += Render;
            app.BindSettings(panel); Render();
        }
        private void Render()
        {
            var p = progress.Data;
            panel.Render(new SettingsViewState(p.MusicEnabled ? p.MusicVolume : 0, p.SoundEnabled ? p.SoundVolume : 0, false, true, false, p.HapticsEnabled, true));
            panel.RenderContext(p.NotificationsEnabled, p.SmoothMotion, app.Session != null, "Version " + UnityEngine.Application.version, "User ID: Not connected");
        }
        private void Change(System.Action change) { app.CommitSettings(change); Render(); }
        private void Music(float value) => Change(() => progress.SetAudioLevels(value, progress.Data.SoundEnabled ? progress.Data.SoundVolume : 0));
        private void Sound(float value) => Change(() => progress.SetAudioLevels(progress.Data.MusicEnabled ? progress.Data.MusicVolume : 0, value));
        private void Haptics(bool value) => Change(() => progress.SetHomePreferences(value, progress.Data.NotificationsEnabled, progress.Data.SmoothMotion));
        private void Notifications(bool value) => Change(() => progress.SetHomePreferences(progress.Data.HapticsEnabled, value, progress.Data.SmoothMotion));
        private void SmoothMotion(bool value) => Change(() => progress.SetHomePreferences(progress.Data.HapticsEnabled, progress.Data.NotificationsEnabled, value));
        public void Dispose()
        {
            if (!bound) return; bound = false;
            panel.MusicVolumeChanged -= Music;
            panel.SfxVolumeChanged -= Sound;
            panel.HapticsEnabledChanged -= Haptics;
            panel.NotificationsEnabledChanged -= Notifications;
            panel.SmoothMotionChanged -= SmoothMotion;
            panel.CommandRequested -= app.SettingsCommandRequested;
            panel.CloseRequested -= app.CloseSettings;
            progress.Changed -= Render;
            app.UnbindSettings(panel);
        }
    }
}
