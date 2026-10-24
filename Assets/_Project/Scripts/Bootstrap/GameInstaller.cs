using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.UI;
using UnityEngine;

namespace Dreamy.Template
{
    /// <summary>Scene-authored application composition root that owns service installation and cleanup.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed partial class GameInstaller : MonoBehaviour
    {
        [Header("Persistence")]
        [SerializeField] private bool prettySaveInEditor = true;
        [Tooltip("Optional game-specific composition. Leave empty for the template demo.")]
        [SerializeField] private MonoBehaviour gameCompositionProfile;
        private IGameCompositionProfile gameProfile;

        private CancellationTokenSource initializationCancellation;
        private static GameInstaller activeInstaller;

        public static BootstrapState State { get; private set; }
        public static Exception InitializationException { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeInstaller = null;
            State = BootstrapState.None;
            InitializationException = null;
        }

        private void Awake()
        {
            if (activeInstaller != null && activeInstaller != this)
            {
                // Reject only this component; a reloaded scene may also own GameInit.
                Destroy(this);
                return;
            }

            activeInstaller = this;
            gameProfile = gameCompositionProfile as IGameCompositionProfile;
            initializationCancellation = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            State = BootstrapState.Initializing;
            InitializationException = null;
            var cancellationToken = initializationCancellation.Token;
            try
            {
                if (gameCompositionProfile != null && gameProfile == null)
                    throw new InvalidOperationException("Game composition must implement IGameCompositionProfile.");
                // 1. Foundation: persistence, pooling and audio.
                InstallPersistence();
                InstallInfrastructure();

                // 2. Economy: one wallet shared by Shop and resource holders.
                if (gameProfile == null) InstallEconomy();

                // 3. Configuration: register every document before loading.
                await InstallConfigurationAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // 4. Features: all required services and config are now ready.
                presenterFactory = new PanelPresenterFactory();
                if (gameProfile == null) InstallFeatures();
                else gameProfile.Install(new GameCompositionContext(this, datasave, dataConfig, feedbackHost, presenterFactory, cancellationToken));
                InstallPresentation();
                cancellationToken.ThrowIfCancellationRequested();

                State = BootstrapState.Ready;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                TearDownServices();
            }
            catch (Exception exception)
            {
                TearDownServices();
                if (!cancellationToken.IsCancellationRequested)
                {
                    InitializationException = exception;
                    State = BootstrapState.Failed;
                }
                Debug.LogException(exception, this);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) datasave?.SaveAll();
        }

        private void OnApplicationQuit() => datasave?.SaveAll();

        public UniTask RetryAsync() => activeInstaller == this && State == BootstrapState.Failed
            ? InitializeAsync() : UniTask.CompletedTask;

        private void OnDestroy()
        {
            // An inactive component may be destroyed before Awake creates its lifetime.
            if (initializationCancellation == null) return;
            initializationCancellation.Cancel();
            State = BootstrapState.None;
            InitializationException = null;
            TearDownServices();
            initializationCancellation.Dispose();
            initializationCancellation = null;
            if (activeInstaller == this) activeInstaller = null;
        }
    }
}
