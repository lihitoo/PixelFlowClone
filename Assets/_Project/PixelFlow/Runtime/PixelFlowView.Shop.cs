using System;
using System.Collections.Generic;
using System.Globalization;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PixelFlow
{
    public sealed partial class PixelFlowView
    {
        private readonly Dictionary<string, Sprite> shopSprites = new();
        private TMP_Text shopBusyText;
        private Dreamy.Feature.Shop.Integration.ShopPanel foundationShop;
        private PixelFlowShopCatalog shopCatalog;
        public ScrollRect ShopScroll => foundationShop != null ? foundationShop.Scroll : null;
        public Button ShopCloseButton => foundationShop != null ? foundationShop.CloseButton : null;
        public Button ShopRestoreButton => foundationShop != null ? foundationShop.RestoreButton : null;
        public IReadOnlyDictionary<string, Button> ShopOfferButtons => foundationShop != null ? foundationShop.OfferButtons : new Dictionary<string, Button>();
        public event Action RestorePurchasesRequested, MiniShopRequested, ShopGoldRequested;
        public void ConfigureShop(PixelFlowShopCatalog catalog) => shopCatalog = catalog;
        public void BindFoundationShop(Dreamy.Feature.Shop.Integration.ShopPanel panel) => foundationShop = panel;
        public void HideFoundationModal() { if (homeModal != null) homeModal.SetActive(false); }
        public void HideForFoundationPanel()
        {
            HidePages(); home.SetActive(false); hud.SetActive(false); overlay.SetActive(false);
            ShowHomeChrome(false); homeBackground.SetActive(false);
        }
        private Sprite ShopSprite(string key)
        {
            if (!shopSprites.TryGetValue(key, out var value))
            {
                value = Resources.Load<Sprite>("PixelFlow/UI/Shop/" + key);
                if (value == null) throw new InvalidOperationException("Missing source Shop sprite: " + key);
                shopSprites.Add(key, value);
            }
            return value;
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
            BeginHomeModal("Get " + BoosterNames[(int)kind], 730, close);
            Image(homeModalContent, "Booster Icon", ShopSprite(new[] { "Tray", "Hand", "Shuffle", "Super" }[(int)kind]), Color.white, 160, 160, 0, 100, preserve: true);
            HomeText(homeModalContent, "Booster Purchase Detail", BoosterDescriptions[(int)kind] + "\n\n" + store.Data.Coins + " coins", 27, 520, 150, 0, -35);
            bool enough = store.Data.Coins >= store.Price(kind);
            HomeModalPrimary = ModalButton("Buy Booster", enough ? store.Price(kind) + " coins" : "Get coins", -180,
                () => { if (enough) PurchaseRequested?.Invoke(kind); else MiniShopRequested?.Invoke(); });
        }
    }
}
