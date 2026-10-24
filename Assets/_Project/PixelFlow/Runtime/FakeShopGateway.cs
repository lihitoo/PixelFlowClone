using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Datasave;
using Dreamy.Shop;
using Newtonsoft.Json;

namespace PixelFlow
{
    [Serializable] public sealed class FakeShopStoreSave : SaveData
    {
        public override string SaveKey => "pixel-flow-fake-store";
        public override int Version => 1;
        public List<ShopStoreOrder> Orders = new();
    }
    public enum FakeShopOutcome { Success, Cancelled, Failed, Deferred }

    // Explicitly temporary, owner-requested fake. No billing SDK/network call.
    // Provider ledger persists before callback, independently of game rewards.
    public sealed class FakeShopGateway : IPixelFlowStoreGateway
    {
        private readonly IDatasaveService saves;
        private readonly PixelFlowShopCatalog catalog;
        private FakeShopStoreSave state;
        private UniTaskCompletionSource<ShopGatewayPurchaseResult> waiting;
        public FakeShopOutcome NextOutcome { get; set; }
        public bool FailRestore { get; set; }
        public int DelayMilliseconds { get; set; } = 500;
        public bool HasDeferred => state.Orders.Any(x => x.Status == "Deferred");
        public IReadOnlyList<ShopStoreOrder> Unfulfilled => state.Orders.Where(x => x.Status == "Approved").Select(CopyOrder).ToArray();
        private static ShopStoreOrder CopyOrder(ShopStoreOrder value) => JsonConvert.DeserializeObject<ShopStoreOrder>(JsonConvert.SerializeObject(value));
        public FakeShopGateway(IDatasaveService saves, PixelFlowShopCatalog catalog)
        {
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves)); this.catalog = catalog;
            state = saves.Load<FakeShopStoreSave>();
            if (state.Orders == null || state.Orders.Any(x => x == null || string.IsNullOrWhiteSpace(x.TransactionId) || x.Status is not ("Approved" or "Acknowledged" or "Deferred" or "Cancelled")))
                throw new InvalidOperationException("Invalid fake-store ledger.");
        }
        private void Commit(Action<FakeShopStoreSave> mutate)
        {
            var candidate = JsonConvert.DeserializeObject<FakeShopStoreSave>(JsonConvert.SerializeObject(state));
            mutate(candidate); saves.Save(candidate); state = candidate;
        }
        public async UniTask<ShopGatewayPurchaseResult> PurchaseAsync(ShopGatewayPurchaseRequest request, CancellationToken cancellationToken = default)
        {
            var offer = catalog.Find(request.OfferId);
            if (offer.ProductId != request.StoreProductId || HasDeferred) return ShopGatewayPurchaseResult.Failed();
            var outcome = NextOutcome; NextOutcome = FakeShopOutcome.Success;
            if (DelayMilliseconds > 0) await UniTask.Delay(DelayMilliseconds, ignoreTimeScale: true, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (outcome == FakeShopOutcome.Cancelled) return ShopGatewayPurchaseResult.Cancelled();
            if (outcome == FakeShopOutcome.Failed) return ShopGatewayPurchaseResult.Failed();
            string id = "fake-" + Guid.NewGuid().ToString("N");
            Commit(s => s.Orders.Add(new ShopStoreOrder { OfferId = offer.Id, ProductId = offer.ProductId, TransactionId = id, NoAds = offer.NoAds, Status = outcome == FakeShopOutcome.Deferred ? "Deferred" : "Approved" }));
            if (outcome != FakeShopOutcome.Deferred) return ShopGatewayPurchaseResult.Purchased(id);
            waiting = new UniTaskCompletionSource<ShopGatewayPurchaseResult>();
            try { return await waiting.Task.AttachExternalCancellation(cancellationToken); }
            finally { waiting = null; }
        }
        public void ResolveDeferred(bool approve)
        {
            var order = state.Orders.FirstOrDefault(x => x.Status == "Deferred"); if (order == null) return;
            string id = order.TransactionId;
            Commit(s => s.Orders.Find(x => x.TransactionId == id).Status = approve ? "Approved" : "Cancelled");
            waiting?.TrySetResult(approve ? ShopGatewayPurchaseResult.Purchased(id) : ShopGatewayPurchaseResult.Cancelled());
        }
        public void Acknowledge(string transactionId)
        {
            var order = state.Orders.Find(x => x.TransactionId == transactionId) ?? throw new ArgumentException("Unknown order.");
            if (order.Status == "Acknowledged") return;
            if (order.Status != "Approved") throw new InvalidOperationException("Order is not approved.");
            Commit(s => s.Orders.Find(x => x.TransactionId == transactionId).Status = "Acknowledged");
        }
        public async UniTask<bool> RestoreNoAdsAsync(CancellationToken token)
        {
            if (DelayMilliseconds > 0) await UniTask.Delay(DelayMilliseconds, ignoreTimeScale: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            if (FailRestore) throw new InvalidOperationException("Unable to restore purchases. Please try again.");
            return state.Orders.Any(x => x.NoAds && x.Status is "Approved" or "Acknowledged");
        }
    }
}
