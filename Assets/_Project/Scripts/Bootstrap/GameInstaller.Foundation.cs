using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Audio;
using Dreamy.Core.Pooling;
using Dreamy.DataConfig;
using Dreamy.Datasave;
using Dreamy.Economy;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Feedback;
using Dreamy.Template.Pooling;
using UnityEngine;

namespace Dreamy.Template
{
    public sealed partial class GameInstaller
    {
        [Header("Feedback")]
        [SerializeField] private FeedbackHost feedbackHostPrefab;

        private FeedbackHost feedbackHost;
        private IDatasaveService datasave;
        private IDataConfigService dataConfig;
        private IResourceWallet wallet;
        private IResourceBalanceSource walletBalanceSource;

        private void InstallPersistence()
        {
            if (gameProfile != null)
            {
                datasave = new DatasaveService(gameProfile.CreateSaveOptions(Application.isEditor && prettySaveInEditor));
                RegisterService<IDatasaveService>(datasave, ownsInstance: true);
                return;
            }
            ISaveCodec codec;
#if UNITY_EDITOR
            codec = new PlainTextSaveCodec();
#else
            codec = new XorSaveCodec("Dreamy123@");
#endif

            datasave = new DatasaveService(new DatasaveOptions
            {
                PrettyPrint = Application.isEditor && prettySaveInEditor,
                Codec = codec
            });
            RegisterService<IDatasaveService>(datasave, ownsInstance: true);
        }

        private void InstallInfrastructure()
        {
            RegisterService<IPoolService>(new LeanPoolService(), ownsInstance: true);
            // The Audio package owns the instance; only undo our registration.
            if (gameProfile == null) RegisterService<IAudioService>(DreamyAudio.Service);
            InstallFeedback();
        }

        private void InstallFeedback()
        {
            if (feedbackHostPrefab == null)
                throw new System.InvalidOperationException("GameInstaller needs a FeedbackHost prefab.");

            var host = Instantiate(feedbackHostPrefab, transform);
            feedbackHost = host;
            // Host owns its services. Unregister them before shutting down the rig.
            teardownActions.Add(() =>
            {
                if (host == null) return;
                host.Shutdown();
                if (Application.isPlaying) Destroy(host.gameObject);
                else DestroyImmediate(host.gameObject);
            });
            host.Initialize();
            RegisterService<FeedbackHost>(host);
            RegisterService<IFeedbackService>(host.Feedback);
            RegisterService<IVfxService>(host.Vfx);
            RegisterService<IFloatingTextService>(host.FloatingText);
            RegisterService<IUiFeedbackService>(host.Ui);
            if (host.Haptic != null) RegisterService<IHapticService>(host.Haptic);
            if (host.IconFly != null) RegisterService<IIconFlyService>(host.IconFly);
            if (host.Screen != null) RegisterService<IScreenFeedbackService>(host.Screen);
            if (host.CameraShake != null) RegisterService<ICameraShakeService>(host.CameraShake);
        }

        private void InstallEconomy()
        {
            var resourceWallet = new DatasaveResourceWallet(datasave);
            wallet = resourceWallet;
            walletBalanceSource = resourceWallet;
            RegisterService<IResourceWallet>(resourceWallet, ownsInstance: true);
            RegisterService<IResourceBalanceProvider>(resourceWallet);
        }

        private async UniTask InstallConfigurationAsync(CancellationToken cancellationToken)
        {
            dataConfig = new DataConfigService(gameProfile?.CreateConfigSource() ?? new ResourcesJsonConfigSource());
            if (gameProfile == null)
            {
                dataConfig.Register<TemplateConfig>("templateConfig");
                dataConfig.Register<TestConfigTable>("testConfigs");
                ShopFeatureInstaller.RegisterConfig(dataConfig);
            }
            else gameProfile.RegisterConfiguration(dataConfig);

            RegisterService<IDataConfigService>(dataConfig, ownsInstance: true);
            await dataConfig.InitializeAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
