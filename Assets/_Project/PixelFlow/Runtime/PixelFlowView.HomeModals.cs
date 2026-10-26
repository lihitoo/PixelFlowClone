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
        private Button homeModalClose;
        private LivesPanel livesView;
        public void BindLives(LivesPanel panel) => livesView = panel;
        public Button HomeModalClose => !IsHomeModalVisible && gameplay.IsOfferVisible ? gameplay.OfferCloseButton : !IsHomeModalVisible && livesView != null ? livesView.CloseButton : settingsView != null && !IsHomeModalVisible ? settingsView.CloseButton : homeModalClose;
        private Button homeModalPrimary;
        public Button HomeModalPrimary { get => !IsHomeModalVisible && gameplay.IsOfferVisible ? gameplay.OfferBuyButton : !IsHomeModalVisible && livesView != null ? livesView.RefillButton : homeModalPrimary; private set => homeModalPrimary = value; }
        public Slider HomeMusicSlider => settingsView != null ? settingsView.MusicSlider : null;
        public Slider HomeSoundSlider => settingsView != null ? settingsView.SoundSlider : null;
        private void BuildHomeModal()
        {
            homeModal = Rect(safe, "Source Home Modal", 0, 0, 0, 0).gameObject; Stretch((RectTransform)homeModal.transform);
            Image(homeModal.transform, "Modal Backdrop", null, new Color(.01f, .025f, .07f, .78f), 0, 0, 0, 0, stretch: true, blocks: true);
            homeModalCard = Rect(homeModal.transform, "Source Popup Card", 630, 790, 0, 0);
            Image(homeModalCard, "Original Popup", HomeSprite("PopupHeader"), Color.white, 0, 0, 0, 0, stretch: true, blocks: true);
            homeModalTitle = HomeText(homeModalCard, "Title", "", 44, 470, 85, 0, -68, new Vector2(.5f, 1));
            homeModalClose = HomeButton(homeModalCard, "Close Home Modal", "Close", 85, 85, -15, -12, () => homeModalBack?.Invoke(), new Vector2(1, 1));
            homeModalContent = Rect(homeModalCard, "Modal Content", 0, 0, 0, 0); Stretch(homeModalContent); homeModalContent.offsetMax = new Vector2(0, -135);
            homeModal.SetActive(false);
        }
        private void BeginHomeModal(string title, float height, Action close)
        {
            HidePages(true); ShowHomeChrome(false); gameplay.HideResult();
            homeModalBack = close; backAction = close;
            for (int i = homeModalContent.childCount - 1; i >= 0; i--)
            {
                var old = homeModalContent.GetChild(i).gameObject; old.SetActive(false); Object.Destroy(old);
            }
            HomeModalPrimary = null; homeModalClose.gameObject.SetActive(true);
            homeModalCard.sizeDelta = new Vector2(630, height); homeModalTitle.text = title;
            homeModal.SetActive(true); homeModal.transform.SetAsLastSibling();
        }
        private Button ModalButton(string name, string label, float y, Action action, bool enabled = true)
        {
            var button = Button(homeModalContent, name, label, 440, 82, 0, y, HomeSprite("Play"), action, 29);
            button.interactable = enabled; return button;
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
    }
}
