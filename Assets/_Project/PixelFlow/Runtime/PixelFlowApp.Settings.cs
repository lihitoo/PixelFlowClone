using System;
using Cysharp.Threading.Tasks;
using Dreamy.Feature.Settings.Integration;
using Dreamy.UI;
using UnityEngine;

namespace PixelFlow
{
    public sealed partial class PixelFlowApp
    {
        private SettingsPanel settingsPanel;
        private bool openingSettings, closingSettings;
        public bool IsSettingsReady => settingsPanel != null && !openingSettings && !closingSettings;
        private void PresentSettings()
        {
            settingsReturn = ScreenState;
            ScreenState = AppScreen.Settings;
            OpenSettingsAsync().Forget();
        }
        private async UniTask OpenSettingsAsync()
        {
            if (openingSettings) return;
            var token = lifetime.Token;
            openingSettings = true; view.PrepareSettings();
            try
            {
                var panel = await PanelManager.Instance.Create<SettingsPanel>(PixelFlowPanelAddress.Settings);
                token.ThrowIfCancellationRequested();
                await panel.Show();
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                Debug.LogWarning("Settings panel load failed: " + error.Message);
                if (!token.IsCancellationRequested)
                    view.ShowShopResult("Settings unavailable", "Unable to open Settings. Please try again.", CloseSettings, () => OpenSettingsAsync().Forget());
            }
            finally { openingSettings = false; }
        }
        public void BindSettings(SettingsPanel panel) { settingsPanel = panel; view.BindSettings(panel); }
        public void UnbindSettings(SettingsPanel panel) { if (settingsPanel == panel) { settingsPanel = null; view.BindSettings(null); } }
        public void CloseSettings() { if (!storeUiBusy && !openingSettings) CloseSettingsAsync().Forget(); }
        private async UniTask CloseSettingsAsync()
        {
            if (closingSettings) return;
            var token = lifetime.Token;
            closingSettings = true;
            try
            {
                if (settingsPanel != null) await settingsPanel.Hide();
                if (token.IsCancellationRequested) return;
                view.HideFoundationModal();
                ScreenState = settingsReturn; RestoreScreen();
            }
            finally { closingSettings = false; }
        }
        public void CommitSettings(Action change)
        {
            if (ScreenState != AppScreen.Settings || storeUiBusy || openingSettings || closingSettings) return;
            Guard(() => { change(); ApplyAudioSettings(); });
        }
        public void SettingsCommandRequested(SettingsCommand command)
        {
            if (ScreenState != AppScreen.Settings || storeUiBusy || openingSettings || closingSettings || settingsPanel == null) return;
            if (command == SettingsCommand.Restore) { RestoreShopAsync().Forget(); return; }
            if (command is SettingsCommand.Restart or SettingsCommand.GiveUp)
            {
                if (Session == null) return;
                bool restart = command == SettingsCommand.Restart;
                settingsPanel.ShowNotice(restart ? "Restart level?" : "Give up?", "Your current attempt will end.\nYou will lose a life unless unlimited lives are active.",
                    restart ? "Restart" : "Give Up", () => EndAttempt(restart).Forget());
                return;
            }
            string title, detail;
            switch (command)
            {
                case SettingsCommand.Privacy: title = "Privacy Settings"; detail = "Privacy services are unavailable right now.\nPlease try again later."; break;
                case SettingsCommand.SaveProgress: title = "Save Your Progress"; detail = "Your progress is saved on this device.\nAccount sign-in is unavailable right now."; break;
                case SettingsCommand.Feedback: title = "Send Feedback"; detail = "Support is unavailable right now.\nPlease try again later."; break;
                case SettingsCommand.Instagram: title = "Instagram"; detail = "This link is unavailable right now.\nPlease try again later."; break;
                default: title = "Delete Account"; detail = "No account is connected.\nYour local progress has been kept."; break;
            }
            settingsPanel.ShowNotice(title, detail, "OK", null);
        }
        private async UniTask EndAttempt(bool restart)
        {
            if (closingSettings || busy) return;
            var token = lifetime.Token;
            busy = true;
            try { progress.AbandonRun(ProgressStore.UtcSeconds); }
            catch (Exception error) { SaveError(error, () => EndAttempt(restart).Forget()); return; }
            finally { busy = false; }
            closingSettings = true;
            try
            {
                if (settingsPanel != null) await settingsPanel.Hide();
                if (token.IsCancellationRequested) return;
                ClearSession(); ScreenState = AppScreen.Home;
                if (restart) StartLevel(SelectedLevel); else Home();
            }
            finally { closingSettings = false; }
        }
    }
}
