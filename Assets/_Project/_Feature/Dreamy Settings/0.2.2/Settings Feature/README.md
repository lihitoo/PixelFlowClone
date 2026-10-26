# Settings Feature

Production integration for the supplied passive SettingsPanel and RateUsPanel views. At the composition root, after audio/save readiness:

```csharp
var factory = new PanelPresenterFactory();
SettingsFeatureInstaller.Install(factory, audioService, OpenRateUs, platformGateway, hapticsGateway, reviewGateway);
PanelManager.Instance.PresenterFactory = factory;
```

One Install call installs the runtime service and registers both presenters. If the host already owns an ISettingsService, use `SettingsFeatureInstaller.Install(factory, settingsService, OpenRateUs)`. Runtime SettingsInstaller remains available for custom UI; callers using these panels only need SettingsFeatureInstaller.

Open from any caller through `PanelManager.Show<SettingsPanel>(address)` or `Transition<SettingsPanel>(address)`. In the sandbox, GameInstaller owns the combined Shop/Settings factory, attaches it to each scene's manager, and opens Rate Us using `Panel/RateUsPanel.prefab`. FoundationDemoRoot only opens views. Every opening owns one presenter; close/disable/destroy/failure releases it, and cached reopen creates a fresh presenter. Add Dreamy.UI.Presentation to asmdefs using the factory or IPanelPresenter.

Gateway choices are explicit; the installer never creates mock providers:

- SettingsPlatformGateway is a placeholder for future DreamySDK integration. All capabilities return false and operations return Unavailable until SDK logic is added. It performs no store launch, consent, purchase restoration, or review request.
- PlayerPrefsSettingsHapticsGateway persists the device-local preference and calls an injected real haptic provider. Without an installed haptic gateway, the toggle is unavailable. The constructor applies the saved preference to the provider.
- PlayerPrefsSettingsReviewGateway stores device-local positive rating/request and optional reward claim. Account-scoped games should supply their Datasave/backend implementation. Existing Dreamy.Settings.Sample preference keys are preserved for compatibility.

Review-linked reward remains off by default. Its opt-in example requires Android and an idempotent wallet; keep it disabled for Google Play ([policy](https://support.google.com/googleplay/android-developer/answer/9898684?hl=en)). No gateway claims to verify a published store review.

## Pixel Flow local patch, 2026-10-25

SettingsPanel.cs and SettingsPanel.prefab are edited directly; their GUIDs remain3a10b35bd2c7daf49b0024068d988aa6/aaf44ae2917c45749a7bdf79e64c2cc7. The authored popup uses the inherited BaseFeaturePanel SafeArea, source Lilita/font material, source sliders/toggles/icons and justified sliced frames. SliderCommit commits pointer/keyboard edits once instead of saving every drag frame. The view remains passive and keeps ISettingsView compatibility; the source-specific controls additionally expose typed commands and preferences.

The consumer root registers SettingsScreenPresenter because the pinned package presenter does not cover notification/smooth-motion preferences, attempt confirmation, save-error recovery or these source actions. It reads the existing ProgressStore, with no second settings service/save/installer. PanelManager owns cached popup lifecycle and presenter disposal. Source account/privacy/support/social actions explicitly report unavailable services; restore uses the owner-authorized fake Shop gateway. Both legacy procedural settings pages and HomeSliderCommit were removed. Preserve these local fields, SliderCommit and lifecycle bindings when merging upstream. Evidence and remaining parity/service gaps: Docs/ExactClone/Settings/review.html.
