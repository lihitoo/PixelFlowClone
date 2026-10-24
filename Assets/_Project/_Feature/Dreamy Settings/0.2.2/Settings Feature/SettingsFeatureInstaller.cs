using System;
using Dreamy.Audio;
using Dreamy.Settings;
using Dreamy.UI;

namespace Dreamy.Feature.Settings.Integration
{
    /// <summary>Production composition entry point for the supplied Settings views.</summary>
    public static class SettingsFeatureInstaller
    {
        public static ISettingsService Install(PanelPresenterFactory factory, IAudioService audio, Action openRateUs,
            ISettingsPlatformGateway platform = null, ISettingsHapticsGateway haptics = null,
            ISettingsReviewGateway review = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (openRateUs == null) throw new ArgumentNullException(nameof(openRateUs));
            return Install(factory, SettingsInstaller.Install(audio, platform, haptics, review), openRateUs);
        }

        public static ISettingsService Install(PanelPresenterFactory factory, ISettingsService service, Action openRateUs)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (openRateUs == null) throw new ArgumentNullException(nameof(openRateUs));
            factory.Register<SettingsPanel>(view => new SettingsPresenter(service, view, openRateUs));
            factory.Register<RateUsPanel>(view => new RateUsPresenter(service, view));
            return service;
        }
    }
}
