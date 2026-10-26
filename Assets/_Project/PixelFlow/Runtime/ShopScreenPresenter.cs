using System;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Shop;
using Dreamy.UI;

namespace PixelFlow
{
    /// <summary>Game header intents around the existing package ShopPresenter.</summary>
    public sealed class ShopScreenPresenter : IPanelPresenter
    {
        private readonly ShopPresenter purchases;
        private readonly ShopPanel panel;
        private readonly PixelFlowApp app;
        private bool bound;
        public ShopScreenPresenter(IShopService service, ShopPanel panel, PixelFlowApp app)
        {
            this.panel = panel; this.app = app;
            purchases = new ShopPresenter(service, panel, panel.PresentPurchaseAsync);
        }
        public void Show()
        {
            if (!bound)
            {
                bound = true;
                panel.PurchaseBusyChanged += app.FoundationShopBusy;
                panel.PurchaseResult += app.FoundationShopResult;
                panel.SettingsRequested += app.FoundationShopSettings;
                panel.LivesRequested += app.FoundationShopLives;
                panel.RankRequested += app.FoundationShopRank;
                panel.HeaderRefreshRequested += app.RefreshFoundationShopHeader;
                panel.OnPreHide += Closing;
                app.BindFoundationShop(panel);
            }
            purchases.Show();
        }
        private void Closing() { panel.SetInteractable(false); app.FoundationShopClosing(); }
        public void Dispose()
        {
            purchases.Dispose(); if (!bound) return; bound = false;
            panel.PurchaseBusyChanged -= app.FoundationShopBusy;
            panel.PurchaseResult -= app.FoundationShopResult;
            panel.SettingsRequested -= app.FoundationShopSettings;
            panel.LivesRequested -= app.FoundationShopLives;
            panel.RankRequested -= app.FoundationShopRank;
            panel.HeaderRefreshRequested -= app.RefreshFoundationShopHeader;
            panel.OnPreHide -= Closing;
        }
    }
}
