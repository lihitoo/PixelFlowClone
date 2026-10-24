using System;
using System.IO;
using NUnit.Framework;
using PixelFlow.Rules;

namespace PixelFlow.Tests
{
    public sealed class HomeFlowTests
    {
        [Test] public void RegenerationCapsAndDoesNotMutateInput()
        {
            var original = new HomeLives { Hearts = 2, NextHeartUtc = 100 };
            var state = HomeLifeRules.Advance(original, 120, 5, 10);
            Assert.That(state.Hearts, Is.EqualTo(5)); Assert.That(state.NextHeartUtc, Is.Zero);
            Assert.That(original.Hearts, Is.EqualTo(2));
            Assert.That(HomeLifeRules.Advance(original, 90, 5, 10).Hearts, Is.EqualTo(2));
        }
        [Test] public void UnlimitedLifeDoesNotSpendAndEmptyCannotPlay()
        {
            var state = new HomeLives { Hearts = 0, NextHeartUtc = 100, UnlimitedUntilUtc = 90 };
            Assert.That(HomeLifeRules.CanPlay(state, 80), Is.True);
            Assert.That(HomeLifeRules.Spend(state, 80, 5, 10).Hearts, Is.Zero);
            Assert.That(HomeLifeRules.Spend(state, 95, 5, 10), Is.Null);
        }
        [Test] public void LegacyLivesMigrationPreservesWalletAndSettings()
        {
            var fixture = new ProgressFlowTests.SaveFixture { Value = new PixelFlowProgress { ProfileSchema = 2, Coins = 1234, Boosters = new int[4], SoundEnabled = false } };
            var store = new ProgressStore(fixture, 2600);
            Assert.That(store.Data.Coins, Is.EqualTo(1234)); Assert.That(store.Data.SoundEnabled, Is.False);
            Assert.That(store.Data.Lives.Hearts, Is.EqualTo(5));
            Assert.That(new ProgressStore(fixture, 2600).Data.Coins, Is.EqualTo(1234));
        }
        [Test] public void RefillCommitsWalletAndLivesTogetherAndRejectsDoubleSpend()
        {
            var fixture = new ProgressFlowTests.SaveFixture { Value = new PixelFlowProgress { ProfileSchema = 2, Coins = 2000, Boosters = new int[4], Lives = new HomeLives { Hearts = 1 } } };
            var store = new ProgressStore(fixture, 2600); fixture.Fail = true;
            Assert.Throws<IOException>(() => store.RefillLives(100));
            Assert.That(store.Data.Coins, Is.EqualTo(2000)); Assert.That(store.Data.Lives.Hearts, Is.EqualTo(1));
            fixture.Fail = false; Assert.That(store.RefillLives(100), Is.True);
            Assert.That(store.Data.Coins, Is.EqualTo(1100)); Assert.That(store.Data.Lives.Hearts, Is.EqualTo(5));
            Assert.That(store.RefillLives(100), Is.False); Assert.That(store.Data.Coins, Is.EqualTo(1100));
        }
        [Test] public void SeparateAudioLevelsPersistAndRejectInvalidInput()
        {
            var fixture = new ProgressFlowTests.SaveFixture(); var store = new ProgressStore(fixture, 2600);
            store.SetAudioLevels(.2f, 0); var loaded = new ProgressStore(fixture, 2600);
            Assert.That(loaded.Data.MusicVolume, Is.EqualTo(.2f)); Assert.That(loaded.Data.SoundEnabled, Is.False);
            Assert.Throws<ArgumentException>(() => store.SetAudioLevels(float.NaN, 1));
            fixture.Fail = true; Assert.Throws<IOException>(() => store.SetAudioLevels(.5f, .5f));
            Assert.That(store.Data.MusicVolume, Is.EqualTo(.2f));
        }
    }
}
