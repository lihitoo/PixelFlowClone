using System;
using Dreamy.Datasave;
using Dreamy.UI;
using UnityEngine;

namespace Dreamy.Template.Demo
{
    public interface IFoundationDemoView
    {
        event Action AddScoreRequested;
        event Action DamageRequested;
        event Action HealRequested;
        event Action SaveRequested;
        event Action LoadRequested;
        event Action CloseRequested;
        void SetStatus(string value);
        void SetScore(int value);
        void SetHealth(float value);
        void SetSaveInteractable(bool value);
        void Close();
    }

    public sealed class FoundationDemoRoot : IPanelPresenter
    {
        private readonly IDatasaveService datasave;
        private readonly IFoundationDemoView panel;
        private readonly Func<TemplateConfig> getConfig;
        private bool canSave;
        private bool isBound;
        private TemplateSave saveData;
        private int score;
        private float health = 100f;
        private bool disposed;

        public FoundationDemoRoot(
            IFoundationDemoView panel,
            IDatasaveService datasave,
            Func<TemplateConfig> getConfig)
        {
            this.panel = panel ?? throw new ArgumentNullException(nameof(panel));
            this.datasave = datasave ?? throw new ArgumentNullException(nameof(datasave));
            this.getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        }

        public void Show()
        {
            if (disposed || isBound) return;
            TemplateConfig config = getConfig();
            score = config.StartingScore;
            panel.SetSaveInteractable(false);

            panel.AddScoreRequested += AddScore;
            panel.DamageRequested += Damage;
            panel.HealRequested += Heal;
            panel.SaveRequested += Save;
            panel.LoadRequested += Load;
            panel.CloseRequested += Close;
            RefreshPanel();
            isBound = true;
            if (TryLoad())
            {
                saveData.DemoOpenCount++;
                Persist($"Demo opened | count={saveData.DemoOpenCount} | starting score={config.StartingScore}");
            }
        }

        private void AddScore()
        {
            score += 10;
            RefreshPanel();
        }

        private void Damage()
        {
            health = Mathf.Max(0f, health - 10f);
            RefreshPanel();
        }

        private void Heal()
        {
            health = Mathf.Min(100f, health + 10f);
            RefreshPanel();
        }

        private void Save()
        {
            if (!canSave) return;
            saveData.CurrentScore = score;
            saveData.BestScore = Mathf.Max(saveData.BestScore, score);
            Persist($"Saved | score={saveData.CurrentScore} | best score={saveData.BestScore}");
        }

        private void Persist(string successStatus)
        {
            try
            {
                datasave.Save(saveData);
                panel.SetStatus(successStatus);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Foundation demo save failed: {exception}");
                panel.SetStatus("Save failed. Your score is still available; try Save again.");
            }
        }

        private void Load() => TryLoad();

        private bool TryLoad()
        {
            try
            {
                TemplateSave loaded = datasave.Load<TemplateSave>();
                if (!loaded.IsInitialized)
                {
                    loaded.CurrentScore = getConfig().StartingScore;
                    loaded.BestScore = Mathf.Max(loaded.BestScore, loaded.CurrentScore);
                    loaded.IsInitialized = true;
                }
                saveData = loaded;
                score = loaded.CurrentScore;
                canSave = true;
                panel.SetSaveInteractable(true);
                RefreshPanel();
                panel.SetStatus($"Loaded | score={loaded.CurrentScore} | best score={loaded.BestScore}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Foundation demo load failed: {exception}");
                canSave = false;
                panel.SetSaveInteractable(false);
                panel.SetStatus("Save could not be loaded. Saving is disabled to protect your data. Try Load again.");
                return false;
            }
        }

        private void RefreshPanel()
        {
            panel.SetScore(score);
            panel.SetHealth(health);
        }

        private void Close() => panel.Close();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            panel.AddScoreRequested -= AddScore;
            panel.DamageRequested -= Damage;
            panel.HealRequested -= Heal;
            panel.SaveRequested -= Save;
            panel.LoadRequested -= Load;
            panel.CloseRequested -= Close;
        }
    }
}
