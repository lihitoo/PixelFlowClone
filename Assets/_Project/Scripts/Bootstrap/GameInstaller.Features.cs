using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Audio;
using Dreamy.Core;
using Dreamy.Economy;
using Dreamy.Feature.Settings.Integration;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Feedback;
using Dreamy.Settings;
using Dreamy.Shop;
using Dreamy.UI;
using UnityEngine;

namespace Dreamy.Template
{
    public sealed partial class GameInstaller
    {
        private readonly List<EntitlementEffectBinding> entitlementBindings = new();

        private void InstallFeatures()
        {
            InstallSettings();
            InstallShop();
        }

        private void InstallSettings()
        {
            if (!ServiceLocator.TryGet<ISettingsPlatformGateway>(out var settingsGateway))
            {
                settingsGateway = new SettingsPlatformGateway();
                RegisterService<ISettingsPlatformGateway>(settingsGateway, ownsInstance: true);
            }
            if (!ServiceLocator.TryGet<ISettingsHapticsGateway>(out var hapticsGateway))
            {
                ServiceLocator.TryGet<IHapticService>(out var hapticService);
                hapticsGateway = new PlayerPrefsSettingsHapticsGateway(
                    enabled => { if (hapticService != null) hapticService.Enabled = enabled; },
                    () => hapticService != null && (Application.isEditor ||
                        Application.platform == RuntimePlatform.Android ||
                        Application.platform == RuntimePlatform.IPhonePlayer));
                RegisterService<ISettingsHapticsGateway>(hapticsGateway, ownsInstance: true);
            }
            ServiceLocator.TryGet<ISettingsReviewGateway>(out var reviewGateway);
            TrackFeatureInstallation<ISettingsService>(() =>
                SettingsFeatureInstaller.Install(presenterFactory, ServiceLocator.Get<IAudioService>(),
                    () => OpenRateUsAsync().Forget(), settingsGateway, hapticsGateway, reviewGateway));
        }

        private void InstallShop()
        {
            if (!ServiceLocator.TryGet<IShopPurchaseGateway>(out var purchaseGateway))
            {
                purchaseGateway = new ShopPurchaseGateway(PurchaseWithBillingAsync);
                RegisterService<IShopPurchaseGateway>(purchaseGateway, ownsInstance: true);
            }
            TrackFeatureInstallation<IShopService>(() =>
                ShopFeatureInstaller.Install(presenterFactory, dataConfig.GetTable<ShopCatalogConfig>(),
                    wallet, purchaseGateway, (IResourceBalanceProvider)wallet, feedbackHost));
            InstallEntitlementEffects();
        }

        private UniTask<ShopGatewayPurchaseResult> PurchaseWithBillingAsync(
            ShopGatewayPurchaseRequest request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.FromResult(ShopGatewayPurchaseResult.Cancelled());

            // TODO: Replace simulation with DreamySDK.Purchase(request.StoreProductId, ...).
            // Map the validated SDK result to Purchased(transactionId), Cancelled() or Failed().
            var transactionId = $"simulated-iap-{Guid.NewGuid():N}";
            Debug.Log($"[Shop] Simulated IAP purchase: {request.StoreProductId} ({transactionId}).", this);
            return UniTask.FromResult(ShopGatewayPurchaseResult.Purchased(transactionId));
        }

        private void InstallEntitlementEffects()
        {
            BindEntitlement(EntitlementIds.RemoveAds, ApplyNoAdsDemo);
            BindEntitlement(EntitlementIds.LifetimeVip, ApplyLifetimeVipDemo);
            BindEntitlement(EntitlementIds.StarterFund, UnlockStarterFundDemo);
            BindEntitlement(EntitlementIds.Season1PremiumPass, UnlockSeason1PremiumPassDemo);
        }

        private void BindEntitlement(ResourceId id, Action effect)
        {
            entitlementBindings.Add(new EntitlementEffectBinding(
                walletBalanceSource, id, effect, exception => Debug.LogException(exception, this)));
        }

        // Replace these demo callbacks with the game SDK/services.
        // Wallet balances remain the source of entitlement ownership.
        private void ApplyNoAdsDemo() => LogEntitlementEffect("No Ads enabled");

        private void ApplyLifetimeVipDemo() => LogEntitlementEffect("VIP enabled");

        private void UnlockStarterFundDemo() => LogEntitlementEffect("Starter Fund unlocked");

        private void UnlockSeason1PremiumPassDemo() => LogEntitlementEffect("Season 1 Premium Pass unlocked");

        private void LogEntitlementEffect(string effect)
        {
            Debug.Log($"[Shop] {effect}", this);
        }

        private void DisposeEntitlementEffects()
        {
            foreach (var binding in entitlementBindings) binding.Dispose();
            entitlementBindings.Clear();
        }
    }
}
