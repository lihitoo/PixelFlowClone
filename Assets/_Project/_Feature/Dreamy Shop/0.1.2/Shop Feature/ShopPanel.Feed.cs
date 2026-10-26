using System;
using System.Collections.Generic;
using System.Linq;
using Dreamy.Economy.UI;
using Dreamy.Economy;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Feature.Shop.Integration
{
    /// <summary>Serialized native skin; the imported ShopPanel owns lifecycle/presentation.</summary>
    public sealed partial class ShopPanel
    {
        [Serializable] public sealed class OfferSlot
        {
            public string Id, Section;
            public RectTransform Root;
            public ShopOfferItem Item;
            public Button Purchase;
            public Sprite Icon;
            public TMP_Text[] BoosterQuantities;
            public TMP_Text UnlimitedDuration;
        }
        [Serializable] public sealed class SectionSlot { public string Id; public RectTransform Frame; public TMP_Text Label; }
        [SerializeField] private OfferSlot[] offers = Array.Empty<OfferSlot>();
        [SerializeField] private SectionSlot[] sections = Array.Empty<SectionSlot>();
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button startButton, settingsButton, livesButton, coinsButton, shopButton, rankButton;
        [SerializeField] private GameObject header;
        [SerializeField] private TMP_Text miniBalance, rankLabel;
        [SerializeField] private Sprite rankSprite, lockedRankSprite;
        private IResourceBalanceProvider balances;
        [SerializeField] private TMP_Text heartCount, heartTime;
        [SerializeField] private Image heartIcon;
        [SerializeField] private Sprite heartSprite, infiniteSprite;
        [SerializeField] private ResourceUIHolder coinHolder;
        [Header("Feed layout (native authored units)")]
        [SerializeField] private float sectionHeight = 114, goldRowHeight = 480, goldColumnSpacing = 333, bannerHeight = 352.5f, bundleHeight = 435;
        [SerializeField] private float sectionCenter = 40.5f, goldBottomGap = 37.5f, contentBottomPadding = 24;
        private readonly Dictionary<string, Button> buttons = new();
        private readonly List<Action> unsubscribe = new();
        public ScrollRect Scroll => scroll;
        public ResourceUIHolder CoinHolder => coinHolder;
        public Button StartButton => startButton;
        public Button SettingsButton => settingsButton;
        public Button RankButton => rankButton;
        public Button CloseButton => closeButton;
        public IReadOnlyDictionary<string, Button> OfferButtons => buttons;
        private Transform feedRewardSource;
        public event Action SettingsRequested, LivesRequested, RankRequested, HeaderRefreshRequested;
        public event Action<bool> PurchaseBusyChanged;
        public event Action<ShopPurchaseResult> PurchaseResult;
        private string focus;
        public void Bind(IResourceBalanceProvider balances, bool mini, string section)
        {
            this.balances = balances; header.SetActive(!mini); miniBalance.gameObject.SetActive(mini);
            closeButton.gameObject.SetActive(mini);
            coinHolder.Bind(balances); focus = section;
        }
        private void BindFeedEvents()
        {
            foreach (var slot in offers)
            {
                Action<string> callback = id => { feedRewardSource = slot.Item.RewardSource; RequestPurchase(id); };
                slot.Item.PurchaseRequested += callback; unsubscribe.Add(() => slot.Item.PurchaseRequested -= callback);
            }
            BindButton(startButton, RequestClose);
            BindButton(settingsButton, () => SettingsRequested?.Invoke()); BindButton(livesButton, () => LivesRequested?.Invoke());
            BindButton(coinsButton, () => ScrollTo("Gold")); BindButton(shopButton, () => ScrollTo("Top"));
            BindButton(rankButton, () => RankRequested?.Invoke());
        }
        private void BindButton(Button button, Action action)
        {
            UnityEngine.Events.UnityAction handler = () => action(); button.onClick.AddListener(handler); unsubscribe.Add(() => button.onClick.RemoveListener(handler));
        }
        private void UnbindFeedEvents() { foreach (var remove in unsubscribe) remove(); unsubscribe.Clear(); }
        private void RenderFeed(ShopViewState state)
        {
            var visible = state.Offers.ToDictionary(x => x.Offer.Id); buttons.Clear();
            foreach (var slot in offers)
            {
                bool active = visible.TryGetValue(slot.Id, out var value); slot.Root.gameObject.SetActive(active);
                if (!active) continue; slot.Item.Render(value); slot.Item.SetIcon(slot.Icon); buttons.Add(slot.Id, slot.Purchase);
                string[] ids = { "booster.tray", "booster.hand", "booster.shuffle", "booster.super" };
                for (int i = 0; slot.BoosterQuantities != null && i < slot.BoosterQuantities.Length; i++)
                {
                    long amount = value.Offer.Rewards.FirstOrDefault(x => x.Resource.ResourceId.Value == ids[i])?.Resource.Amount ?? 0;
                    slot.BoosterQuantities[i].text = "x" + amount;
                }
                if (slot.UnlimitedDuration != null)
                    slot.UnlimitedDuration.text = (value.Offer.Rewards.FirstOrDefault(x => x.Resource.ResourceId.Value == "timer.unlimited-seconds")?.Resource.Amount ?? 0) / 3600 + "h";
            }
            foreach (var section in sections) { section.Frame.gameObject.SetActive(false); section.Label.gameObject.SetActive(false); }
            foreach (var id in visible.Keys)
                if (!buttons.ContainsKey(id)) throw new InvalidOperationException("Author a native Shop slot for offer: " + id);
            float y = 0;
            foreach (var slot in offers.Where(x => x.Section == "NoAds" && x.Root.gameObject.activeSelf)) { Place(slot, 0, y); y += bannerHeight; }
            var golds = offers.Where(x => x.Section == "Gold" && x.Root.gameObject.activeSelf).ToArray();
            if (golds.Length > 0)
            {
                Section("Gold", ref y);
                for (int i = 0; i < golds.Length; i++) Place(golds[i], (i % 3 - 1) * goldColumnSpacing, y + i / 3 * goldRowHeight);
                y += (golds.Length + 2) / 3 * goldRowHeight + goldBottomGap;
            }
            foreach (string id in new[] { "NoAdsBundle", "Bundle" })
            {
                var group = offers.Where(x => x.Section == id && x.Root.gameObject.activeSelf).ToArray(); if (group.Length == 0) continue;
                Section(id, ref y); foreach (var slot in group) { Place(slot, 0, y); y += bundleHeight; }
            }
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, y + contentBottomPadding);
            Canvas.ForceUpdateCanvases();
            if (focus != null) { string destination = focus; focus = null; ScrollTo(destination); }
            HeaderRefreshRequested?.Invoke();
        }
        private static void Place(OfferSlot slot, float x, float y) => slot.Root.anchoredPosition = new Vector2(x, -y);
        private void Section(string id, ref float y)
        {
            var slot = sections.First(x => x.Id == id); slot.Frame.gameObject.SetActive(true); slot.Label.gameObject.SetActive(true);
            slot.Frame.anchoredPosition = slot.Label.rectTransform.anchoredPosition = new Vector2(0, -y - sectionCenter); y += sectionHeight;
        }
        public void ScrollTo(string id)
        {
            float y = 0;
            var section = sections.FirstOrDefault(x => x.Id == id && x.Frame.gameObject.activeSelf);
            if (section != null) y = -section.Frame.anchoredPosition.y - sectionCenter;
            scroll.StopMovement(); scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(y, 0, Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height)));
        }
        public void RenderHeader(int hearts, bool unlimited, long seconds, bool atMaximum, bool rankUnlocked)
        {
            rankButton.image.sprite = rankUnlocked ? rankSprite : lockedRankSprite;
            rankLabel.text = rankUnlocked ? "Rank" : "";
            miniBalance.text = balances.GetBalance(new ResourceId("currency.coin")).ToString("N0");
            heartIcon.sprite = unlimited ? infiniteSprite : heartSprite;
            heartCount.text = unlimited ? "" : hearts.ToString();
            heartTime.text = atMaximum && !unlimited ? "MAX" : $"{seconds / 60:00}:{seconds % 60:00}";
        }
        private void SetFeedInteractable(bool enabled)
        { foreach (var slot in offers) slot.Item.SetPurchaseInteractable(enabled); if (enabled) coinHolder.RefreshBalance(); PurchaseBusyChanged?.Invoke(!enabled); }
        private void ShowFeedResult(ShopPurchaseResult result) { HeaderRefreshRequested?.Invoke(); PurchaseResult?.Invoke(result); }
        public void RetryPurchase(string id) => PurchaseRequested?.Invoke(id);
    }
}
