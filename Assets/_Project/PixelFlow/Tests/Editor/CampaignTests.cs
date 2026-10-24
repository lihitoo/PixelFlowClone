using System;
using System.IO;
using Dreamy.Datasave;
using Newtonsoft.Json;
using NUnit.Framework;
using PixelFlow.Rules;
using UnityEditor;
using UnityEngine;

namespace PixelFlow.Tests
{
    public sealed class CampaignTests
    {
        private static PixelFlowCampaignConfig Campaign()
        {
            var source = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/PixelFlow/Resources/PixelFlow/pixelFlowCampaign.json");
            Assert.That(source, Is.Not.Null);
            var campaign = JsonConvert.DeserializeObject<PixelFlowCampaignConfig>(source.text);
            campaign.Initialize("pixelFlowCampaign"); return campaign;
        }

        [Test] public void CampaignPreservesOriginalEarlyLevelCountsAndPalette()
        {
            var campaign = Campaign();
            Assert.That(campaign.Levels.Count, Is.EqualTo(7)); Assert.That(campaign.Colors.Count, Is.EqualTo(34));
            Assert.That(campaign.Levels[0].Pixels.Count, Is.EqualTo(180));
            Assert.That(campaign.Levels[1].Pixels.Count, Is.EqualTo(400));
            Assert.That(campaign.Levels[0].Queues[0].Shooters[0].Ammo, Is.EqualTo(40));
            Assert.That(campaign.Colors.Find(x => x.Id == 5).Name, Is.EqualTo("Pink"));
            Assert.That(campaign.Colors.Find(x => x.Id == 8).Name, Is.EqualTo("Turquoise"));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void OriginalLevelCanBeClearedThroughThePublicRules(int index)
        {
            var campaign = Campaign();
            var session = new PuzzleSession(campaign.Levels[index], campaign.LoopSeconds, campaign.ShotInterval);
            int shots = 0; session.Shot += _ => shots++;
            for (int tick = 0; tick < 12000 && session.Status == SessionStatus.Playing; tick++)
            {
                if (tick % 7 == 0 && session.Active.Count < session.Level.ConveyorLimit)
                {
                    int selected = PixelFlowSmoke.Choose(session);
                    if (selected >= 0) session.Launch(selected);
                }
                session.Tick(.025f);
            }
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Won), "Source level " + (index + 1) + ": " + session.LossReason);
            Assert.That(shots, Is.EqualTo(campaign.Levels[index].Pixels.Count));
            Assert.That(session.RemainingPixels, Is.Zero);
        }

        [Test] public void CompletingAndReloadingPreservesUnlockWithoutDuplicateCompletion()
        {
            string scope = "PixelFlowTests_" + Guid.NewGuid().ToString("N");
            string directory = Path.Combine(Application.persistentDataPath, scope);
            try
            {
                var first = new ProgressStore(new DatasaveService(new DatasaveOptions { DirectoryName = scope }), 7);
                first.Complete(0); first.Complete(0); first.ToggleSound();
                var second = new ProgressStore(new DatasaveService(new DatasaveOptions { DirectoryName = scope }), 7);
                Assert.That(second.Data.UnlockedLevel, Is.EqualTo(1));
                Assert.That(second.Data.CompletedLevels, Is.EquivalentTo(new[] { 0 }));
                Assert.That(second.Data.SoundEnabled, Is.False);
                second.Complete(6); Assert.That(second.Data.UnlockedLevel, Is.EqualTo(6));
            }
            finally
            {
                // Only this fixture's newly generated directory can be removed.
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
