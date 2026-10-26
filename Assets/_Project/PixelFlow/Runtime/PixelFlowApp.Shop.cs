using System;
using Cysharp.Threading.Tasks;
using Dreamy.Shop;
using Dreamy.UI;
using Dreamy.Feature.Shop.Integration;
using PixelFlow.Rules;
using UnityEngine;

namespace PixelFlow
{
    public sealed partial class PixelFlowApp
    {
        private Action activeShopClose;
        private bool activeShopMini, storeUiBusy;
        private bool openingShop, closingShop;
        public bool IsShopReady => nativeShopView != null && !openingShop && !closingShop;
        public bool IsShopTransitioning => openingShop || closingShop;
        private string shopFocus;
        private Dreamy.Feature.Shop.Integration.ShopPanel nativeShopView;
        private AppScreen homePopupReturn = AppScreen.Home;
        private BoosterKind boosterBuyKind;
        public PixelFlowShopService Shop { get; private set; }
        private void InitializeShop(PixelFlowInstallerProfile profile)
        {
            Shop = profile.Shop;
        }
        private void PresentShop(Action close, bool mini, string section = null)
        {
            activeShopClose = close; activeShopMini = mini; ScreenState = AppScreen.Shop;
            shopFocus = section; OpenFoundationShopAsync().Forget();
        }
        private async UniTask OpenFoundationShopAsync()
        {
            if (openingShop) return; openingShop = true;
            view.HideForFoundationPanel();
            try
            {
                var panel = await PanelManager.Instance.Create<ShopPanel>(PixelFlowPanelAddress.Shop);
                if (lifetime.IsCancellationRequested) return;
                await panel.Show();
                if (Shop.Gateway.HasDeferred) view.ShowShopResult("Purchase pending", "Waiting for purchase approval.", RestoreFoundationShop);
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                Debug.LogWarning("Shop panel load failed: " + error.Message);
                if (!lifetime.IsCancellationRequested) view.ShowShopResult("Shop unavailable", "Unable to open the Shop. Please try again.", activeShopClose,
                    () => OpenFoundationShopAsync().Forget());
            }
            finally { openingShop = false; }
        }
        public void BindFoundationShop(Dreamy.Feature.Shop.Integration.ShopPanel nativeView)
        {
            nativeShopView = nativeView; nativeView.Bind(foundation.Wallet, activeShopMini, shopFocus); shopFocus = null;
            view.BindFoundationShop(nativeView); RefreshFoundationShopHeader();
        }
        public void RefreshFoundationShopHeader()
        {
            if (nativeShopView == null) return;
            var tuning = Campaign.Home; long now = ProgressStore.UtcSeconds;
            var lives = HomeLifeRules.Advance(progress.Data.Lives, now, tuning.MaximumHearts, tuning.HeartIntervalSeconds);
            bool unlimited = lives.UnlimitedUntilUtc > now;
            long seconds = unlimited ? lives.UnlimitedUntilUtc - now : HomeLifeRules.SecondsToNext(lives, now, tuning.HeartIntervalSeconds);
            nativeShopView.RenderHeader(lives.Hearts, unlimited, seconds, lives.Hearts == tuning.MaximumHearts, progress.Data.UnlockedLevel + 1 >= tuning.LeaderboardLevel);
        }
        public void FoundationShopBusy(bool busy)
        { storeUiBusy = busy; if (busy) view.ShowShopBusy("Processing purchase..."); }
        public void FoundationShopResult(ShopPurchaseResult result)
        {
            if (result.IsSuccess)
            {
                var offer = Shop.Catalog.Find(result.Offer.Id);
                string reward = offer.Coins > 0 ? offer.Coins.ToString("N0") + " coins\n" : "";
                if (offer.Boosters > 0) reward += "Each booster x" + offer.Boosters + "\n";
                if (offer.UnlimitedSeconds > 0) reward += offer.UnlimitedSeconds / 3600 + "h unlimited lives\n";
                if (offer.NoAds) reward += "No Ads activated";
                view.ShowShopResult("Purchase successful!", reward, RestoreFoundationShop); Play(presentation.Tap);
            }
            else if (result.Status == ShopPurchaseStatus.PurchaseCancelled)
                view.ShowShopResult("Purchase cancelled", "Nothing was added to your inventory.", RestoreFoundationShop);
            else
            {
                string id = result.Offer?.Id;
                view.ShowShopResult(result.Status == ShopPurchaseStatus.RewardGrantFailed ? "Purchase not delivered" : "Purchase failed",
                    "Please try again to complete your purchase.", RestoreFoundationShop, () => nativeShopView?.RetryPurchase(id));
            }
        }
        public void FoundationShopSettings() => OpenPage(FlowPage.Settings);
        public void FoundationShopRank() => OpenPage(FlowPage.Rank);
        public void FoundationShopLives() => OpenPage(FlowPage.Lives);
        public void FoundationShopClosing() => ReturnAfterShopHide().Forget();
        private async UniTask ReturnAfterShopHide()
        {
            var panel = nativeShopView; var close = activeShopClose;
            storeUiBusy = true; closingShop = true;
            try
            {
                await UniTask.WaitUntil(() => panel == null || !panel.gameObject.activeSelf, cancellationToken: lifetime.Token);
                nativeShopView = null; view.BindFoundationShop(null); storeUiBusy = false;
                close?.Invoke();
            }
            catch (OperationCanceledException) { }
            finally { closingShop = false; }
        }
        private void RestoreFoundationShop()
        {
            view.HideFoundationModal(); ScreenState = AppScreen.Shop;
            if (nativeShopView != null) { PanelManager.Instance.Get<ShopPanel>().Render(foundation.FoundationShop.GetState()); RefreshFoundationShopHeader(); }
            else OpenFoundationShopAsync().Forget();
        }
        private async UniTaskVoid RestoreShopAsync()
        {
            if (storeUiBusy || Shop.Busy) return;
            var origin = ScreenState; var token = lifetime.Token; storeUiBusy = true;
            Action close = () => { ScreenState = origin; RestoreScreen(); };
            view.ShowShopBusy("Restoring purchases...");
            try
            {
                bool restored = await Shop.RestoreAsync(token); if (token.IsCancellationRequested) return;
                view.UpdateHomeProfile(progress.Data);
                view.ShowShopResult(restored ? "Purchases restored" : "No purchases found", restored ? "No Ads is active.\nConsumable rewards are not granted again." : "There are no purchases to restore.", close);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error) { Debug.LogWarning("Shop restore failed: " + error.Message); if (!token.IsCancellationRequested) view.ShowShopResult("Restore failed", "Unable to restore purchases. Please try again.", close, () => RestoreShopAsync().Forget()); }
            finally { storeUiBusy = false; }
        }
        private void ShowBoosterPurchase()
        { view.ShowGame(SelectedLevel); view.ShowBoosterPurchase(progress, boosterBuyKind, () => { ScreenState = AppScreen.Playing; RestoreScreen(); }); }
        private void OpenBoosterMiniShop()
        {
            if (ScreenState != AppScreen.BoosterPurchase) return;
            PresentShop(() => { ScreenState = AppScreen.BoosterPurchase; ShowBoosterPurchase(); }, true, "Gold");
        }
        private void BuyBoosterFromPopup(BoosterKind kind)
        {
            if (busy) return;
            bool purchased; busy = true;
            try { purchased = progress.Buy(kind); }
            catch (Exception error) { SaveError(error, () => BuyBoosterFromPopup(kind)); return; }
            finally { busy = false; }
            if (!purchased) { ShowBoosterPurchase(); return; }
            ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); UseBooster(kind);
        }
    }
}
