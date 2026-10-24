using System;
using System.Collections.Generic;
using Dreamy.Economy;
using UnityEngine;

namespace PixelFlow
{
    /// <summary>Economy integration over the existing progress authority; no second wallet save.</summary>
    public sealed class PixelFlowResourceWallet : IResourceWallet, IResourceBalanceSource, IDisposable
    {
        private readonly ProgressStore progress;
        private readonly Dictionary<ResourceId, long> observed = new();
        private bool disposed;
        public event Action<ResourceBalanceChanged> BalanceChanged;
        public PixelFlowResourceWallet(ProgressStore progress)
        { this.progress = progress ?? throw new ArgumentNullException(nameof(progress)); progress.Changed += Refresh; }
        public long GetBalance(ResourceId id)
        {
            long value = ProgressStore.ResourceBalance(progress.Data, id);
            observed[id] = value; return value;
        }
        public bool TryGrant(ResourceGrantRequest request)
        {
            if (disposed) throw new ObjectDisposedException(nameof(PixelFlowResourceWallet));
            return progress.ApplyResourceTransaction(request.TransactionId, default, new[] { request.Resource });
        }
        public bool TryExchange(ResourceExchangeRequest request)
        {
            if (disposed) throw new ObjectDisposedException(nameof(PixelFlowResourceWallet));
            return progress.ApplyResourceTransaction(request.TransactionId, request.Cost, request.Rewards);
        }
        private void Refresh()
        {
            foreach (var id in new List<ResourceId>(observed.Keys))
            {
                long value = ProgressStore.ResourceBalance(progress.Data, id); if (value == observed[id]) continue;
                observed[id] = value;
                // A HUD/feedback listener must not turn a committed transaction
                // into a delivery failure or stop notifications to other holders.
                foreach (var callback in BalanceChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
                    try { ((Action<ResourceBalanceChanged>)callback)(new ResourceBalanceChanged(id, value)); }
                    catch (Exception error) { Debug.LogWarning("Resource holder notification failed: " + error.Message); }
            }
        }
        public void Dispose()
        { if (disposed) return; disposed = true; progress.Changed -= Refresh; observed.Clear(); BalanceChanged = null; }
    }

    public sealed partial class ProgressStore
    {
        public static long ResourceBalance(PixelFlowProgress data, ResourceId id) => id.Value switch
        {
            "currency.coin" => data.Coins,
            "booster.tray" => data.Boosters[0], "booster.hand" => data.Boosters[1],
            "booster.shuffle" => data.Boosters[2], "booster.super" => data.Boosters[3],
            "entitlement.remove-ads" => data.NoAds ? 1 : 0,
            "life.heart" => data.Lives.Hearts,
            _ => throw new ArgumentException("Unknown Pixel Flow resource: " + id.Value)
        };
        public bool ApplyResourceTransaction(string transaction, ResourceAmount cost, IReadOnlyList<ResourceAmount> rewards)
        {
            if (string.IsNullOrWhiteSpace(transaction) || transaction.Length > 200 || rewards == null || rewards.Count == 0)
                throw new ArgumentException("Invalid resource transaction.");
            if (Data.ResourceTransactions?.Contains(transaction) == true) return true;
            if (cost.ResourceId.IsValid && ResourceBalance(Data, cost.ResourceId) < cost.Amount) return false;
            var next = Clone(Data);
            if (cost.ResourceId.IsValid) SetResource(next, cost.ResourceId, checked(ResourceBalance(next, cost.ResourceId) - cost.Amount));
            foreach (var reward in rewards) SetResource(next, reward.ResourceId, checked(ResourceBalance(next, reward.ResourceId) + reward.Amount));
            next.ResourceTransactions ??= new List<string>(); next.ResourceTransactions.Add(transaction); Commit(next); return true;
        }
        private void SetResource(PixelFlowProgress data, ResourceId id, long value)
        {
            if (value < 0 || value > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
            int amount = checked((int)value);
            switch (id.Value)
            {
                case "currency.coin": data.Coins = amount; break;
                case "booster.tray": SetBooster(0); break;
                case "booster.hand": SetBooster(1); break;
                case "booster.shuffle": SetBooster(2); break;
                case "booster.super": SetBooster(3); break;
                case "entitlement.remove-ads": if (amount > 1) amount = 1; data.NoAds = amount > 0; break;
                case "life.heart": if (amount > homeTuning.MaximumHearts) throw new ArgumentOutOfRangeException(nameof(value)); data.Lives.Hearts = amount; break;
                default: throw new ArgumentException("Unknown Pixel Flow resource: " + id.Value);
            }
            void SetBooster(int index) { if (amount > 999) throw new ArgumentOutOfRangeException(nameof(value)); data.Boosters[index] = amount; }
        }
    }
}
