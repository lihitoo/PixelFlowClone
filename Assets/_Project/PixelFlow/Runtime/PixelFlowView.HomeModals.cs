using System;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PixelFlow
{
    public sealed partial class PixelFlowView
    {
        private GameObject homeModal;
        private RectTransform homeModalCard, homeModalContent;
        private TMP_Text homeModalTitle;
        private Action homeModalBack;
        public Button HomeModalClose { get; private set; }
        public Button HomeModalPrimary { get; private set; }
        public Slider HomeMusicSlider { get; private set; }
        public Slider HomeSoundSlider { get; private set; }
        private void BuildHomeModal()
        {
            homeModal = Rect(safe, "Source Home Modal", 0, 0, 0, 0).gameObject; Stretch((RectTransform)homeModal.transform);
            Image(homeModal.transform, "Modal Backdrop", null, new Color(.01f, .025f, .07f, .78f), 0, 0, 0, 0, stretch: true, blocks: true);
            homeModalCard = Rect(homeModal.transform, "Source Popup Card", 630, 790, 0, 0);
            Image(homeModalCard, "Original Popup", HomeSprite("PopupHeader"), Color.white, 0, 0, 0, 0, stretch: true, blocks: true);
            homeModalTitle = HomeText(homeModalCard, "Title", "", 44, 470, 85, 0, -68, new Vector2(.5f, 1));
            HomeModalClose = HomeButton(homeModalCard, "Close Home Modal", "Close", 85, 85, -15, -12, () => homeModalBack?.Invoke(), new Vector2(1, 1));
            homeModalContent = Rect(homeModalCard, "Modal Content", 0, 0, 0, 0); Stretch(homeModalContent); homeModalContent.offsetMax = new Vector2(0, -135);
            homeModal.SetActive(false);
        }
        private void BeginHomeModal(string title, float height, Action close)
        {
            HidePages(true); ShowHomeChrome(false); overlay.SetActive(false);
            homeModalBack = close; backAction = close;
            for (int i = homeModalContent.childCount - 1; i >= 0; i--)
            {
                var old = homeModalContent.GetChild(i).gameObject; old.SetActive(false); Object.Destroy(old);
            }
            HomeModalPrimary = null; HomeMusicSlider = HomeSoundSlider = null; HomeModalClose.gameObject.SetActive(true);
            homeModalCard.sizeDelta = new Vector2(630, height); homeModalTitle.text = title;
            homeModal.SetActive(true); homeModal.transform.SetAsLastSibling();
        }
        private Button ModalButton(string name, string label, float y, Action action, bool enabled = true)
        {
            var button = Button(homeModalContent, name, label, 440, 82, 0, y, HomeSprite("Play"), action, 29);
            button.interactable = enabled; return button;
        }
        public void ShowLives(ProgressStore store, Action close)
        {
            BeginHomeModal("Lives", 780, close);
            long now = ProgressStore.UtcSeconds;
            var state = HomeLifeRules.Advance(store.Data.Lives, now, homeTuning.MaximumHearts, homeTuning.HeartIntervalSeconds);
            bool full = state.Hearts == homeTuning.MaximumHearts, unlimited = state.UnlimitedUntilUtc > now;
            HomeImage(homeModalContent, "Original Heart", unlimited ? "HeartInfinite" : "Heart", 160, 160, 0, 150);
            HomeText(homeModalContent, "Lives Detail", unlimited ? "Unlimited lives" : state.Hearts + " / " + homeTuning.MaximumHearts + " lives", 32, 530, 60, 0, 45);
            int seconds = HomeLifeRules.SecondsToNext(state, now, homeTuning.HeartIntervalSeconds);
            HomeText(homeModalContent, "Regeneration", full ? "Your lives are full!" : unlimited ? "Keep the flow going!" : "Next life in " + string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60), 26, 530, 70, 0, -30);
            HomeModalPrimary = ModalButton("Refill Lives", "Refill  " + homeTuning.HeartRefillCoins + " coins", -140, () => RefillRequested?.Invoke(), !full && !unlimited && store.Data.Coins >= homeTuning.HeartRefillCoins);
            if (!full && store.Data.Coins < homeTuning.HeartRefillCoins)
                ModalButton("Get Coins", "Get coins", -235, () => PageRequested?.Invoke(FlowPage.Shop));
        }
        public void ShowHomeDestination(FlowPage flow, ProgressStore store, Action close)
        {
            string title = flow switch { FlowPage.Pass => "Pixel Pass", FlowPage.FireQuest => "Fire Quest", FlowPage.Arena => "Pixel Arena", FlowPage.NoAds => "No Ads", _ => "Leaderboard" };
            string icon = flow switch { FlowPage.Pass => "BattlePass", FlowPage.FireQuest => "FireQuest", FlowPage.Arena => "Arena", FlowPage.NoAds => "NoAds", _ => "Leaderboard" };
            bool locked = flow == FlowPage.Rank && store.Data.UnlockedLevel + 1 < homeTuning.LeaderboardLevel;
            BeginHomeModal(title, 760, close); HomeImage(homeModalContent, "Destination Icon", locked ? "Lock" : icon, 155, 155, 0, 145);
            string text = locked ? "Unlocks at Level " + homeTuning.LeaderboardLevel : flow == FlowPage.NoAds ? "Remove intrusive ads.\nPurchase options are unavailable right now." : "Unable to load " + title + ".\nPlease try again later.";
            HomeText(homeModalContent, "Destination State", text, 29, 530, 150, 0, -25);
            HomeModalPrimary = ModalButton("Destination Retry", locked ? "Keep playing" : "Retry", -190,
                () => { if (locked) close(); else ShowHomeDestination(flow, store, close); });
        }
        public void ShowSourceSettings(ProgressStore store, Action close)
        {
            BeginHomeModal("Settings", 1050, close); var data = store.Data;
            HomeMusicSlider = SoundSlider("Music", 300, data.MusicEnabled ? data.MusicVolume : 0);
            HomeSoundSlider = SoundSlider("Sound", 180, data.SoundEnabled ? data.SoundVolume : 0);
            Action commit = () => AudioLevelsRequested?.Invoke(HomeMusicSlider.value, HomeSoundSlider.value);
            HomeMusicSlider.gameObject.AddComponent<HomeSliderCommit>().Initialize(commit);
            HomeSoundSlider.gameObject.AddComponent<HomeSliderCommit>().Initialize(commit);
            Preference("Haptic", 70, data.HapticsEnabled, 0);
            Preference("Notifications", -20, data.NotificationsEnabled, 1);
            Preference("Smooth Motion", -110, data.SmoothMotion, 2);
            SmallSetting("Help", -138, -220, () => { ShowHelp(() => ShowSourceSettings(store, close)); });
            SmallSetting("Save Progress", 138, -220, () => Information("Progress", "Your progress is saved on this device.\n\nCloud sign-in is unavailable right now.", () => ShowSourceSettings(store, close)));
            SmallSetting("Privacy", -138, -300, () => Information("Privacy", "This clone keeps gameplay progress locally.\n\nNo account is currently connected.", () => ShowSourceSettings(store, close)));
            SmallSetting("Restore", 138, -300, () => RestorePurchasesRequested?.Invoke());
            HomeText(homeModalContent, "Version", "Pixel Flow Clone  |  " + Application.unityVersion, 16, 520, 35, 0, -375);
        }
        private Slider SoundSlider(string title, float y, float value)
        {
            HomeText(homeModalContent, title + " Label", title, 30, 180, 60, -170, y);
            var rect = Rect(homeModalContent, title + " Slider", 310, 58, 100, y);
            var background = Image(rect, "Original Slider Background", HomeSprite("SliderBackground"), Color.white, 0, 0, 0, 0, stretch: true, blocks: true);
            var fillArea = Rect(rect, "Fill Area", 0, 0, 0, 0); Stretch(fillArea); fillArea.offsetMin = new Vector2(12, 5); fillArea.offsetMax = new Vector2(-12, -5);
            var fillImage = Image(fillArea, "Original Slider Fill", HomeSprite("SliderFill"), Color.white, 0, 0, 0, 0, stretch: true);
            var handles = Rect(rect, "Handle Area", 0, 0, 0, 0); Stretch(handles); handles.offsetMin = new Vector2(20, 0); handles.offsetMax = new Vector2(-20, 0);
            var handle = HomeImage(handles, "Original Slider Handle", "SliderHandle", 68, 68, 0, 0);
            var slider = rect.gameObject.AddComponent<Slider>(); slider.fillRect = fillImage.rectTransform; slider.handleRect = handle.rectTransform;
            slider.targetGraphic = background; slider.minValue = 0; slider.maxValue = 1; slider.SetValueWithoutNotify(value); return slider;
        }
        private void Preference(string title, float y, bool enabled, int index)
        {
            HomeText(homeModalContent, title + " Label", title, 28, 300, 62, -110, y);
            var button = HomeButton(homeModalContent, title + " Toggle", "ToggleBackground", 165, 61, 178, y, () => HomePreferenceRequested?.Invoke(index));
            if (enabled) HomeImage(button.transform, "Original Toggle On", "ToggleOn", 145, 51, 0, 0);
            HomeImage(button.transform, "Original Toggle Handle", "ToggleHandle", 70, 70, enabled ? 47 : -47, 0);
            HomeText(button.transform, "Toggle State", enabled ? "ON" : "OFF", 17, 62, 40, enabled ? -35 : 35, 0);
        }
        private void SmallSetting(string title, float x, float y, Action action) => Button(homeModalContent, title, title, 258, 65, x, y, HomeSprite("EventBlue"), action, 23);
        private void Information(string title, string text, Action close)
        {
            BeginHomeModal(title, 700, close); HomeText(homeModalContent, "Info", text, 28, 520, 280, 0, 30);
            HomeModalPrimary = ModalButton("Information Close", "OK", -180, close);
        }
    }
}
