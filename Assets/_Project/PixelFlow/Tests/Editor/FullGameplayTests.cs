using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using PixelFlow.Rules;
using UnityEditor;
using UnityEngine;

namespace PixelFlow.Tests
{
    public sealed class FullGameplayTests
    {
        public static LevelDefinition Small()
        {
            return new LevelDefinition
            {
                SourceNumber = 1, Width = 2, Height = 2,
                Pixels = new() { new() { X = 0, Y = 0, Color = 1 }, new() { X = 1, Y = 0, Color = 2 } },
                Queues = new() { new() { Shooters = new() { new() { Id = 0, Color = 1, Ammo = 1 }, new() { Id = 1, Color = 2, Ammo = 1, Surprise = true } } } }
            };
        }
        private static PixelFlowCampaignConfig Catalog()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/PixelFlow/Resources/PixelFlow/pixelFlowCampaign.json");
            var data = JsonConvert.DeserializeObject<PixelFlowCampaignConfig>(asset.text); data.Initialize("pixelFlowCampaign"); return data;
        }

        [TestCase(false, -1)] [TestCase(true, 2)]
        public void BoardColorPickUsesBlockingLayerAndRejectsOutsideBoard(bool goal, int expected)
        {
            var definition = Small();
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = BoardObjectKind.Wood, Goal = goal,
                Cells = new() { new(0, 0) }, Layers = new() { new(2, 1) } });
            var session = new PuzzleSession(definition);
            Assert.That(session.PixelAt(0, 0), Is.EqualTo(1), "Pixel color differs from the covering object");
            Assert.That(session.TargetColorAt(0, 0), Is.EqualTo(expected));
            Assert.That(session.TargetColorAt(-1, 0), Is.EqualTo(-1));
            Assert.That(session.TargetColorAt(definition.Width, 0), Is.EqualTo(-1));
            if (goal)
            {
                Assert.That(session.ClearVisibleColor(2), Is.True);
                Assert.That(session.TargetColorAt(0, 0), Is.EqualTo(1), "Cleared cover reveals the original pixel target");
            }
        }

        [Test, Timeout(180000)] public void EveryMainCampaignEntryLoadsWithTypedValidatedFeatures()
        {
            var campaign = Catalog(); Assert.That(campaign.LevelCount, Is.EqualTo(2600));
            int objects = 0, links = 0;
            for (int index = 0; index < campaign.LevelCount; index++)
            {
                var level = campaign.GetLevel(index);
                Assert.That(level.SourceNumber, Is.EqualTo(index + 1));
                objects += level.Objects.Count; links += level.Links.Count;
            }
            Assert.That(objects, Is.GreaterThan(1000)); Assert.That(links, Is.GreaterThan(1000));
        }

        [TestCase(8)] [TestCase(14)] [TestCase(30)] [TestCase(40)] [TestCase(55)] [TestCase(70)]
        [TestCase(100)] [TestCase(120)] [TestCase(140)] [TestCase(171)] [TestCase(200)] [TestCase(230)]
        [TestCase(290)] [TestCase(350)] [TestCase(400)] [TestCase(500)] [TestCase(600)] [TestCase(700)]
        [TestCase(801)] [TestCase(901)] [TestCase(1001)] [TestCase(1101)] [TestCase(1201)] [TestCase(1301)]
        [TestCase(1401)] [TestCase(1501)] [TestCase(1601)] [TestCase(1701)] [TestCase(1801)] [TestCase(1901)]
        [TestCase(2001)] [TestCase(2201)] [TestCase(2401)]
        public void RepresentativeSourceFeatureStateRoundTrips(int number)
        {
            var campaign = Catalog(); var definition = campaign.GetLevel(number - 1);
            var first = new PuzzleSession(definition);
            string encoded = JsonConvert.SerializeObject(first.CaptureState());
            var restored = new PuzzleSession(definition);
            restored.RestoreState(JsonConvert.DeserializeObject<SessionSnapshot>(encoded));
            Assert.That(JsonConvert.SerializeObject(restored.CaptureState()), Is.EqualTo(encoded));
        }

        [Test] public void LinkedLaunchIsAtomicAndRespectsTotalCapacity()
        {
            var definition = Small(); definition.ConveyorLimit = 1;
            definition.Links.Add(new LinkDefinition { Members = new() { 0, 1 } });
            var session = new PuzzleSession(definition);
            Assert.That(session.Launch(0), Is.EqualTo(LaunchResult.ConveyorFull));
            Assert.That(session.QueueCount(0), Is.EqualTo(2)); Assert.That(session.Launches, Is.Zero);
            Assert.That(session.AddTray(), Is.True);
            Assert.That(session.Launch(0), Is.EqualTo(LaunchResult.Accepted));
            Assert.That(session.Active.Count, Is.EqualTo(2)); Assert.That(session.QueueCount(0), Is.Zero);
        }

        [Test] public void HiddenShooterRevealsAtFrontAndFrozenShooterNeedsHits()
        {
            var definition = Small(); definition.Queues[0].Shooters[1].Ice = 1;
            var session = new PuzzleSession(definition);
            Assert.That(session.Shooters[1].Revealed, Is.False);
            session.Launch(0); Assert.That(session.Shooters[1].Revealed, Is.True);
            Assert.That(session.CanSelect(1), Is.False);
            session.Tick(1.5f);
            Assert.That(session.Shooters[1].Ice, Is.Zero); Assert.That(session.CanSelect(1), Is.True);
        }

        [Test] public void HammerRemovesWoodWithoutInventingExtraAmmunition()
        {
            var definition = Small(); definition.Queues[0].Shooters.Insert(0, new ShooterDefinition { Id = 9, Kind = ShooterKind.Hammer });
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = BoardObjectKind.Wood, Cells = new() { new(0, 0) }, Layers = new() { new(-1, 1) } });
            var session = new PuzzleSession(definition);
            Assert.That(session.Launch(9), Is.EqualTo(LaunchResult.Accepted));
            Assert.That(session.BoardObjects[0].Active, Is.False); Assert.That(session.Active.Count, Is.Zero);
            Assert.That(session.Shooters[0].Ammo, Is.EqualTo(1));
        }

        [Test] public void PixelHitsOpenIceAndMatchingSlotCage()
        {
            var definition = Small(); definition.SlotCages.Add(new SlotCageDefinition { Slot = 0, Color = 1, Health = 1 });
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = BoardObjectKind.Ice, Cells = new() { new(1, 0) }, Layers = new() { new(-1, 1) } });
            var session = new PuzzleSession(definition); Assert.That(session.SlotBlocked(0), Is.True);
            session.Launch(0); session.Tick(1.5f);
            Assert.That(session.SlotBlocked(0), Is.False); Assert.That(session.BoardObjects[0].Active, Is.False);
        }

        [Test] public void RearSelectionAndColorBoosterHaveRealStateEffects()
        {
            var session = new PuzzleSession(Small());
            Assert.That(session.Launch(1), Is.EqualTo(LaunchResult.Unavailable));
            Assert.That(session.Launch(1, true), Is.EqualTo(LaunchResult.Accepted));
            Assert.That(session.ClearVisibleColor(99), Is.False);
            Assert.That(session.ClearVisibleColor(1), Is.True); Assert.That(session.RemainingPixels, Is.EqualTo(1));
        }

        [Test] public void TimerLossReviveAndSnapshotRestorePreserveRunIdentity()
        {
            var definition = Small(); definition.TimeLimit = .02f;
            var session = new PuzzleSession(definition); session.Tick(.1f);
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Lost));
            var saved = session.CaptureState(); var restored = new PuzzleSession(definition); restored.RestoreState(saved);
            Assert.That(restored.RunId, Is.EqualTo(session.RunId)); Assert.That(restored.Revive(), Is.True);
            Assert.That(restored.TimeRemaining, Is.GreaterThanOrEqualTo(30)); Assert.That(restored.Revives, Is.EqualTo(1));
        }

        [Test] public void CorruptSnapshotIsRejectedBeforeMutatingTheSession()
        {
            var session = new PuzzleSession(Small()); string baseline = JsonConvert.SerializeObject(session.CaptureState());
            var bad = session.CaptureState(); bad.Queues[0][1] = bad.Queues[0][0];
            Assert.Throws<ArgumentException>(() => session.RestoreState(bad));
            Assert.That(JsonConvert.SerializeObject(session.CaptureState()), Is.EqualTo(baseline));
        }

        [TestCase(BoardObjectKind.Gate)] [TestCase(BoardObjectKind.PixelPipe)] [TestCase(BoardObjectKind.Snake)]
        [TestCase(BoardObjectKind.EggBox)] [TestCase(BoardObjectKind.Pumpkin)] [TestCase(BoardObjectKind.MultiSnake)]
        [TestCase(BoardObjectKind.BeanBox)] [TestCase(BoardObjectKind.Crossbow)] [TestCase(BoardObjectKind.ColoredCage)] [TestCase(BoardObjectKind.Matryoshka)]
        public void ColoredObjectLayersContributeGoalsAndClearInOrder(BoardObjectKind kind)
        {
            var definition = Small();
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = kind, Goal = true, Cells = new() { new(0, 1) }, Layers = new() { new(1, 1), new(2, 1) } });
            var session = new PuzzleSession(definition);
            Assert.That(session.InitialHealth, Is.EqualTo(4));
            Assert.That(session.ClearVisibleColor(1), Is.True);
            Assert.That(session.BoardObjects[0].Active, Is.True); Assert.That(session.BoardObjects[0].Color, Is.EqualTo(2));
            Assert.That(session.RemainingHealth, Is.EqualTo(2));
            Assert.That(session.ClearVisibleColor(2), Is.True); Assert.That(session.Status, Is.EqualTo(SessionStatus.Won));
        }

        [TestCase(BoardObjectKind.Ice)] [TestCase(BoardObjectKind.Biscuit)] [TestCase(BoardObjectKind.Split)]
        [TestCase(BoardObjectKind.ColorDoor)] [TestCase(BoardObjectKind.Curtain)] [TestCase(BoardObjectKind.Accordion)] [TestCase(BoardObjectKind.Mixer)]
        public void ProtectiveOverlaysOpenThroughTheirHitConditionWithoutExtraGoalAmmo(BoardObjectKind kind)
        {
            var definition = Small();
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = kind, Cells = new() { new(1, 0) }, Layers = new() { new(1, 1) } });
            var session = new PuzzleSession(definition); Assert.That(session.InitialHealth, Is.EqualTo(2));
            session.ClearVisibleColor(1); Assert.That(session.BoardObjects[0].Active, Is.False);
            session.ClearVisibleColor(2); Assert.That(session.Status, Is.EqualTo(SessionStatus.Won));
        }

        [Test] public void ExposedKeyUnlocksQueueAndCageReleasesDistinctShooter()
        {
            var definition = Small(); definition.Queues[0].Shooters.Insert(0, new ShooterDefinition { Id = 9, Kind = ShooterKind.Lock });
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = BoardObjectKind.Key, Cells = new() { new(0, 1) }, Layers = new() { new(-1, 1) } });
            definition.Objects.Add(new BoardObjectDefinition { Id = 1, Kind = BoardObjectKind.ShooterCage, Cells = new() { new(1, 1) }, Layers = new() { new(-1, 1) }, Release = new ShooterDefinition { Id = 10, Ammo = 2, Color = 1 } });
            var session = new PuzzleSession(definition);
            Assert.That(session.Shooters[9].Location, Is.EqualTo(ShooterLocation.Finished));
            Assert.That(session.Shooters[10].Location, Is.EqualTo(ShooterLocation.Queue)); Assert.That(session.QueueCount(0), Is.EqualTo(3));
            var copy = new PuzzleSession(definition); copy.RestoreState(session.CaptureState());
            Assert.That(copy.Shooters.Count, Is.EqualTo(4)); Assert.That(copy.QueueCount(0), Is.EqualTo(3));
        }

        [Test] public void MalletsUfoTotemAndAstronautRespondToTheirOwnEvents()
        {
            var definition = Small(); definition.Queues[0].Shooters.Insert(0, new ShooterDefinition { Id = 8, Kind = ShooterKind.Mallet, FeatureColor = 2 });
            definition.Queues[0].Shooters.Insert(2, new ShooterDefinition { Id = 9, Kind = ShooterKind.Totem, Counter = 1 });
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = BoardObjectKind.MusicToy, Cells = new() { new(1, 0) }, Layers = new() { new(2, 1) } });
            definition.Objects.Add(new BoardObjectDefinition { Id = 1, Kind = BoardObjectKind.Ufo, Cells = new() { new(1, 0) }, Layers = new() { new(1, 1) } });
            var session = new PuzzleSession(definition); Assert.That(session.Launch(8), Is.EqualTo(LaunchResult.Accepted));
            Assert.That(session.BoardObjects[0].Active, Is.False);
            session.Launch(0); session.Tick(1.5f);
            Assert.That(session.BoardObjects[1].Active, Is.False); Assert.That(session.Shooters[9].Location, Is.EqualTo(ShooterLocation.Finished));
            definition = Small(); definition.Queues[0].Shooters[0].Astronaut = true;
            definition.Objects.Add(new BoardObjectDefinition { Id = 0, Kind = BoardObjectKind.Spaceship, Cells = new() { new(1, 0) }, Layers = new() { new(1, 1) } });
            session = new PuzzleSession(definition); session.Launch(0); Assert.That(session.BoardObjects[0].Active, Is.False);
        }

        [Test] public void DoorReturnsWrongColorEarlyAndMatchingPassageDamagesIt()
        {
            var definition = Small(); definition.ConveyorDoors.Add(new ConveyorDoorDefinition { Color = 2, Health = 1, Progress = .001f });
            var session = new PuzzleSession(definition); session.Launch(0); session.Tick(.02f);
            Assert.That(session.Shooters[0].Location, Is.EqualTo(ShooterLocation.Waiting)); Assert.That(session.DoorHealth(0), Is.EqualTo(1));
            session.Launch(1); session.Tick(.02f); Assert.That(session.DoorHealth(0), Is.Zero);
        }

        [Test] public void UnicornUsesWaitingAmmoAndCannotRepeatWithoutCharging()
        {
            var session = new PuzzleSession(Small()); var snapshot = session.CaptureState();
            snapshot.HitCount = 100; snapshot.Queues[0] = new[] { 1 }; snapshot.Waiting[0] = 0;
            snapshot.Shooters[0].Location = ShooterLocation.Waiting; session.RestoreState(snapshot);
            Assert.That(session.UseUnicorn(), Is.True); Assert.That(session.Shooters[0].Ammo, Is.Zero);
            Assert.That(session.Waiting[0], Is.Null); Assert.That(session.RemainingHealth, Is.EqualTo(1));
            Assert.That(session.UnicornCharge, Is.Zero); Assert.That(session.UseUnicorn(), Is.False);
        }

        [Test] public void EntirelyObjectBasedSourceLevelHasGoalsAndRestores()
        {
            var level = Catalog().GetLevel(2100); Assert.That(level.Pixels, Is.Empty);
            var session = new PuzzleSession(level); Assert.That(session.InitialHealth, Is.GreaterThan(0));
            var restored = new PuzzleSession(level); restored.RestoreState(session.CaptureState()); Assert.That(restored.RemainingHealth, Is.EqualTo(session.RemainingHealth));
        }

        [Test] public void SnapshotCannotClaimAWinWithGoalsStillOnBoard()
        {
            var session = new PuzzleSession(Small()); var saved = session.CaptureState(); saved.Status = SessionStatus.Won;
            Assert.Throws<ArgumentException>(() => session.RestoreState(saved)); Assert.That(session.Status, Is.EqualTo(SessionStatus.Playing));
        }

        [TestCase(8)] [TestCase(14)]
        public void SurpriseAndConnectedSourceLevelsCanBeSolvedThroughNormalLaunchRules(int number)
        {
            var campaign = Catalog(); var session = new PuzzleSession(campaign.GetLevel(number - 1), campaign.LoopSeconds, campaign.ShotInterval);
            for (int tick = 0; tick < 16000 && session.Status == SessionStatus.Playing; tick++)
            {
                if (tick % 7 == 0 && session.Active.Count < session.ConveyorCapacity)
                {
                    int selected = PixelFlowSmoke.Choose(session); if (selected >= 0) session.Launch(selected);
                }
                session.Tick(.025f);
            }
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Won), "Source " + number + ": " + session.LossReason);
        }
    }
}
