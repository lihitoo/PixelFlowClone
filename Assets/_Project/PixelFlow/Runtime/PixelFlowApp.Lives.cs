using System;
using Cysharp.Threading.Tasks;
using Dreamy.UI;
using UnityEngine;

namespace PixelFlow
{
    public sealed partial class PixelFlowApp
    {
        private LivesPanel livesPanel;
        private bool openingLives, closingLives;
        public bool IsLivesReady => livesPanel != null && !openingLives && !closingLives;
        public bool IsLivesTransitioning => openingLives || closingLives;
        private async UniTask OpenLivesAsync()
        {
            if (openingLives) return;
            openingLives = true; var token = lifetime.Token;
            try
            {
                var panel = await PanelManager.Instance.Create<LivesPanel>(PixelFlowPanelAddress.Lives);
                token.ThrowIfCancellationRequested(); await panel.Show();
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                Debug.LogWarning("Lives panel load failed: " + error.Message);
                if (!token.IsCancellationRequested) view.ShowShopResult("Lives unavailable", "Please try again.", CloseLives, () => OpenLivesAsync().Forget());
            }
            finally { openingLives = false; }
        }
        public void BindLives(LivesPanel panel) { livesPanel = panel; view.BindLives(panel); }
        public void UnbindLives(LivesPanel panel) { if (livesPanel == panel) { livesPanel = null; view.BindLives(null); } }
        public void CloseLives() { if (!openingLives && !storeUiBusy) LeaveLives(false).Forget(); }
        public void LivesShop() { if (!openingLives && !storeUiBusy) LeaveLives(true).Forget(); }
        private async UniTask LeaveLives(bool shop)
        {
            if (closingLives) return;
            closingLives = true; var token = lifetime.Token;
            try
            {
                if (livesPanel != null) await livesPanel.Hide();
                if (token.IsCancellationRequested) return;
                view.HideFoundationModal();
                if (shop) PresentShop(() => { ScreenState = AppScreen.HomePopup; ShowHomePopup(); }, true, "Gold");
                else { ScreenState = homePopupReturn; RestoreScreen(); }
            }
            finally { closingLives = false; }
        }
    }
}
