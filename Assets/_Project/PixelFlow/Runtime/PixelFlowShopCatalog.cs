using System;
using System.Collections.Generic;
using Dreamy.DataConfig;

namespace PixelFlow
{
    [Serializable]
    public sealed class PixelFlowShopCatalog : ConfigBase
    {
        public string Provenance;
        public int BundlesUnlockLevel = 20;
        public List<PixelFlowShopOffer> Offers = new();
        public override void Initialize(string documentName)
        {
            if (BundlesUnlockLevel < 1 || Offers == null || Offers.Count == 0) throw new ArgumentException("Invalid Shop catalog.");
            var ids = new HashSet<string>(); var products = new HashSet<string>();
            foreach (var offer in Offers)
            {
                if (offer == null || string.IsNullOrWhiteSpace(offer.Id) || !ids.Add(offer.Id) || string.IsNullOrWhiteSpace(offer.ProductId) || !products.Add(offer.ProductId) ||
                    string.IsNullOrWhiteSpace(offer.Title) || string.IsNullOrWhiteSpace(offer.Price) || offer.Coins < 0 || offer.Boosters < 0 || offer.Boosters > 999 ||
                    offer.UnlimitedSeconds < 0 || offer.UnlimitedSeconds > 7 * 86400 || offer.GoldIcon < 0 || offer.GoldIcon > 5 ||
                    offer.Section is not ("Gold" or "NoAds" or "NoAdsBundle" or "Bundle") || offer.Style is not ("Blue" or "Purple" or "Yellow"))
                    throw new ArgumentException("Invalid/duplicate Shop offer.");
                if (offer.NoAds != offer.Once || offer.Coins == 0 && offer.Boosters == 0 && !offer.NoAds && offer.UnlimitedSeconds == 0)
                    throw new ArgumentException("Invalid Shop rewards/ownership.");
            }
        }
        public PixelFlowShopOffer Find(string id) => Offers.Find(x => x.Id == id) ?? throw new ArgumentException("Unknown Shop offer: " + id);
        public bool IsVisible(PixelFlowShopOffer offer, PixelFlowProgress profile, HomeTuning home) =>
            !(offer.Once && profile.NoAds) && (offer.Section is "NoAds" or "NoAdsBundle" ? profile.UnlockedLevel + 1 >= home.NoAdsLevel :
                offer.Section == "Bundle" ? profile.UnlockedLevel + 1 >= BundlesUnlockLevel : true);
    }
    [Serializable]
    public sealed class PixelFlowShopOffer
    {
        public string Id, ProductId, Title, Price, Section, Style = "Blue";
        public int Coins, Boosters, UnlimitedSeconds, GoldIcon;
        public bool NoAds, Once;
    }
}
