using Dreamy.UI;
using PixelFlow.Rules;

namespace PixelFlow
{
    public sealed class LivesPresenter : IPanelPresenter
    {
        private readonly LivesPanel panel;
        private readonly ProgressStore progress;
        private readonly HomeTuning tuning;
        private readonly PixelFlowApp app;
        private bool bound;
        public LivesPresenter(LivesPanel panel, ProgressStore progress, HomeTuning tuning, PixelFlowApp app)
        { this.panel = panel; this.progress = progress; this.tuning = tuning; this.app = app; }
        public void Show()
        {
            if (bound) return; bound = true;
            panel.CloseRequested += app.CloseLives; panel.RefillRequested += app.RefillLives; panel.CoinsRequested += app.LivesShop;
            panel.RefreshRequested += Render; progress.Changed += Render;
            app.BindLives(panel); Render();
        }
        private void Render()
        {
            long now = ProgressStore.UtcSeconds;
            var lives = HomeLifeRules.Advance(progress.Data.Lives, now, tuning.MaximumHearts, tuning.HeartIntervalSeconds);
            bool unlimited = lives.UnlimitedUntilUtc > now;
            long seconds = unlimited ? lives.UnlimitedUntilUtc - now : HomeLifeRules.SecondsToNext(lives, now, tuning.HeartIntervalSeconds);
            panel.Render(lives.Hearts, tuning.MaximumHearts, seconds, unlimited, progress.Data.Coins >= tuning.HeartRefillCoins, tuning.HeartRefillCoins);
        }
        public void Dispose()
        {
            if (!bound) return; bound = false;
            panel.CloseRequested -= app.CloseLives; panel.RefillRequested -= app.RefillLives; panel.CoinsRequested -= app.LivesShop;
            panel.RefreshRequested -= Render; progress.Changed -= Render; app.UnbindLives(panel);
        }
    }
}
