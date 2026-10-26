using System;
using System.Collections.Generic;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow
{
    public sealed partial class PixelFlowView
    {
        private TMP_Text shopBusyText;
        private Dreamy.Feature.Shop.Integration.ShopPanel foundationShop;
        private static readonly IReadOnlyDictionary<string, Button> EmptyShopButtons = new Dictionary<string, Button>();
        public ScrollRect ShopScroll => foundationShop != null ? foundationShop.Scroll : null;
        public Button ShopCloseButton => foundationShop != null ? foundationShop.CloseButton : null;
        public IReadOnlyDictionary<string, Button> ShopOfferButtons => foundationShop != null ? foundationShop.OfferButtons : EmptyShopButtons;
        public event Action MiniShopRequested, ShopGoldRequested;
        public void BindFoundationShop(Dreamy.Feature.Shop.Integration.ShopPanel panel) => foundationShop = panel;
        public void HideFoundationModal() { if (homeModal != null) homeModal.SetActive(false); }
        public void HideForFoundationPanel()
        {
            HidePages(); home.SetActive(false); hud.SetActive(false); gameplay.HideResult();
            ShowHomeChrome(false); homeBackground.SetActive(false);
        }
        public void ScrollShopTo(string section) => foundationShop?.ScrollTo(section);
        public void ShowShopBusy(string message)
        {
            BeginHomeModal("", 580, () => { }); HomeModalClose.gameObject.SetActive(false);
            shopBusyText = HomeText(homeModalContent, "Store Processing", message, 34, 510, 240, 0, 45);
        }
        public void UpdateStoreWaiting()
        { if (shopBusyText != null && shopBusyText.gameObject.activeInHierarchy) shopBusyText.text = "Waiting for purchase approval..."; }
        public void ShowShopResult(string title, string detail, Action close, Action retry = null)
        {
            BeginHomeModal(title, 680, close); HomeText(homeModalContent, "Purchase Result", detail, 28, 520, 220, 0, 35);
            HomeModalPrimary = ModalButton("Store Result Action", retry == null ? "Continue" : "Retry", -168, retry ?? close);
        }
        public void ShowBoosterPurchase(ProgressStore store, BoosterKind kind, Action close)
        {
            HidePages(); backAction = close;
            bool enough = store.Data.Coins >= store.Price(kind);
            gameplay.ShowOffer(new[] { "Add Tray", "Hand", "Shuffle", "Super Pig" }[(int)kind], BoosterDescriptions[(int)kind],
                gameplay.Boosters[(int)kind].Icon.sprite, enough ? store.Price(kind) + " coins" : "Get coins",
                () => { if (enough) PurchaseRequested?.Invoke(kind); else MiniShopRequested?.Invoke(); }, close);
        }
    }
}
