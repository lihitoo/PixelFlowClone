using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Dreamy.Datasave;
using Dreamy.Shop;
using Newtonsoft.Json;
using NUnit.Framework;
using PixelFlow.Rules;
using UnityEngine;

namespace PixelFlow.Tests
{
    public sealed class ShopFlowTests
    {
        internal sealed class StoreFixture : IDatasaveService
        {
            private readonly Dictionary<string, string> values = new();
            public string FailKey; public bool FailAcknowledgement;
            public T Load<T>(string key = null) where T : SaveData, new()
            { var empty = new T(); return values.TryGetValue(key ?? empty.SaveKey, out var value) ? JsonConvert.DeserializeObject<T>(value) : empty; }
            public void Save<T>(T data, string key = null) where T : SaveData
            {
                string id = key ?? data.SaveKey;
                if (id == FailKey || FailAcknowledgement && data is FakeShopStoreSave store && store.Orders.Exists(x => x.Status == "Acknowledged")) throw new IOException("Fixture disk failure");
                values[id] = JsonConvert.SerializeObject(data);
            }
            public void SaveAll() { } public bool Exists(string key) => values.ContainsKey(key);
            public void Delete(string key) => values.Remove(key); public void DeleteAll() => values.Clear();
        }
        private StoreFixture storage; private ProgressStore progress; private PixelFlowShopCatalog catalog;
        private FakeShopGateway gateway; private PixelFlowShopService shop;
        [SetUp] public void Setup()
        {
            storage = new StoreFixture(); progress = new ProgressStore(storage, 2600);
            progress.Data.UnlockedLevel = 100; progress.Save();
            catalog = JsonConvert.DeserializeObject<PixelFlowShopCatalog>(Resources.Load<TextAsset>("PixelFlow/pixelFlowShop").text); catalog.Initialize("fixture");
            gateway = new FakeShopGateway(storage, catalog) { DelayMilliseconds = 0 };
            shop = new PixelFlowShopService(progress, catalog, new HomeTuning(), gateway);
        }
        [Test] public void CatalogMatchesObservedElevenOffersAndEarlyUnlocks()
        {
            Assert.That(catalog.Offers.Count, Is.EqualTo(11)); Assert.That(catalog.Find("medium-bundle").UnlimitedSeconds, Is.EqualTo(7200));
            progress.Data.UnlockedLevel = 0;
            Assert.That(catalog.Offers.FindAll(x => catalog.IsVisible(x, progress.Data, new HomeTuning())).Count, Is.EqualTo(6));
        }
        [Test] public async Task RepeatedConsumablePurchasesHaveDistinctReceiptsAndPersist()
        {
            Assert.That(await shop.PurchaseAsync("gold-1000", default), Is.EqualTo(ShopGatewayPurchaseStatus.Purchased));
            Assert.That(await shop.PurchaseAsync("gold-1000", default), Is.EqualTo(ShopGatewayPurchaseStatus.Purchased));
            Assert.That(new ProgressStore(storage, 2600).Data.Coins, Is.EqualTo(2300));
            Assert.That(progress.Data.ShopTransactions.Count, Is.EqualTo(2)); Assert.That(gateway.Unfulfilled, Is.Empty);
        }
        [TestCase("gold-1000", 1000, 0, 0, false)]
        [TestCase("gold-5000", 5000, 0, 0, false)]
        [TestCase("gold-10000", 10000, 0, 0, false)]
        [TestCase("gold-25000", 25000, 0, 0, false)]
        [TestCase("gold-50000", 50000, 0, 0, false)]
        [TestCase("gold-100000", 100000, 0, 0, false)]
        [TestCase("no-ads", 0, 0, 0, true)]
        [TestCase("no-ads-bundle", 1000, 1, 3600, true)]
        [TestCase("small-bundle", 5000, 1, 7200, false)]
        [TestCase("medium-bundle", 10000, 2, 7200, false)]
        [TestCase("ultra-bundle", 25000, 5, 21600, false)]
        public async Task EveryProductCommitsItsObservedRewards(string id, int coins, int boosters, int unlimitedSeconds, bool noAds)
        {
            int initialCoins = progress.Data.Coins;
            int[] initialInventory = (int[])progress.Data.Boosters.Clone();
            long started = ProgressStore.UtcSeconds;
            Assert.That(await shop.PurchaseAsync(id, default), Is.EqualTo(ShopGatewayPurchaseStatus.Purchased));
            var persisted = new ProgressStore(storage, 2600).Data;
            Assert.That(persisted.Coins, Is.EqualTo(initialCoins + coins));
            for (int i = 0; i < 4; i++) Assert.That(persisted.Boosters[i], Is.EqualTo(initialInventory[i] + boosters));
            Assert.That(persisted.NoAds, Is.EqualTo(noAds));
            if (unlimitedSeconds == 0) Assert.That(persisted.Lives.UnlimitedUntilUtc, Is.Zero);
            else Assert.That(persisted.Lives.UnlimitedUntilUtc, Is.InRange(started + unlimitedSeconds, ProgressStore.UtcSeconds + unlimitedSeconds));
            Assert.That(persisted.ShopTransactions.Count, Is.EqualTo(1));
            Assert.That(gateway.Unfulfilled, Is.Empty);
        }
        [Test] public async Task BundleSaveFailureLeavesAllRewardsUntouchedAndRecoversOnce()
        {
            storage.FailKey = PixelFlowProgress.Key;
            Assert.ThrowsAsync<IOException>(async () => await shop.PurchaseAsync("small-bundle", default).AsTask());
            Assert.That(progress.Data.Coins, Is.EqualTo(300)); Assert.That(progress.Amount(BoosterKind.Tray), Is.EqualTo(3));
            Assert.That(progress.Data.Lives.UnlimitedUntilUtc, Is.Zero); Assert.That(gateway.Unfulfilled.Count, Is.EqualTo(1));
            storage.FailKey = null; Assert.That(shop.Recover(), Is.EqualTo(1)); Assert.That(shop.Recover(), Is.Zero);
            Assert.That(progress.Data.Coins, Is.EqualTo(5300)); Assert.That(progress.Amount(BoosterKind.Color), Is.EqualTo(4));
            Assert.That(progress.Data.Lives.UnlimitedUntilUtc, Is.GreaterThan(ProgressStore.UtcSeconds + 7100));
            var reloaded = new PixelFlowShopService(new ProgressStore(storage, 2600), catalog, new HomeTuning(), new FakeShopGateway(storage, catalog));
            Assert.That(reloaded.Recover(), Is.Zero); await Task.CompletedTask;
        }
        [Test] public async Task AcknowledgementFailureCannotReplayPaidRewards()
        {
            storage.FailAcknowledgement = true;
            Assert.ThrowsAsync<IOException>(async () => await shop.PurchaseAsync("gold-1000", default).AsTask());
            Assert.That(progress.Data.Coins, Is.EqualTo(1300)); Assert.That(gateway.Unfulfilled.Count, Is.EqualTo(1));
            storage.FailAcknowledgement = false; Assert.That(shop.Recover(), Is.Zero); Assert.That(progress.Data.Coins, Is.EqualTo(1300));
            Assert.That(gateway.Unfulfilled, Is.Empty); await Task.CompletedTask;
        }
        [Test] public async Task NoAdsBundleCannotBeBoughtTwiceAndRestoreDoesNotRepeatConsumables()
        {
            await shop.PurchaseAsync("no-ads-bundle", default);
            long unlimited = progress.Data.Lives.UnlimitedUntilUtc; Assert.That(progress.Data.NoAds, Is.True);
            Assert.That(await shop.PurchaseAsync("no-ads", default), Is.EqualTo(ShopGatewayPurchaseStatus.Failed));
            progress.Data.NoAds = false; progress.Save(); Assert.That(await shop.RestoreAsync(default), Is.True);
            Assert.That(progress.Data.Coins, Is.EqualTo(1300)); Assert.That(progress.Amount(BoosterKind.Hand), Is.EqualTo(4));
            Assert.That(progress.Data.Lives.UnlimitedUntilUtc, Is.EqualTo(unlimited)); Assert.That(progress.Data.ShopTransactions.Count, Is.EqualTo(1));
        }
        [TestCase(FakeShopOutcome.Cancelled, ShopGatewayPurchaseStatus.Cancelled)]
        [TestCase(FakeShopOutcome.Failed, ShopGatewayPurchaseStatus.Failed)]
        public async Task CancelOrFailureDoesNotGrant(FakeShopOutcome outcome, ShopGatewayPurchaseStatus expected)
        {
            gateway.NextOutcome = outcome; Assert.That(await shop.PurchaseAsync("gold-1000", default), Is.EqualTo(expected));
            Assert.That(progress.Data.Coins, Is.EqualTo(300)); Assert.That(progress.Data.ShopTransactions, Is.Empty); Assert.That(gateway.Unfulfilled, Is.Empty);
        }
        [Test] public async Task DeferredPurchaseBlocksAnotherAndOnlyGrantsAfterApproval()
        {
            gateway.NextOutcome = FakeShopOutcome.Deferred;
            var pending = shop.PurchaseAsync("gold-1000", default).AsTask();
            Assert.That(shop.Busy, Is.True); Assert.That(progress.Data.Coins, Is.EqualTo(300));
            Assert.That(await shop.PurchaseAsync("gold-5000", default), Is.EqualTo(ShopGatewayPurchaseStatus.Failed));
            gateway.ResolveDeferred(true); Assert.That(await pending, Is.EqualTo(ShopGatewayPurchaseStatus.Purchased));
            Assert.That(progress.Data.Coins, Is.EqualTo(1300)); Assert.That(shop.Busy, Is.False);
        }
        [Test] public async Task DeferredOrderSurvivesCancelledClientAndRestart()
        {
            using var cancellation = new CancellationTokenSource(); gateway.NextOutcome = FakeShopOutcome.Deferred;
            var task = shop.PurchaseAsync("gold-1000", cancellation.Token).AsTask(); cancellation.Cancel();
            try { await task; Assert.Fail("Expected cancellation"); } catch (OperationCanceledException) { }
            var provider = new FakeShopGateway(storage, catalog) { DelayMilliseconds = 0 };
            Assert.That(provider.HasDeferred, Is.True); provider.ResolveDeferred(true);
            var resumed = new PixelFlowShopService(progress, catalog, new HomeTuning(), provider);
            Assert.That(resumed.Recover(), Is.EqualTo(1)); Assert.That(resumed.Recover(), Is.Zero); Assert.That(progress.Data.Coins, Is.EqualTo(1300));
        }
        [Test] public async Task InventoryOverflowCannotPartiallyGrantBundle()
        {
            progress.Data.Boosters[2] = 999; progress.Save();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await shop.PurchaseAsync("small-bundle", default).AsTask());
            Assert.That(progress.Data.Coins, Is.EqualTo(300)); Assert.That(progress.Amount(BoosterKind.Tray), Is.EqualTo(3));
            Assert.That(progress.Data.Lives.UnlimitedUntilUtc, Is.Zero); Assert.That(gateway.Unfulfilled.Count, Is.EqualTo(1)); await Task.CompletedTask;
        }
        [Test] public async Task EmptyRestoreAndRestoreFailureCannotInventEntitlement()
        {
            Assert.That(await shop.RestoreAsync(default), Is.False); gateway.FailRestore = true;
            Assert.ThrowsAsync<InvalidOperationException>(async () => await shop.RestoreAsync(default).AsTask()); Assert.That(progress.Data.NoAds, Is.False);
        }
    }
}
