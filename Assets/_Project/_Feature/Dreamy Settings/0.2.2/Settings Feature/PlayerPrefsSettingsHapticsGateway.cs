using System;
using Dreamy.Settings;
using UnityEngine;

namespace Dreamy.Feature.Settings.Integration
{
    /// <summary>Device-local preference storage connected to an actual haptic provider.</summary>
    public sealed class PlayerPrefsSettingsHapticsGateway : ISettingsHapticsGateway
    {
        // Preserve existing preference keys across the integration rename.
        private const string PreferenceKey = "Dreamy.Settings.Sample.HapticsEnabled";
        private readonly Action<bool> apply;
        private readonly Func<bool> supported;

        public PlayerPrefsSettingsHapticsGateway(Action<bool> apply, Func<bool> supported = null)
        {
            this.apply = apply ?? throw new ArgumentNullException(nameof(apply));
            this.supported = supported ?? (() => true);
            if (IsSupported) apply(IsEnabled);
        }

        public bool IsSupported => supported();
        public bool IsEnabled => PlayerPrefs.GetInt(PreferenceKey, 1) != 0;

        public void SetEnabled(bool enabled)
        {
            if (!IsSupported) return;
            apply(enabled);
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
