using System;
using Cysharp.Threading.Tasks;
using Dreamy.Settings;
using Dreamy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dreamy.Feature.Settings.Integration
{
    public enum SettingsCommand { Privacy, Feedback, Restore, SaveProgress, Restart, GiveUp, Instagram, DeleteAccount }

    /// <summary>Passive authored settings view; persistence and platform actions belong to its presenter.</summary>
    public sealed class SettingsPanel : UIPanel, ISettingsView
    {
        [Serializable] private sealed class CommandButton
        {
            public SettingsCommand Command;
            public Button Button;
            [NonSerialized] public UnityAction Listener;
        }
        [Serializable] private sealed class Preference
        {
            public Toggle Toggle;
            public RectTransform Handle;
            public TMP_Text Label;
        }
        [SerializeField] private SliderCommit musicSlider;
        [SerializeField] private SliderCommit soundSlider;
        [SerializeField] private Preference haptics;
        [SerializeField] private Preference notifications;
        [SerializeField] private Preference smoothMotion;
        [SerializeField] private CommandButton[] commands;
        [SerializeField] private GameObject gameplayActions;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text userIdText;
        [SerializeField] private GameObject notice;
        [SerializeField] private TMP_Text noticeTitle;
        [SerializeField] private TMP_Text noticeDetail;
        [SerializeField] private Button noticePrimary;
        [SerializeField] private TMP_Text noticePrimaryLabel;
        [SerializeField] private Button noticeClose;
        private Action noticeAction, noticeBack;

        public override bool CanBack => true;
        public override bool CanCache => true;
        public override UILayer Layer => UILayer.Popup;
        public Slider MusicSlider => musicSlider.Slider;
        public Slider SoundSlider => soundSlider.Slider;
        public Toggle HapticsToggle => haptics.Toggle;
        public Toggle NotificationsToggle => notifications.Toggle;
        public Toggle SmoothMotionToggle => smoothMotion.Toggle;
        public Button CloseButton => closeButton;
        public bool IsNoticeVisible => notice.activeSelf;
        public Button NoticePrimary => noticePrimary;
        public Button NoticeClose => noticeClose;
        public Button ButtonFor(SettingsCommand command) => Array.Find(commands, item => item.Command == command)?.Button;
        public event Action<float> MusicVolumeChanged, SfxVolumeChanged;
        public event Action<bool> HapticsEnabledChanged, NotificationsEnabledChanged, SmoothMotionChanged;
        public event Action GdprRequested, RestorePurchasesRequested, OpenRateUsRequested, CloseRequested;
        public event Action<SettingsCommand> CommandRequested;

        private void OnEnable()
        {
            musicSlider.Changed += RequestMusic;
            soundSlider.Changed += RequestSound;
            haptics.Toggle.onValueChanged.AddListener(RequestHaptics);
            notifications.Toggle.onValueChanged.AddListener(RequestNotifications);
            smoothMotion.Toggle.onValueChanged.AddListener(RequestSmoothMotion);
            closeButton.onClick.AddListener(Back);
            noticePrimary.onClick.AddListener(ConfirmNotice);
            noticeClose.onClick.AddListener(CloseNotice);
            foreach (var item in commands)
            {
                item.Listener ??= () => RequestCommand(item.Command);
                item.Button.onClick.AddListener(item.Listener);
            }
        }
        protected override void OnDisable()
        {
            musicSlider.Changed -= RequestMusic;
            soundSlider.Changed -= RequestSound;
            haptics.Toggle.onValueChanged.RemoveListener(RequestHaptics);
            notifications.Toggle.onValueChanged.RemoveListener(RequestNotifications);
            smoothMotion.Toggle.onValueChanged.RemoveListener(RequestSmoothMotion);
            closeButton.onClick.RemoveListener(Back);
            noticePrimary.onClick.RemoveListener(ConfirmNotice);
            noticeClose.onClick.RemoveListener(CloseNotice);
            foreach (var item in commands) item.Button.onClick.RemoveListener(item.Listener);
            noticeAction = noticeBack = null; notice.SetActive(false);
            base.OnDisable();
        }
        public void Render(SettingsViewState state)
        {
            musicSlider.Render(state.MusicVolume);
            soundSlider.Render(state.SfxVolume);
            RenderPreference(haptics, state.HapticsEnabled);
        }
        public void RenderContext(bool notificationEnabled, bool smoothEnabled, bool inLevel, string version, string userId)
        {
            RenderPreference(notifications, notificationEnabled);
            RenderPreference(smoothMotion, smoothEnabled);
            gameplayActions.SetActive(inLevel);
            versionText.text = version; userIdText.text = userId;
        }
        private static void RenderPreference(Preference preference, bool enabled)
        {
            preference.Toggle.SetIsOnWithoutNotify(enabled);
            preference.Handle.anchoredPosition = new Vector2(enabled ? 62 : -62, 0);
            preference.Label.text = enabled ? "ON" : "OFF";
            preference.Label.rectTransform.anchoredPosition = new Vector2(enabled ? -38 : 38, 0);
        }
        public void SetPlatformActionsInteractable(bool interactable)
        {
            foreach (var item in commands) item.Button.interactable = interactable;
        }
        public void ShowNotice(string title, string detail, string primary, Action action, Action back = null)
        {
            noticeTitle.text = title; noticeDetail.text = detail; noticePrimaryLabel.text = primary;
            noticeAction = action; noticeBack = back; notice.SetActive(true);
        }
        public void Back() { if (notice.activeSelf) CloseNotice(); else CloseRequested?.Invoke(); }
        public void Close() => Hide().Forget();
        private void ConfirmNotice() { var action = noticeAction; notice.SetActive(false); noticeAction = noticeBack = null; action?.Invoke(); }
        private void CloseNotice() { var back = noticeBack; notice.SetActive(false); noticeAction = noticeBack = null; back?.Invoke(); }
        private void RequestMusic(float value) => MusicVolumeChanged?.Invoke(value);
        private void RequestSound(float value) => SfxVolumeChanged?.Invoke(value);
        private void RequestHaptics(bool value) => HapticsEnabledChanged?.Invoke(value);
        private void RequestNotifications(bool value) => NotificationsEnabledChanged?.Invoke(value);
        private void RequestSmoothMotion(bool value) => SmoothMotionChanged?.Invoke(value);
        private void RequestCommand(SettingsCommand command)
        {
            if (CommandRequested != null) CommandRequested(command);
            else if (command == SettingsCommand.Privacy) GdprRequested?.Invoke();
            else if (command == SettingsCommand.Restore) RestorePurchasesRequested?.Invoke();
        }
    }
}
