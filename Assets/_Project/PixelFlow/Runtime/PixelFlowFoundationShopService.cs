using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Shop;
using Newtonsoft.Json;

namespace PixelFlow
{
    /// <summary>Existing Dreamy presenter/view protocol over atomic Pixel Flow delivery.</summary>
    public sealed class PixelFlowFoundationShopService : IShopService
    {
        private readonly PixelFlowShopService service;
        private readonly ProgressStore progress;
        private readonly HomeTuning home;
        private readonly CancellationToken lifetime;
        private readonly ShopCatalogConfig catalog;
        public ShopCatalogConfig Catalog => catalog;
        public PixelFlowFoundationShopService(PixelFlowShopService service, ProgressStore progress, HomeTuning home, CancellationToken lifetime)
        {
            this.service = service; this.progress = progress; this.home = home; this.lifetime = lifetime;
            var offers = service.Catalog.Offers.Select(offer =>
            {
                var rewards = new List<object>();
                if (offer.Coins > 0) rewards.Add(new { resourceId = "currency.coin", amount = offer.Coins });
                foreach (string id in new[] { "booster.tray", "booster.hand", "booster.shuffle", "booster.super" })
                    if (offer.Boosters > 0) rewards.Add(new { resourceId = id, amount = offer.Boosters });
                if (offer.UnlimitedSeconds > 0) rewards.Add(new { resourceId = "timer.unlimited-seconds", amount = offer.UnlimitedSeconds });
                if (offer.NoAds) rewards.Add(new { resourceId = "entitlement.remove-ads", amount = 1 });
                return new { id = offer.Id, titleKey = offer.Title, rewardText = offer.Coins.ToString("N0"), iconKey = "Gold" + offer.GoldIcon,
                    purchaseKind = (int)ShopPurchaseKind.Iap, purchaseOnce = offer.Once,
                    ownershipResourceId = offer.Once ? "entitlement.remove-ads" : null, storeProductId = offer.ProductId,
                    displayPrice = offer.Price, rewards };
            }).ToArray();
            catalog = JsonConvert.DeserializeObject<ShopCatalogConfig>(JsonConvert.SerializeObject(new { catalogId = "pixel-flow", offers }));
            catalog.Initialize("PixelFlow Foundation adapter");
        }
        public ShopViewState GetState()
        {
            var offers = new List<ShopOfferViewState>();
            foreach (var offer in catalog.Offers)
                if (service.Catalog.IsVisible(service.Catalog.Find(offer.Id), progress.Data, home)) offers.Add(new ShopOfferViewState(offer, false));
            return new ShopViewState(offers);
        }
        public ShopPurchaseResult Purchase(string id) => ShopPurchaseResult.IapGatewayUnavailable(Find(id));
        private ShopOfferConfig Find(string id) => catalog.Offers.FirstOrDefault(x => x.Id == id);
        public async UniTask<ShopPurchaseResult> PurchaseAsync(string id, CancellationToken cancellationToken = default)
        {
            var offer = Find(id); if (offer == null) return ShopPurchaseResult.OfferNotFound();
            if (service.Busy) return ShopPurchaseResult.PurchaseInProgress(offer);
            if (offer.PurchaseOnce && progress.Data.NoAds) return ShopPurchaseResult.AlreadyOwned(offer);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellationToken);
            try
            {
                if (service.Gateway.Unfulfilled.Any(x => x.OfferId == id)) { service.Recover(); return ShopPurchaseResult.Purchased(offer); }
                var result = await service.PurchaseAsync(id, linked.Token);
                return result switch
                {
                    ShopGatewayPurchaseStatus.Purchased => ShopPurchaseResult.Purchased(offer),
                    ShopGatewayPurchaseStatus.Cancelled => ShopPurchaseResult.PurchaseCancelled(offer),
                    _ => ShopPurchaseResult.PurchaseFailed(offer)
                };
            }
            catch (OperationCanceledException) { return ShopPurchaseResult.PurchaseCancelled(offer); }
            catch (Exception) { return ShopPurchaseResult.RewardGrantFailed(offer); }
        }
    }
}
