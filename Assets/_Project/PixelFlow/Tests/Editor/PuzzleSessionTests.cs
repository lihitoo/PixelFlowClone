using System;
using System.Collections.Generic;
using NUnit.Framework;
using PixelFlow.Rules;

namespace PixelFlow.Tests
{
    public sealed class PuzzleSessionTests
    {
        private static LevelDefinition MakeLevel(int slots = 5, int limit = 5)
        {
            return new LevelDefinition
            {
                Width = 2, Height = 2, SlotCount = slots, ConveyorLimit = limit,
                Pixels = new List<PixelDefinition> { new() { X = 0, Y = 0, Color = 1 }, new() { X = 1, Y = 0, Color = 2 } },
                Queues = new List<QueueDefinition> {
                    new() { Shooters = new List<ShooterDefinition> { new() { Id = 0, Ammo = 1, Color = 1 }, new() { Id = 1, Ammo = 1, Color = 2 } } },
                    new() { Shooters = new List<ShooterDefinition> { new() { Id = 2, Ammo = 5, Color = 3 }, new() { Id = 3, Ammo = 5, Color = 3 } } }
                }
            };
        }

        [Test] public void OnlyFrontAndWaitingShootersCanBeLaunched()
        {
            var session = new PuzzleSession(MakeLevel());
            Assert.That(session.Launch(1), Is.EqualTo(LaunchResult.Unavailable));
            Assert.That(session.Launch(0), Is.EqualTo(LaunchResult.Accepted));
            Assert.That(session.Launch(0), Is.EqualTo(LaunchResult.Unavailable));
            Assert.That(session.Launch(1), Is.EqualTo(LaunchResult.Accepted));
        }

        [Test] public void FullBeltRejectsWithoutConsumingQueue()
        {
            var session = new PuzzleSession(MakeLevel(limit: 1));
            session.Launch(2);
            Assert.That(session.Launch(0), Is.EqualTo(LaunchResult.ConveyorFull));
            Assert.That(session.QueueAt(0).Id, Is.EqualTo(0));
            Assert.That(session.Launches, Is.EqualTo(1));
        }

        [Test] public void UnmatchedShooterReturnsWithAmmoAndCanBeRelaunched()
        {
            var session = new PuzzleSession(MakeLevel());
            session.Launch(2); session.Tick(9f);
            Assert.That(session.Waiting[0]?.Id, Is.EqualTo(2));
            Assert.That(session.Shooters[2].Ammo, Is.EqualTo(5));
            Assert.That(session.RemainingPixels, Is.EqualTo(2));
            Assert.That(session.Launch(2), Is.EqualTo(LaunchResult.Accepted));
            Assert.That(session.Waiting[0], Is.Null);
        }

        [Test] public void ReturningShooterLosesOnlyWhenNoWaitingSlotRemains()
        {
            var session = new PuzzleSession(MakeLevel(slots: 1));
            session.Launch(2); session.Tick(9f);
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Playing));
            session.Launch(3); session.Tick(9f);
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Lost));
            Assert.That(session.Launch(0), Is.EqualTo(LaunchResult.SessionFinished));
        }

        [Test] public void MatchingShotsConsumeOneAmmoAndEmitOneResult()
        {
            var session = new PuzzleSession(MakeLevel());
            int shots = 0, finishes = 0;
            session.Shot += _ => shots++; session.Finished += _ => finishes++;
            session.Launch(0); session.Launch(1); session.Tick(25);
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Won));
            Assert.That(session.RemainingPixels, Is.Zero);
            Assert.That(shots, Is.EqualTo(2)); Assert.That(finishes, Is.EqualTo(1));
            session.Tick(25); Assert.That(finishes, Is.EqualTo(1));
        }

        [Test] public void SessionsNeverMutateSourceDefinitionOrAnotherSession()
        {
            var definition = MakeLevel(); var first = new PuzzleSession(definition); var second = new PuzzleSession(definition);
            first.Launch(0); first.Tick(10);
            Assert.That(second.RemainingPixels, Is.EqualTo(2));
            Assert.That(second.QueueAt(0).Ammo, Is.EqualTo(1));
            Assert.That(definition.Pixels.Count, Is.EqualTo(2));
            Assert.That(definition.Queues[0].Shooters[0].Ammo, Is.EqualTo(1));
        }

        [Test] public void SmallAndLargeFrameStepsGiveTheSameRuleResult()
        {
            var first = new PuzzleSession(MakeLevel()); var second = new PuzzleSession(MakeLevel());
            first.Launch(2); second.Launch(2);
            first.Tick(9); for (int i = 0; i < 900; i++) second.Tick(.01f);
            Assert.That(first.Status, Is.EqualTo(second.Status));
            Assert.That(first.Waiting[0]?.Id, Is.EqualTo(second.Waiting[0]?.Id));
            Assert.That(first.RemainingPixels, Is.EqualTo(second.RemainingPixels));
        }

        [Test] public void InvalidCoordinatesDuplicateIdsAndMissingAmmoAreRejected()
        {
            var level = MakeLevel(); level.Pixels[0].X = -1; Assert.Throws<ArgumentException>(level.Validate);
            level = MakeLevel(); level.Queues[1].Shooters[0].Id = 0; Assert.Throws<ArgumentException>(level.Validate);
            level = MakeLevel(); level.Pixels[0].Color = 99; Assert.Throws<ArgumentException>(level.Validate);
        }

        [Test] public void InvalidTimeIsRejected()
        {
            var session = new PuzzleSession(MakeLevel());
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(-1));
        }

        [Test] public void NearerDifferentColorBlocksShotsFromThatSide()
        {
            var definition = MakeLevel();
            definition.Queues[0].Shooters.Reverse();
            var session = new PuzzleSession(definition);
            session.Launch(1); session.Tick(1.5f); // Left edge only; color 1 occludes color 2.
            Assert.That(session.Shooters[1].Ammo, Is.EqualTo(1));
            Assert.That(session.RemainingPixels, Is.EqualTo(2));
        }

        [Test] public void QueuedEntranceDoesNotAccumulateAShotBurst()
        {
            var definition = new LevelDefinition { Width = 32, Height = 32, Queues = new List<QueueDefinition> { new() } };
            for (int x = 0; x < 32; x++) for (int y = 0; y < 32; y++) definition.Pixels.Add(new PixelDefinition { X = x, Y = y, Color = 1 });
            for (int i = 0; i < 5; i++) definition.Queues[0].Shooters.Add(new ShooterDefinition { Id = i, Color = 1, Ammo = 250 });
            var session = new PuzzleSession(definition);
            float now = 0; var times = new List<float>();
            session.Shot += shot => { if (shot.ShooterId == 4) times.Add(now); };
            for (int i = 0; i < 5; i++) session.Launch(i);
            for (int tick = 0; tick < 500; tick++) { now += .01f; session.Tick(.01f); }
            Assert.That(times.Count, Is.GreaterThan(5));
            for (int i = 1; i < times.Count; i++) Assert.That(times[i] - times[i - 1], Is.GreaterThanOrEqualTo(.035f));
        }
    }
}
