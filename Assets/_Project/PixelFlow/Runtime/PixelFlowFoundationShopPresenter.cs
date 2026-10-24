using System;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Shop;
using Dreamy.UI;

namespace PixelFlow
{
    /// <summary>Game header/restore intents around the existing package ShopPresenter.</summary>
    public sealed class PixelFlowFoundationShopPresenter : IPanelPresenter
    {
        private readonly ShopPresenter purchases;
        private readonly ShopPanel panel;
        private readonly Dreamy.Feature.Shop.Integration.ShopPanel view;
        private readonly PixelFlowApp app;
        private bool bound;
        public PixelFlowFoundationShopPresenter(IShopService service, ShopPanel panel, PixelFlowApp app)
        {
            this.panel = panel; this.app = app;
            view = panel;
            purchases = new ShopPresenter(service, panel, panel.PresentPurchaseAsync);
        }
        public void Show()
        {
            if (!bound)
            {
                bound = true;
                view.PurchaseBusyChanged += app.FoundationShopBusy;
                view.PurchaseResult += app.FoundationShopResult;
                view.RestoreRequested += app.FoundationShopRestore;
                view.SettingsRequested += app.FoundationShopSettings;
                view.LivesRequested += app.FoundationShopLives;
                view.RankRequested += app.FoundationShopRank;
                view.HeaderRefreshRequested += app.RefreshFoundationShopHeader;
                panel.OnPreHide += Closing;
                app.BindFoundationShop(view);
            }
            purchases.Show();
        }
        private void Closing() { panel.SetInteractable(false); app.FoundationShopClosing(); }
        public void Dispose()
        {
            purchases.Dispose(); if (!bound) return; bound = false;
            view.PurchaseBusyChanged -= app.FoundationShopBusy;
            view.PurchaseResult -= app.FoundationShopResult;
            view.RestoreRequested -= app.FoundationShopRestore;
            view.SettingsRequested -= app.FoundationShopSettings;
            view.LivesRequested -= app.FoundationShopLives;
            view.RankRequested -= app.FoundationShopRank;
            view.HeaderRefreshRequested -= app.RefreshFoundationShopHeader;
            panel.OnPreHide -= Closing;
        }
    }
}
