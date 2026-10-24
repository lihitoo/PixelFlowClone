using System;
using UnityEditor;
using UnityEngine;

namespace PixelFlow.Editor
{
    public static class ShopReviewCommands
    {
        private static PixelFlowApp App()
        {
            var app = UnityEngine.Object.FindFirstObjectByType<PixelFlowApp>();
            if (!EditorApplication.isPlaying || app == null || !app.SaveDirectory.StartsWith("PixelFlowCloneSmoke_", StringComparison.Ordinal))
                throw new InvalidOperationException("An isolated review/test save is required.");
            return app;
        }
        [MenuItem("Pixel Flow/Shop Review/Prepare Test Evidence")]
        public static void PrepareEvidence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            Environment.SetEnvironmentVariable("PIXEL_FLOW_REVIEW_RUN", "shop-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }
        [MenuItem("Pixel Flow/Shop Review/Open Shop")]
        public static void OpenShop() => App().View.ShopButton.onClick.Invoke();
        [MenuItem("Pixel Flow/Shop Review/Show Gold Packs")]
        public static void Golds() => App().View.ScrollShopTo("Gold");
        [MenuItem("Pixel Flow/Shop Review/Show No Ads Bundle")]
        public static void NoAdsBundle() => App().View.ScrollShopTo("NoAdsBundle");
        [MenuItem("Pixel Flow/Shop Review/Show Bundles")]
        public static void Bundles() => App().View.ScrollShopTo("Bundle");
        private static FakeShopGateway Gateway() => App().Shop.Gateway as FakeShopGateway ?? throw new InvalidOperationException("The active gateway is not fake.");
        [MenuItem("Pixel Flow/Shop Review/Fake Next Purchase/Success")]
        public static void Success() => Gateway().NextOutcome = FakeShopOutcome.Success;
        [MenuItem("Pixel Flow/Shop Review/Fake Next Purchase/Cancelled")]
        public static void Cancelled() => Gateway().NextOutcome = FakeShopOutcome.Cancelled;
        [MenuItem("Pixel Flow/Shop Review/Fake Next Purchase/Failed")]
        public static void Failed() => Gateway().NextOutcome = FakeShopOutcome.Failed;
        [MenuItem("Pixel Flow/Shop Review/Fake Next Purchase/Deferred")]
        public static void Deferred() => Gateway().NextOutcome = FakeShopOutcome.Deferred;
        [MenuItem("Pixel Flow/Shop Review/Approve Deferred")]
        public static void Approve() => Gateway().ResolveDeferred(true);
        [MenuItem("Pixel Flow/Shop Review/Reject Deferred")]
        public static void Reject() => Gateway().ResolveDeferred(false);
    }
}
