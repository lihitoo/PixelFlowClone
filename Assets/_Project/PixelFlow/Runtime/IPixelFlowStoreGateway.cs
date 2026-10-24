using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Shop;

namespace PixelFlow
{
    // SDK seam. Future adapters supply verified orders and acknowledge after
    // ProgressStore commits, without changing the Shop view/domain transaction.
    public interface IPixelFlowStoreGateway : IShopPurchaseGateway
    {
        IReadOnlyList<ShopStoreOrder> Unfulfilled { get; }
        bool HasDeferred { get; }
        void Acknowledge(string transactionId);
        UniTask<bool> RestoreNoAdsAsync(CancellationToken token);
    }
    [Serializable] public sealed class ShopStoreOrder
    {
        public string OfferId, ProductId, TransactionId, Status;
        public bool NoAds;
    }
}
