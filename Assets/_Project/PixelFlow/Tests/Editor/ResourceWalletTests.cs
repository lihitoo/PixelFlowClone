using System;
using System.IO;
using Dreamy.Economy;
using NUnit.Framework;

namespace PixelFlow.Tests
{
    public sealed class ResourceWalletTests
    {
        private static readonly ResourceId Coins = new("currency.coin");
        private static readonly ResourceId Tray = new("booster.tray");
        [Test]
        public void ExchangePersistsOnceAndSurvivesReloadWithoutSecondWallet()
        {
            var save = new ShopFlowTests.StoreFixture(); var progress = new ProgressStore(save, 2600);
            using var wallet = new PixelFlowResourceWallet(progress);
            long original = wallet.GetBalance(Coins), trays = wallet.GetBalance(Tray); int changes = 0;
            wallet.BalanceChanged += _ => changes++;
            var request = new ResourceExchangeRequest("exchange-1", new ResourceAmount(Coins, 100), new[] { new ResourceAmount(Tray, 1) });
            Assert.That(wallet.TryExchange(request), Is.True);
            Assert.That(wallet.TryExchange(request), Is.True);
            Assert.That(wallet.GetBalance(Coins), Is.EqualTo(original - 100));
            Assert.That(wallet.GetBalance(Tray), Is.EqualTo(trays + 1)); Assert.That(changes, Is.EqualTo(2));
            using var reloaded = new PixelFlowResourceWallet(new ProgressStore(save, 2600));
            Assert.That(reloaded.TryExchange(request), Is.True); Assert.That(reloaded.GetBalance(Coins), Is.EqualTo(original - 100));
        }
        [Test]
        public void FailedPersistenceDoesNotPublishOrDeduplicateAnUndeliveredGrant()
        {
            var save = new ShopFlowTests.StoreFixture(); var progress = new ProgressStore(save, 2600);
            using var wallet = new PixelFlowResourceWallet(progress); long before = wallet.GetBalance(Coins); int changes = 0;
            wallet.BalanceChanged += _ => changes++; save.FailKey = progress.Data.SaveKey;
            var request = new ResourceGrantRequest("grant-1", new ResourceAmount(Coins, 1000));
            Assert.Throws<IOException>(() => wallet.TryGrant(request));
            Assert.That(wallet.GetBalance(Coins), Is.EqualTo(before)); Assert.That(changes, Is.Zero);
            save.FailKey = null; Assert.That(wallet.TryGrant(request), Is.True);
            Assert.That(wallet.GetBalance(Coins), Is.EqualTo(before + 1000)); Assert.That(changes, Is.EqualTo(1));
        }
        [Test]
        public void InsufficientAndOverflowLeaveAllResourcesUnchanged()
        {
            var save = new ShopFlowTests.StoreFixture(); var progress = new ProgressStore(save, 2600);
            using var wallet = new PixelFlowResourceWallet(progress); long before = wallet.GetBalance(Coins);
            Assert.That(wallet.TryExchange(new ResourceExchangeRequest("poor", new ResourceAmount(Coins, before + 1), new[] { new ResourceAmount(Tray, 1) })), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TryExchange(new ResourceExchangeRequest("overflow", new ResourceAmount(Coins, 100), new[] { new ResourceAmount(Tray, 1000) })));
            Assert.That(wallet.GetBalance(Coins), Is.EqualTo(before)); Assert.That(progress.Data.ResourceTransactions, Does.Not.Contain("overflow"));
        }
        [Test]
        public void DisposeUnsubscribesAndRejectsFurtherWrites()
        {
            var progress = new ProgressStore(new ShopFlowTests.StoreFixture(), 2600);
            var wallet = new PixelFlowResourceWallet(progress); wallet.GetBalance(Coins); int changes = 0;
            wallet.BalanceChanged += _ => changes++; wallet.Dispose();
            progress.Data.Coins++; progress.Save(); Assert.That(changes, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => wallet.TryGrant(new ResourceGrantRequest("closed", new ResourceAmount(Coins, 1))));
        }
    }
}
