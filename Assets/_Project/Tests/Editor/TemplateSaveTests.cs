using System;
using System.IO;
using Dreamy.Datasave;
using Dreamy.Template.Demo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Dreamy.Template.Tests
{
    public sealed class TemplateSaveTests
    {
        [TestCase(42)]
        [TestCase(0)]
        public void SaveAndLoad_PreservesDemoState(int score)
        {
            string directory = Path.Combine(Path.GetTempPath(), "dreamy-template-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var options = new DatasaveOptions { DirectoryName = directory };
                var service = new DatasaveService(options);
                var save = new TemplateSave
                {
                    DemoOpenCount = 7,
                    CurrentScore = score,
                    BestScore = 99,
                    IsInitialized = true
                };
                service.Save(save);

                TemplateSave loaded = new DatasaveService(options).Load<TemplateSave>();
                Assert.That(loaded.DemoOpenCount, Is.EqualTo(7));
                Assert.That(loaded.CurrentScore, Is.EqualTo(score));
                Assert.That(loaded.BestScore, Is.EqualTo(99));
                Assert.That(loaded.IsInitialized, Is.True);
                JObject envelope = JObject.Parse(File.ReadAllText(Path.Combine(directory, save.SaveKey + ".json")));
                Assert.That(envelope.Value<int>("DataVersion"), Is.EqualTo(save.Version));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void LoadFutureVersion_RejectsSaveAndBackup_WithoutOverwritingEither()
        {
            string directory = Path.Combine(Path.GetTempPath(), "dreamy-template-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "TemplateSave.json");
                string contents = JsonConvert.SerializeObject(new
                {
                    FormatVersion = 1,
                    DataVersion = new TemplateSave().Version + 1,
                    DataType = typeof(TemplateSave).AssemblyQualifiedName,
                    Payload = JsonConvert.SerializeObject(new { CurrentScore = 42, IsInitialized = true })
                });
                File.WriteAllText(path, contents);
                File.WriteAllText(path + ".bak", contents);
                var service = new DatasaveService(new DatasaveOptions { DirectoryName = directory });

                Assert.Throws<DatasaveException>(() => service.Load<TemplateSave>());
                service.SaveAll();
                Assert.That(File.ReadAllText(path), Is.EqualTo(contents));
                Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(contents));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void NewSave_IsNotInitialized_UntilDemoSeedsIt()
        {
            Assert.That(new TemplateSave().IsInitialized, Is.False);
        }

        [TestCase("{\"startingCoins\":36}")]
        [TestCase("{\"startingScore\":36}")]
        public void Config_AcceptsLegacyAndCurrentScoreName(string json)
        {
            TemplateConfig config = JsonConvert.DeserializeObject<TemplateConfig>(json);
            Assert.That(config.StartingScore, Is.EqualTo(36));
            Assert.That(JObject.Parse(JsonConvert.SerializeObject(config))["startingCoins"], Is.Null);
        }
    }

    public sealed class FoundationDemoPersistenceTests
    {
        private sealed class View : IFoundationDemoView
        {
            public event Action AddScoreRequested;
            public event Action DamageRequested;
            public event Action HealRequested;
            public event Action SaveRequested;
            public event Action LoadRequested;
            public event Action CloseRequested;
            public string Status;
            public int Score;
            public bool SaveEnabled;
            public bool Closed;
            public void SetStatus(string value) => Status = value;
            public void SetScore(int value) => Score = value;
            public void SetHealth(float value) { }
            public void SetSaveInteractable(bool value) => SaveEnabled = value;
            public void Close() => Closed = true;
            public void AddScore() => AddScoreRequested?.Invoke();
            public void Save() => SaveRequested?.Invoke();
            public void Load() => LoadRequested?.Invoke();
            public void RequestClose() => CloseRequested?.Invoke();
            public void Damage() => DamageRequested?.Invoke();
            public void Heal() => HealRequested?.Invoke();
        }

        private sealed class Saves : IDatasaveService
        {
            public TemplateSave Data = new TemplateSave { CurrentScore = 42, BestScore = 99, IsInitialized = true };
            public bool FailLoad;
            public bool FailSave;
            public int LoadCalls;
            public int SaveCalls;
            public T Load<T>(string key = null) where T : SaveData, new()
            {
                LoadCalls++;
                if (FailLoad) throw new IOException("Unreadable save");
                return (T)(SaveData)Data;
            }
            public void Save<T>(T data, string key = null) where T : SaveData
            {
                SaveCalls++;
                if (FailSave) throw new IOException("Disk full");
            }
            public void SaveAll() { }
            public bool Exists(string key) => true;
            public void Delete(string key) => Assert.Fail("Must not delete existing saves");
            public void DeleteAll() => Assert.Fail("Must not delete existing saves");
        }

        [Test]
        public void LoadFailure_LeavesDemoUsable_BlocksWrites_AndAllowsRecovery()
        {
            var view = new View();
            var saves = new Saves { FailLoad = true };
            using var presenter = new FoundationDemoRoot(view, saves, () => new TemplateConfig { StartingScore = 36 });
            Assert.DoesNotThrow(presenter.Show);
            Assert.That(view.SaveEnabled, Is.False);
            view.AddScore();
            Assert.That(view.Score, Is.EqualTo(46));
            view.Save();
            Assert.That(saves.SaveCalls, Is.Zero);
            view.RequestClose();
            Assert.That(view.Closed, Is.True);

            saves.FailLoad = false;
            view.Load();
            Assert.That(view.Score, Is.EqualTo(42));
            Assert.That(view.SaveEnabled, Is.True);
            view.Save();
            Assert.That(saves.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public void SaveFailure_PreservesCurrentScore_AndCanBeRetried()
        {
            var view = new View();
            var saves = new Saves { FailSave = true };
            using var presenter = new FoundationDemoRoot(view, saves, () => new TemplateConfig());
            Assert.DoesNotThrow(presenter.Show);
            view.AddScore();
            Assert.DoesNotThrow(view.Save);
            Assert.That(view.Score, Is.EqualTo(52));
            Assert.That(view.Status, Does.Contain("Save failed"));
            saves.FailSave = false;
            view.Save();
            Assert.That(saves.Data.CurrentScore, Is.EqualTo(52));
            Assert.That(view.Status, Does.StartWith("Saved"));
        }

        [Test]
        public void ReloadFailure_BlocksSavingPreviousData_AndPreservesDisplayedScore()
        {
            var view = new View();
            var saves = new Saves();
            using var presenter = new FoundationDemoRoot(view, saves, () => new TemplateConfig());
            presenter.Show();
            view.AddScore();
            saves.FailLoad = true;
            view.Load();
            view.Save();
            Assert.That(view.Score, Is.EqualTo(52));
            Assert.That(view.SaveEnabled, Is.False);
            Assert.That(saves.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedShow_AndDisposedPresenter_DoNotDuplicateOrRetainHandlers()
        {
            var view = new View();
            var saves = new Saves();
            var presenter = new FoundationDemoRoot(view, saves, () => new TemplateConfig());
            presenter.Show();
            presenter.Show();
            Assert.That(saves.LoadCalls, Is.EqualTo(1));
            Assert.That(saves.Data.DemoOpenCount, Is.EqualTo(1));
            view.AddScore();
            Assert.That(view.Score, Is.EqualTo(52));
            presenter.Dispose();
            presenter.Dispose();
            view.AddScore();
            view.Save();
            view.Load();
            view.RequestClose();
            Assert.That(view.Score, Is.EqualTo(52));
            Assert.That(saves.SaveCalls, Is.EqualTo(1));
            Assert.That(saves.LoadCalls, Is.EqualTo(1));
            Assert.That(view.Closed, Is.False);
        }
    }

}
