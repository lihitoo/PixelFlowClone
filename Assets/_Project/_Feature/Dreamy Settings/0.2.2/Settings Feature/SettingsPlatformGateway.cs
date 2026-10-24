using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Settings;

namespace Dreamy.Feature.Settings.Integration
{
    /// <summary>Placeholder for platform operations supplied by DreamySDK.</summary>
    public sealed class SettingsPlatformGateway : ISettingsPlatformGateway
    {
        // TODO: Report capabilities from DreamySDK when its integration is added.
        public bool CanShowGdprConsent => false;
        public bool CanRestorePurchases => false;
        public bool CanOpenStore => false;
        public bool CanRequestReview => false;

        public UniTask<SettingsOperationResult> ShowGdprConsentAsync(CancellationToken token = default)
        {
            // TODO: Delegate consent to DreamySDK.
            return UniTask.FromResult(SettingsOperationResult.Unavailable());
        }

        public UniTask<SettingsOperationResult> RestorePurchasesAsync(CancellationToken token = default)
        {
            // TODO: Delegate purchase restoration to DreamySDK.
            return UniTask.FromResult(SettingsOperationResult.Unavailable());
        }

        public UniTask<SettingsOperationResult> OpenStoreAsync(CancellationToken token = default)
        {
            // TODO: Delegate store opening to DreamySDK.
            return UniTask.FromResult(SettingsOperationResult.Unavailable());
        }

        public UniTask<SettingsOperationResult> RequestReviewAsync(CancellationToken token = default)
        {
            // TODO: Delegate review requests to DreamySDK.
            return UniTask.FromResult(SettingsOperationResult.Unavailable());
        }
    }
}
