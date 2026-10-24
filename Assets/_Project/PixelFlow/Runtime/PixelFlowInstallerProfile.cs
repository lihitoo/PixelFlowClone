using System;
using Dreamy.Audio;
using Dreamy.DataConfig;
using Dreamy.Datasave;
using Dreamy.Economy;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Shop;
using Dreamy.Template;
using UnityEngine;

namespace PixelFlow
{
    public sealed class PixelFlowInstallerProfile : MonoBehaviour, IGameCompositionProfile
    {
        [SerializeField] private PixelFlowAudioCues audioCues;
        private bool audioInitialized;
        private PixelFlowApp appController;
        public void BindSceneController(PixelFlowApp controller) => appController = controller;
        public void ReleaseSceneController(PixelFlowApp controller) { if (appController == controller) appController = null; }
        public PixelFlowCampaignConfig Campaign { get; private set; }
        public ProgressStore Progress { get; private set; }
        public PixelFlowShopService Shop { get; private set; }
        public PixelFlowFoundationShopService FoundationShop { get; private set; }
        public PixelFlowResourceWallet Wallet { get; private set; }
        public IAudioService Audio { get; private set; }
        public PixelFlowAudioCues AudioCues => audioCues;
        public GameCompositionContext Context { get; private set; }
        public string SaveDirectory { get; private set; }
        public DatasaveOptions CreateSaveOptions(bool prettyPrint)
        {
            SaveDirectory = "PixelFlowCloneSaves";
            string scope = PixelFlowApp.CommandLineValue("-pixelFlowSaveScope");
#if UNITY_EDITOR
            scope ??= Environment.GetEnvironmentVariable("PIXEL_FLOW_EDITOR_SAVE_SCOPE");
#endif
            if (scope != null)
            {
                if (scope.Length > 64 || !System.Text.RegularExpressions.Regex.IsMatch(scope, "^[A-Za-z0-9_-]+$")) throw new ArgumentException("Invalid isolated save scope.");
                SaveDirectory = "PixelFlowCloneSmoke_" + scope;
            }
            return new DatasaveOptions { DirectoryName = SaveDirectory, PrettyPrint = prettyPrint, Codec = new PlainTextSaveCodec() };
        }
        public IDataConfigSource CreateConfigSource() => new ResourcesJsonConfigSource("PixelFlow");
        public void RegisterConfiguration(IDataConfigService configuration)
        {
            configuration.Register<PixelFlowCampaignConfig>("pixelFlowCampaign");
            configuration.Register<PixelFlowShopCatalog>("pixelFlowShop");
        }
        public void Install(GameCompositionContext context)
        {
            Context = context; Campaign = context.Configuration.GetTable<PixelFlowCampaignConfig>();
            Progress = new ProgressStore(context.Saves, Campaign.LevelCount, Campaign.Economy, Campaign.Home);
            Wallet = new PixelFlowResourceWallet(Progress);
            context.Register(Progress); context.Register<IResourceWallet>(Wallet, true);
            context.Register<IResourceBalanceProvider>(Wallet); context.Register<IResourceBalanceSource>(Wallet);
            var catalog = context.Configuration.GetTable<PixelFlowShopCatalog>();
            Shop = new PixelFlowShopService(Progress, catalog, Campaign.Home, new FakeShopGateway(context.Saves, catalog));
            Shop.Recover(); FoundationShop = new PixelFlowFoundationShopService(Shop, Progress, Campaign.Home, context.Lifetime);
            context.Register(Shop); context.Register<IShopPurchaseGateway>(Shop.Gateway); context.Register<IShopService>(FoundationShop);
            ShopFeatureInstaller.Install(context.Presenters, FoundationShop, context.Feedback,
                panel => new PixelFlowFoundationShopPresenter(FoundationShop, panel, appController != null ? appController : throw new InvalidOperationException("MainScene controller is not bound.")));
            if (audioCues == null || audioCues.Profile == null) throw new InvalidOperationException("Assign the Pixel Flow Dreamy Audio profile/cues.");
            Audio = DreamyAudio.Service;
            if (!audioInitialized) { Audio.Initialize(audioCues.Profile); audioInitialized = true; }
            context.Register<IAudioService>(Audio);
            void ApplyAudio()
            {
                var p = Progress.Data;
                Audio.SetMuted(AudioBusId.Music, false, false);
                Audio.SetMuted(AudioBusId.Sfx, false, false);
                Audio.SetVolume(AudioBusId.Music, p.MasterVolume * (p.MusicEnabled ? p.MusicVolume : 0), false);
                Audio.SetVolume(AudioBusId.Sfx, p.MasterVolume * (p.SoundEnabled ? p.SoundVolume : 0), false);
            }
            ApplyAudio(); Progress.Changed += ApplyAudio;
            context.OnTeardown(() =>
            {
                Progress.Changed -= ApplyAudio;
                foreach (var bus in Audio.Buses) Audio.StopBus(bus.Id);
            });
            context.Register(this);
        }
    }
}
