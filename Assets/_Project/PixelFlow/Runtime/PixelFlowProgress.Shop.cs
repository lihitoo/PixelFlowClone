using System;
using System.Collections.Generic;

namespace PixelFlow
{
    public sealed partial class ProgressStore
    {
        // One bundle, one persisted candidate; acknowledge only after this returns.
        public bool GrantShopPurchase(PixelFlowShopOffer offer, string transactionId, long now)
        {
            if (offer == null || string.IsNullOrWhiteSpace(transactionId) || transactionId.Length > 200 || now < 0 ||
                offer.Coins < 0 || offer.Boosters < 0 || offer.UnlimitedSeconds < 0) throw new ArgumentException("Invalid Shop receipt.");
            if (Data.ShopTransactions?.Contains(transactionId) == true) return false;
            var next = Clone(Data); next.ShopTransactions ??= new List<string>();
            // A durable non-consumable restored/purchased on another device must
            // never replay the bundled consumables after entitlement is present.
            if (!(offer.Once && next.NoAds))
            {
                next.Coins = AddCoins(next.Coins, offer.Coins);
                for (int i = 0; i < next.Boosters.Length; i++)
                {
                    int amount = checked(next.Boosters[i] + offer.Boosters);
                    if (amount > 999) throw new InvalidOperationException("Your booster inventory is full. Purchase remains pending.");
                    next.Boosters[i] = amount;
                }
                if (offer.UnlimitedSeconds > 0)
                    next.Lives.UnlimitedUntilUtc = checked(Math.Max(now, next.Lives.UnlimitedUntilUtc) + offer.UnlimitedSeconds);
                next.NoAds |= offer.NoAds;
            }
            next.ShopTransactions.Add(transactionId); Commit(next); return true;
        }
        public bool RestoreNoAds()
        {
            if (Data.NoAds) return false;
            var next = Clone(Data); next.NoAds = true; Commit(next); return true;
        }
    }
}
