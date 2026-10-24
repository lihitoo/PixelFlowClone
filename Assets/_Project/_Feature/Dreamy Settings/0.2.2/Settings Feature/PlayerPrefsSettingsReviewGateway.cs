using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Economy;
using Dreamy.Settings;
using UnityEngine;

namespace Dreamy.Feature.Settings.Integration
{
    /// <summary>Device-local review preference persistence. Account-scoped games should supply their save/backend owner.</summary>
    public sealed class PlayerPrefsSettingsReviewGateway : ISettingsReviewGateway
    {
        private const string PositiveKey = "Dreamy.Settings.Sample.PositiveRating";
        private const string RewardKey = "Dreamy.Settings.Sample.ReviewRewardClaimed";
        public const string RewardTransactionId = "settings:positive-rating-reward:v1";
        private readonly IResourceWallet wallet;
        private readonly bool enableAndroidReward;
        private readonly string rewardResourceId;
        private readonly long rewardAmount;

        public PlayerPrefsSettingsReviewGateway(IResourceWallet wallet = null, bool enableAndroidReward = false,
            string rewardResourceId = "currency.gem", long rewardAmount = 10)
        {
            this.wallet = wallet;
            this.enableAndroidReward = enableAndroidReward;
            this.rewardResourceId = rewardResourceId;
            this.rewardAmount = rewardAmount;
        }

        public bool HasPositiveRating => PlayerPrefs.GetInt(PositiveKey, 0) >= 4;
        public bool HasClaimedReward => PlayerPrefs.GetInt(RewardKey, 0) != 0;
        public bool CanGrantReward => Application.platform == RuntimePlatform.Android &&
            enableAndroidReward && wallet != null && rewardAmount > 0 &&
            ResourceId.TryParse(rewardResourceId, out _);

        public void RecordPositiveRating(int rating)
        {
            if (rating < 4 || rating > 5) throw new ArgumentOutOfRangeException(nameof(rating));
            PlayerPrefs.SetInt(PositiveKey, rating);
            PlayerPrefs.Save();
        }

        public UniTask<SettingsOperationResult> GrantRewardOnceAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested || !CanGrantReward || !HasPositiveRating)
                return UniTask.FromResult(SettingsOperationResult.Unavailable("Reward is not available."));
            if (HasClaimedReward)
                return UniTask.FromResult(SettingsOperationResult.Succeeded());

            // The wallet must persist/deduplicate this ID, including retries after interrupted saves.
            if (!wallet.TryGrant(new ResourceGrantRequest(RewardTransactionId,
                    new ResourceAmount(new ResourceId(rewardResourceId), rewardAmount))))
                return UniTask.FromResult(SettingsOperationResult.Failed("Reward grant failed; retry is available."));

            PlayerPrefs.SetInt(RewardKey, 1);
            PlayerPrefs.Save();
            return UniTask.FromResult(SettingsOperationResult.Succeeded("Reward granted once."));
        }
    }
}
