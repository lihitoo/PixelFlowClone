using System;
using Cysharp.Threading.Tasks;
using Dreamy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Template.Demo
{
    public sealed class FoundationDemoPanel : UIPanel, IFoundationDemoView
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text healthText;

        [Header("Buttons")]
        [SerializeField] private Button addScoreButton;
        [SerializeField] private Button damageButton;
        [SerializeField] private Button healButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button closeButton;

        public override bool CanBack => true;

        public event Action AddScoreRequested;
        public event Action DamageRequested;
        public event Action HealRequested;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action CloseRequested;
        public event Action Destroyed;

        private void OnEnable()
        {
            addScoreButton.onClick.AddListener(OnAddScore);
            damageButton.onClick.AddListener(OnDamage);
            healButton.onClick.AddListener(OnHeal);
            saveButton.onClick.AddListener(OnSave);
            loadButton.onClick.AddListener(OnLoad);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        protected override void OnDisable()
        {
            addScoreButton.onClick.RemoveListener(OnAddScore);
            damageButton.onClick.RemoveListener(OnDamage);
            healButton.onClick.RemoveListener(OnHeal);
            saveButton.onClick.RemoveListener(OnSave);
            loadButton.onClick.RemoveListener(OnLoad);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnClose);
            base.OnDisable();
        }

        public void Close() => Hide().Forget();
        public void SetSaveInteractable(bool value) => saveButton.interactable = value;

        public void SetStatus(string value) => statusText.text = value;
        public void SetScore(int value) => scoreText.text = $"Score: {value}";
        public void SetHealth(float value) => healthText.text = $"Health: {value:0}";

        private void OnAddScore() => AddScoreRequested?.Invoke();
        private void OnDamage() => DamageRequested?.Invoke();
        private void OnHeal() => HealRequested?.Invoke();
        private void OnSave() => SaveRequested?.Invoke();
        private void OnLoad() => LoadRequested?.Invoke();
        private void OnClose() => CloseRequested?.Invoke();

        protected override void OnDestroy()
        {
            Destroyed?.Invoke();
            base.OnDestroy();
        }
    }
}
