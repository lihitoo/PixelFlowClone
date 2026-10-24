using System;
using System.IO;
using Dreamy.Datasave;
using NUnit.Framework;
using PixelFlow.Rules;

namespace PixelFlow.Tests
{
    public sealed class ProgressFlowTests
    {
        internal sealed class SaveFixture : IDatasaveService
        {
            public PixelFlowProgress Value = new(); public bool Fail;
            public T Load<T>(string key = null) where T : SaveData, new() => (T)(SaveData)Value;
            public void Save<T>(T data, string key = null) where T : SaveData { if (Fail) throw new IOException("Fixture disk failure"); Value = (PixelFlowProgress)(SaveData)data; }
            public void SaveAll() { } public bool Exists(string key) => true;
            public void Delete(string key) => throw new NotSupportedException(); public void DeleteAll() => throw new NotSupportedException();
        }
        [Test] public void MigrationPreservesOldUnlockCompletionAndSound()
        {
            var service = new SaveFixture { Value = new PixelFlowProgress { UnlockedLevel = 6, CompletedLevels = new() { 0, 2 }, SoundEnabled = false } };
            var store = new ProgressStore(service, 2600);
            Assert.That(store.Data.UnlockedLevel, Is.EqualTo(6)); Assert.That(store.Data.CompletedLevels, Is.EquivalentTo(new[] { 0, 2 }));
            Assert.That(store.Data.SoundEnabled, Is.False); Assert.That(store.Data.Coins, Is.EqualTo(300)); Assert.That(store.Amount(BoosterKind.Hand), Is.EqualTo(3));
            var reload = new ProgressStore(service, 2600); Assert.That(reload.Data.Coins, Is.EqualTo(300), "Migration gift is not applied twice");
        }
        [Test] public void SaveFailureCannotConsumeCoinsInventoryOrGrantCompletion()
        {
            var service = new SaveFixture(); var store = new ProgressStore(service, 2600); service.Fail = true;
            Assert.Throws<IOException>(() => store.Buy(BoosterKind.Tray));
            Assert.Throws<IOException>(() => store.RecordWin(0, "run-a"));
            Assert.That(store.Data.Coins, Is.EqualTo(300)); Assert.That(store.Amount(BoosterKind.Tray), Is.EqualTo(3)); Assert.That(store.Data.CompletedLevels, Is.Empty);
        }
        [Test] public void RewardsAndDailyClaimsAreIdempotentAndRejectClockRollback()
        {
            var store = new ProgressStore(new SaveFixture(), 2600);
            Assert.That(store.RecordWin(0, "run-a"), Is.EqualTo(50)); Assert.That(store.RecordWin(0, "run-a"), Is.Zero);
            Assert.That(store.ClaimDaily(1000), Is.True); Assert.That(store.ClaimDaily(1000), Is.False); Assert.That(store.ClaimDaily(999), Is.False);
            Assert.That(store.ClaimDaily(1001), Is.True); Assert.That(store.Data.DailyStreak, Is.EqualTo(2)); Assert.That(store.Data.Coins, Is.EqualTo(475));
        }
        [Test] public void PurchaseConsumptionCheckpointAndReloadAreOnePersistentState()
        {
            var service = new SaveFixture(); var store = new ProgressStore(service, 2600);
            Assert.That(store.Buy(BoosterKind.Hand), Is.True);
            var session = new PuzzleSession(FullGameplayTests.Small()); session.Launch(1, true);
            Assert.That(store.SpendBooster(BoosterKind.Hand, 0, session.CaptureState()), Is.True);
            var reload = new ProgressStore(service, 2600);
            Assert.That(reload.Data.Coins, Is.EqualTo(180)); Assert.That(reload.Amount(BoosterKind.Hand), Is.EqualTo(3));
            var restored = new PuzzleSession(FullGameplayTests.Small()); restored.RestoreState(reload.Data.ActiveRun);
            Assert.That(restored.Launches, Is.EqualTo(1)); Assert.That(restored.Shooters[1].Location, Is.EqualTo(ShooterLocation.Conveyor));
        }
        [Test] public void FutureSchemaFailsWithoutOverwritingTheSave()
        {
            var service = new SaveFixture { Value = new PixelFlowProgress { ProfileSchema = 99, Coins = 12345 } };
            Assert.Throws<InvalidDataException>(() => new ProgressStore(service, 2600)); Assert.That(service.Value.Coins, Is.EqualTo(12345));
        }
    }
}
