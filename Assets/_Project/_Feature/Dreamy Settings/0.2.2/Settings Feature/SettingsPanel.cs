using System;
using Cysharp.Threading.Tasks;
using Dreamy.Settings;
using Dreamy.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Feature.Settings.Integration
{
    public sealed class SettingsPanel : UIPanel, ISettingsView
    {
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Button gdprButton;
        [SerializeField] private Button restorePurchasesButton;
        [SerializeField] private Button openRateUsButton;
        [SerializeField] private Button closeButton;


        public override bool CanBack => true;

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action<bool> HapticsEnabledChanged;
        public event Action GdprRequested;
        public event Action RestorePurchasesRequested;
        public event Action OpenRateUsRequested;
        public event Action CloseRequested;

        private void OnEnable()
        {
            musicToggle.onValueChanged.AddListener(RequestMusicToggle);
            sfxToggle.onValueChanged.AddListener(RequestSfxToggle);
            if (hapticsToggle != null) hapticsToggle.onValueChanged.AddListener(RequestHapticsToggle);
            gdprButton.onClick.AddListener(RequestGdpr);
            restorePurchasesButton.onClick.AddListener(RequestRestorePurchases);
            openRateUsButton.onClick.AddListener(RequestOpenRateUs);
            closeButton.onClick.AddListener(RequestClose);
        }

        protected override void OnDisable()
        {
            musicToggle.onValueChanged.RemoveListener(RequestMusicToggle);
            sfxToggle.onValueChanged.RemoveListener(RequestSfxToggle);
            if (hapticsToggle != null) hapticsToggle.onValueChanged.RemoveListener(RequestHapticsToggle);
            gdprButton.onClick.RemoveListener(RequestGdpr);
            restorePurchasesButton.onClick.RemoveListener(RequestRestorePurchases);
            openRateUsButton.onClick.RemoveListener(RequestOpenRateUs);
            closeButton.onClick.RemoveListener(RequestClose);
            base.OnDisable();
        }

        public void Render(SettingsViewState state)
        {
            musicToggle.SetIsOnWithoutNotify(state.MusicVolume > 0f);
            sfxToggle.SetIsOnWithoutNotify(state.SfxVolume > 0f);
            if (hapticsToggle != null)
            {
                hapticsToggle.SetIsOnWithoutNotify(state.HapticsEnabled);
                hapticsToggle.interactable = state.CanSetHaptics;
            }
            gdprButton.gameObject.SetActive(state.CanShowGdprConsent);
            restorePurchasesButton.gameObject.SetActive(state.CanRestorePurchases);
            openRateUsButton.gameObject.SetActive(state.CanRequestReview);
        }

        public void SetPlatformActionsInteractable(bool interactable)
        {
            gdprButton.interactable = interactable;
            restorePurchasesButton.interactable = interactable;
        }

        public void Close() => Hide().Forget();

        private void RequestMusicToggle(bool isOn) => MusicVolumeChanged?.Invoke(isOn ? 1f : 0f);
        private void RequestSfxToggle(bool isOn) => SfxVolumeChanged?.Invoke(isOn ? 1f : 0f);
        private void RequestHapticsToggle(bool isOn) => HapticsEnabledChanged?.Invoke(isOn);
        private void RequestGdpr() => GdprRequested?.Invoke();
        private void RequestRestorePurchases() => RestorePurchasesRequested?.Invoke();
        private void RequestOpenRateUs() => OpenRateUsRequested?.Invoke();
        private void RequestClose() => CloseRequested?.Invoke();
    }
}
