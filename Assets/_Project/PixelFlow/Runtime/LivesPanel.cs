using System;
using Dreamy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow
{
    public sealed class LivesPanel : UIPanel
    {
        [SerializeField] private Image heart;
        [SerializeField] private Sprite normalHeart, unlimitedHeart;
        [SerializeField] private TMP_Text amount, heading, timer, refillPrice;
        [SerializeField] private Button closeButton, refillButton, coinsButton;
        private float refreshAt;
        public override bool CanBack => true;
        public override bool CanCache => true;
        public override UILayer Layer => UILayer.Popup;
        public Button CloseButton => closeButton;
        public Button RefillButton => refillButton;
        public Button CoinsButton => coinsButton;
        public event Action CloseRequested, RefillRequested, CoinsRequested, RefreshRequested;
        private void OnEnable()
        {
            closeButton.onClick.AddListener(RequestClose); refillButton.onClick.AddListener(RequestRefill); coinsButton.onClick.AddListener(RequestCoins);
        }
        protected override void OnDisable()
        {
            closeButton.onClick.RemoveListener(RequestClose); refillButton.onClick.RemoveListener(RequestRefill); coinsButton.onClick.RemoveListener(RequestCoins);
            base.OnDisable();
        }
        private void Update()
        {
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + 1; RefreshRequested?.Invoke();
        }
        public void Render(int count, int maximum, long seconds, bool unlimited, bool canAfford, int price)
        {
            bool full = count == maximum;
            heart.sprite = unlimited ? unlimitedHeart : normalHeart;
            amount.text = unlimited ? "" : count.ToString();
            heading.text = unlimited ? "Unlimited lives" : full ? "Your lives are full!" : "Next heart time:";
            timer.text = full && !unlimited ? "" : Duration(seconds);
            refillPrice.text = "Refill Now!\n" + price;
            refillButton.interactable = !full && !unlimited && canAfford;
            coinsButton.gameObject.SetActive(!full && !unlimited && !canAfford);
        }
        private static string Duration(long seconds) => seconds >= 3600 ? string.Format("{0}:{1:00}:{2:00}", seconds / 3600, seconds / 60 % 60, seconds % 60) : string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        private void RequestClose() => CloseRequested?.Invoke();
        private void RequestRefill() => RefillRequested?.Invoke();
        private void RequestCoins() => CoinsRequested?.Invoke();
    }
}
