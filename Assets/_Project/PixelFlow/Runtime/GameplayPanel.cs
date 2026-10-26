using System;
using System.Collections.Generic;
using Dreamy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PixelFlow
{
    /// <summary>Authored gameplay chrome. The existing app owns gameplay, inventory and navigation.</summary>
    public sealed class GameplayPanel : UIPanel
    {
        [Serializable] public sealed class BoosterSlot
        {
            public Button Button;
            public TMP_Text Amount;
            public GameObject Plus;
            public Image Icon;
            [NonSerialized] internal int RenderedAmount = int.MinValue;
        }
        [SerializeField] private Button settingsButton, coinsButton;
        [SerializeField] private TMP_Text levelLabel, walletLabel, beltLabel, timerLabel;
        [SerializeField] private Image levelFrame, settingsFrame;
        [SerializeField] private Sprite normalHeader, hardHeader, veryHardHeader;
        [SerializeField] private Sprite normalButton, hardButton, veryHardButton;
        [SerializeField] private BoosterSlot[] boosters;
        [Header("Authored stock purchase popup")]
        [SerializeField] private GameObject offer, offerBackdrop;
        [SerializeField] private TMP_Text offerTitle, offerDetail, offerPrice;
        [SerializeField] private Image offerIcon;
        [SerializeField] private Button offerBuy, offerClose;
        private Action buyOffer, closeOffer;
        [Header("Authored results and target instructions")]
        [SerializeField] private GameObject resultBackdrop, winContent, failContent, targetContent;
        [SerializeField] private TMP_Text winTitle, winReward, failTitle, failDetail, continuePrice, targetTitle, targetDetail;
        [SerializeField] private Image targetIcon;
        [SerializeField] private Button nextButton, continueButton, resultClose, rewardedButton;
        [SerializeField] private VideoPlayer continueVideo;
        [SerializeField] private RawImage continuePreview;
        private RenderTexture videoTexture;
        [SerializeField] private HoldPreview holdPreview;
        [SerializeField] private CanvasGroup failGroup;
        private Action nextResult, continueResult, closeResult;
        private UnityEngine.Events.UnityAction[] requests;
        private int renderedLevel = -1, renderedCoins = -1;
        private string renderedDifficulty;
        public override bool CanBack => false;
        public override bool CanCache => true;
        public bool IsReady => isActiveAndEnabled && canvasGroup.alpha > .99f;
        public Button SettingsButton => settingsButton;
        public Button CoinsButton => coinsButton;
        public TMP_Text LevelLabel => levelLabel;
        public TMP_Text WalletLabel => walletLabel;
        public TMP_Text BeltLabel => beltLabel;
        public TMP_Text TimerLabel => timerLabel;
        public IReadOnlyList<BoosterSlot> Boosters => boosters;
        public bool IsOfferVisible => offer.activeSelf;
        public Button OfferBuyButton => offerBuy;
        public Button OfferCloseButton => offerClose;
        public Button NextButton => nextButton;
        public Button ContinueButton => continueButton;
        public Button ResultCloseButton => resultClose;
        public Button RewardedButton => rewardedButton;
        public GameObject TargetContent => targetContent;
        public TMP_Text WinReward => winReward;
        public HoldPreview HoldPreview => holdPreview;
        public bool IsResultVisible => winContent.activeSelf || failContent.activeSelf;
        public event Action SettingsRequested, CoinsRequested;
        public event Action<int> BoosterRequested;
        private void OnEnable()
        {
            settingsButton.onClick.AddListener(RequestSettings); coinsButton.onClick.AddListener(RequestCoins);
            offerBuy.onClick.AddListener(BuyOffer); offerClose.onClick.AddListener(CloseOffer);
            nextButton.onClick.AddListener(NextResult); continueButton.onClick.AddListener(ContinueResult); resultClose.onClick.AddListener(CloseResult);
            holdPreview.Changed += PeekResult;
            if (requests == null)
            {
                requests = new UnityEngine.Events.UnityAction[boosters.Length];
                for (int i = 0; i < requests.Length; i++) { int index = i; requests[i] = () => BoosterRequested?.Invoke(index); }
            }
            for (int i = 0; i < boosters.Length; i++) boosters[i].Button.onClick.AddListener(requests[i]);
        }
        protected override void OnDisable()
        {
            settingsButton.onClick.RemoveListener(RequestSettings); coinsButton.onClick.RemoveListener(RequestCoins);
            offerBuy.onClick.RemoveListener(BuyOffer); offerClose.onClick.RemoveListener(CloseOffer);
            nextButton.onClick.RemoveListener(NextResult); continueButton.onClick.RemoveListener(ContinueResult); resultClose.onClick.RemoveListener(CloseResult);
            holdPreview.Changed -= PeekResult;
            HideOffer();
            HideResult(); HideTarget();
            if (requests != null) for (int i = 0; i < boosters.Length; i++) boosters[i].Button.onClick.RemoveListener(requests[i]);
            base.OnDisable();
        }
        public void RenderHeader(int level, string difficulty, int coins)
        {
            if (renderedLevel == level && renderedDifficulty == difficulty && renderedCoins == coins) return;
            renderedLevel = level; renderedDifficulty = difficulty; renderedCoins = coins;
            levelLabel.text = "Level " + level; walletLabel.text = coins.ToString("N0");
            bool veryHard = difficulty is "VeryHard" or "Very Hard" or "UltraHard" or "Ultra Hard";
            bool hard = difficulty == "Hard";
            levelFrame.sprite = veryHard ? veryHardHeader : hard ? hardHeader : normalHeader;
            var button = veryHard ? veryHardButton : hard ? hardButton : normalButton;
            settingsFrame.sprite = button;
            foreach (var booster in boosters) booster.Button.image.sprite = button;
        }
        public void RenderBooster(int index, int amount, bool interactable)
        {
            var slot = boosters[index]; slot.Button.interactable = interactable;
            if (slot.RenderedAmount == amount) return;
            slot.RenderedAmount = amount; slot.Amount.text = amount.ToString();
            slot.Amount.gameObject.SetActive(amount > 0); slot.Plus.SetActive(amount == 0);
        }
        public void RenderBelt(int active, int capacity) => beltLabel.SetText("{0}/{1}", active, capacity);
        public void RenderTimer(float seconds) => timerLabel.text = seconds > 0 ? Mathf.CeilToInt(seconds).ToString() : "";
        public void ShowOffer(string title, string detail, Sprite icon, string price, Action buy, Action close)
        {
            offerTitle.text = title; offerDetail.text = detail; offerIcon.sprite = icon; offerPrice.text = price;
            buyOffer = buy; closeOffer = close; offerBackdrop.SetActive(true); offer.SetActive(true);
        }
        public void HideOffer()
        {
            if (offer != null) offer.SetActive(false);
            if (offerBackdrop != null) offerBackdrop.SetActive(false);
            buyOffer = closeOffer = null;
        }
        private void BuyOffer() => buyOffer?.Invoke();
        private void CloseOffer() => closeOffer?.Invoke();
        public void ShowResult(bool won, int level, int reward, bool canContinue, int cost, string lossReason, Action next, Action resume, Action close)
        {
            HideOffer(); HideTarget(); HideResult();
            nextResult = next; continueResult = resume; closeResult = close;
            resultBackdrop.SetActive(true); winContent.SetActive(won); failContent.SetActive(!won);
            settingsButton.gameObject.SetActive(false); levelFrame.gameObject.SetActive(false);
            foreach (var slot in boosters) slot.Button.gameObject.SetActive(false);
            winTitle.text = "LEVEL " + level + "\nCOMPLETED!"; winReward.text = reward.ToString("N0");
            bool space = lossReason != null && lossReason.IndexOf("waiting", StringComparison.OrdinalIgnoreCase) >= 0;
            failTitle.text = space ? "OUT OF SPACE!" : "LEVEL FAILED!";
            failDetail.text = canContinue ? "Open up slots to keep playing!" : "Try this level again from Home.";
            continuePrice.text = cost.ToString("N0"); continueButton.gameObject.SetActive(!won && canContinue);
            rewardedButton.interactable = false; // No rewarded provider is installed.
            holdPreview.gameObject.SetActive(!won); PeekResult(false);
            continuePreview.gameObject.SetActive(!won && canContinue && space);
            if (!won && canContinue && space)
            {
                videoTexture = new RenderTexture(640, 448, 0) { name = "Continue Preview" };
                continuePreview.texture = videoTexture; continueVideo.targetTexture = videoTexture; continueVideo.Play();
            }
        }
        public void HideResult()
        {
            PeekResult(false);
            if (holdPreview != null) holdPreview.gameObject.SetActive(false);
            if (continueVideo != null) { continueVideo.Stop(); continueVideo.targetTexture = null; }
            if (continuePreview != null) continuePreview.texture = null;
            if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); videoTexture = null; }
            if (resultBackdrop != null) resultBackdrop.SetActive(false);
            if (winContent != null) winContent.SetActive(false);
            if (failContent != null) failContent.SetActive(false);
            nextResult = continueResult = closeResult = null;
            if (settingsButton != null) settingsButton.gameObject.SetActive(true);
            if (levelFrame != null) levelFrame.gameObject.SetActive(true);
            if (boosters != null) foreach (var slot in boosters) if (slot.Button != null) slot.Button.gameObject.SetActive(true);
        }
        public void ShowTarget(bool hand)
        {
            HideOffer(); HideResult(); targetContent.SetActive(true);
            targetTitle.text = hand ? "HAND" : "SUPER PIG";
            targetDetail.text = hand ? "Pick any pig or item in the queue." : "Select a color to shoot with super powers!";
            targetIcon.sprite = boosters[hand ? 1 : 3].Icon.sprite;
            foreach (var slot in boosters) slot.Button.gameObject.SetActive(false);
        }
        public void HideTarget() { if (targetContent != null) targetContent.SetActive(false); }
        private void NextResult() => nextResult?.Invoke();
        private void ContinueResult() => continueResult?.Invoke();
        private void CloseResult() => closeResult?.Invoke();
        private void PeekResult(bool held)
        {
            if (failGroup != null) { failGroup.alpha = held ? 0 : 1; failGroup.blocksRaycasts = !held; }
            if (resultBackdrop != null) resultBackdrop.GetComponent<Image>().enabled = !held;
        }
        private void RequestSettings() => SettingsRequested?.Invoke();
        private void RequestCoins() => CoinsRequested?.Invoke();
    }
}
