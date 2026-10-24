using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Shop;

namespace PixelFlow
{
    public sealed class PixelFlowShopService
    {
        private readonly ProgressStore progress;
        private readonly HomeTuning home;
        public PixelFlowShopCatalog Catalog { get; }
        public IPixelFlowStoreGateway Gateway { get; }
        public bool Busy { get; private set; }
        public PixelFlowShopService(ProgressStore progress, PixelFlowShopCatalog catalog, HomeTuning home, IPixelFlowStoreGateway gateway)
        { this.progress = progress; Catalog = catalog; this.home = home; Gateway = gateway; }
        public async UniTask<ShopGatewayPurchaseStatus> PurchaseAsync(string id, CancellationToken token)
        {
            if (Busy || Gateway.HasDeferred) return ShopGatewayPurchaseStatus.Failed;
            var offer = Catalog.Find(id);
            Busy = true;
            try
            {
                // Deliver old approved orders before starting another charge.
                Recover();
                if (!Catalog.IsVisible(offer, progress.Data, home)) return ShopGatewayPurchaseStatus.Failed;
                var result = await Gateway.PurchaseAsync(new ShopGatewayPurchaseRequest(offer.Id, offer.ProductId), token);
                if (result.IsSuccess)
                {
                    // Use gateway's retained order, never UI-supplied rewards.
                    bool found = false;
                    foreach (var order in Gateway.Unfulfilled)
                        if (order.TransactionId == result.TransactionId && order.OfferId == id && order.ProductId == offer.ProductId) found = true;
                    if (!found) throw new InvalidOperationException("Purchase receipt is unavailable.");
                    Recover();
                }
                return result.Status;
            }
            finally { Busy = false; }
        }
        public int Recover()
        {
            int delivered = 0;
            foreach (var order in Gateway.Unfulfilled)
            {
                var offer = Catalog.Find(order.OfferId);
                if (offer.ProductId != order.ProductId) throw new InvalidOperationException("Receipt product mismatch.");
                if (progress.GrantShopPurchase(offer, order.TransactionId, ProgressStore.UtcSeconds)) delivered++;
                Gateway.Acknowledge(order.TransactionId);
            }
            return delivered;
        }
        public async UniTask<bool> RestoreAsync(CancellationToken token)
        {
            if (Busy || Gateway.HasDeferred) throw new InvalidOperationException("Another purchase is pending.");
            Busy = true;
            try
            {
                Recover();
                if (!await Gateway.RestoreNoAdsAsync(token)) return false;
                progress.RestoreNoAds(); return true;
            }
            finally { Busy = false; }
        }
    }
}
