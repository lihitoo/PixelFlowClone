using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.DataConfig;
using Dreamy.Datasave;
using Dreamy.Core;
using Dreamy.Audio;
using Dreamy.Feedback;
using Dreamy.Template;
using PixelFlow.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace PixelFlow
{
    public enum AppScreen { Loading, Home, Playing, Paused, Won, Lost, Error, LevelSelect, Shop, Daily, Settings, Tutorial, Targeting, Confirm, HomePopup, BoosterPurchase }

    [DefaultExecutionOrder(-500)]
    public sealed partial class PixelFlowApp : MonoBehaviour
    {
        [SerializeField] private PixelFlowPresentation presentation;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Canvas uiCanvas;
        private CancellationTokenSource lifetime;
        private ProgressStore progress;
        private PixelFlowInstallerProfile foundation;
        private PixelFlowView view;
        private BoardPresenter board;
        private AudioHandle musicHandle;
        private MusicAudioFile currentMusic;
        private float lastPop = -100, resultAt = -1, nextCheckpoint;
        private string initializationError;
        private int requestedExitCode, terminalReward;
        private bool initializing, busy, terminalCommitted;
        private AppScreen returnScreen;
        private BoosterKind targetBooster;
        private FlowPage homePopup;
        private AppScreen settingsReturn;
        private long lastHomeSecond = -1;
        public PixelFlowCampaignConfig Campaign { get; private set; }
        public PuzzleSession Session { get; private set; }
        public AppScreen ScreenState { get; private set; } = AppScreen.Loading;
        public int SelectedLevel { get; private set; }
        public PixelFlowView View => view;
        public BoardPresenter Board => board;
        public ProgressStore Progress => progress;
        public Camera WorldCamera => worldCamera;
        public string SaveDirectory { get; private set; } = "PixelFlowCloneSaves";
        private void Awake()
        {
            if (HasCommandLine("-pixelFlowSmoke")) Application.runInBackground = true;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (Application.unityVersion == "6000.4.12f1") Application.wantsToQuit += ExitWindowsCompatibility;
#endif
        }
        public void RequestExit(int code = 0) { requestedExitCode = code; Application.Quit(code); }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
        private static extern IntPtr CurrentProcessHandle();
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr handle, uint code);
#endif
        private bool ExitWindowsCompatibility()
        {
            // Retain the verified version-scoped native teardown workaround.
            try { Checkpoint(); progress?.Save(); }
            catch (Exception error) { Debug.LogException(error, this); view?.Toast("Could not save. Please try closing again."); return false; }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!TerminateProcess(CurrentProcessHandle(), unchecked((uint)requestedExitCode))) Debug.LogError("Could not terminate owned player.");
#endif
            return false;
        }
        private void Start()
        {
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
            lifetime = new CancellationTokenSource(); InitializeAsync(lifetime.Token).Forget();
        }
        private async UniTask InitializeAsync(CancellationToken token)
        {
            if (initializing) return; initializing = true;
            try
            {
                initializationError = null;
                if (presentation == null || worldCamera == null || presentation.PigMesh == null || presentation.Font == null || presentation.LitMaterial == null)
                    throw new InvalidOperationException("Pixel Flow scene has incomplete presentation references.");
                if (FindAnyObjectByType<EventSystem>() == null)
                {
                    var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(transform, false);
                    events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                }
                if (worldCamera.GetComponent<PhysicsRaycaster>() == null) worldCamera.gameObject.AddComponent<PhysicsRaycaster>();
                if (uiCanvas == null || uiCanvas.GetComponent<Dreamy.UI.PanelManager>() == null)
                    throw new InvalidOperationException("Assign the authored Dreamy PanelManager Canvas.");
                // This consumer owns Input System back routing. The installed
                // manager's legacy Input.Update poll must not process it twice;
                // dynamic Create/Show still use the manager's normal API.
                uiCanvas.GetComponent<Dreamy.UI.PanelManager>().enabled = false;
                var homePanel = await Dreamy.UI.PanelManager.Instance.Create<Dreamy.Template.Home.HomePanel>(PixelFlowPanelAddress.Home);
                token.ThrowIfCancellationRequested();
                await homePanel.Show();
                var gameplayPanel = await Dreamy.UI.PanelManager.Instance.Create<GameplayPanel>(PixelFlowPanelAddress.Gameplay);
                token.ThrowIfCancellationRequested();
                view?.Dispose(); view = new PixelFlowView(presentation, 2600, uiCanvas, homePanel, gameplayPanel); BindView();
                if (GameInstaller.State == BootstrapState.Failed)
                    await FindFirstObjectByType<GameInstaller>().RetryAsync();
                await UniTask.WaitUntil(() => GameInstaller.State is BootstrapState.Ready or BootstrapState.Failed, cancellationToken: token);
                if (GameInstaller.State == BootstrapState.Failed) throw new InvalidOperationException("Foundation initialization failed.", GameInstaller.InitializationException);
                foundation = ServiceLocator.Get<PixelFlowInstallerProfile>();
                foundation.BindSceneController(this);
                Campaign = foundation.Campaign; progress = foundation.Progress; SaveDirectory = foundation.SaveDirectory;
                view.SetCampaignCount(Campaign.LevelCount);
                view.ConfigureHome(Campaign);
                InitializeShop(foundation);
                SelectedLevel = progress.Data.ActiveRun != null ? Math.Clamp(progress.Data.ActiveLevel, 0, progress.Data.UnlockedLevel) : progress.Data.UnlockedLevel;
                Home();
#if UNITY_EDITOR || DEVELOPMENT_BUILD || PIXEL_FLOW_VERIFICATION
                if (HasCommandLine("-pixelFlowSmoke") && GetComponent<PixelFlowSmoke>() == null) gameObject.AddComponent<PixelFlowSmoke>().Initialize(this);
#endif
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error)
            {
                Debug.LogException(error, this); ScreenState = AppScreen.Error;
                initializationError = "The game could not start. Existing progress has been kept.";
                view?.ShowConfirm("COULD NOT START", initializationError, "RETRY", () => InitializeAsync(lifetime.Token).Forget(), () => view.Toast("Progress has been kept. Retry when ready."), true);
            }
            finally { initializing = false; }
        }
        private void BindView()
        {
            view.PlayRequested += PlaySelected; view.PauseRequested += Pause;
            view.MenuRequested += CloseResult; view.NextRequested += Next;
            view.LevelSelected += SelectLevel;
            view.PageRequested += OpenPage;
            view.PurchaseRequested += Buy; view.DailyRequested += ClaimDaily;
            view.BoosterRequested += UseBooster; view.ContinueRequested += ContinueRun;
            view.HomeRequested += Home; view.RefillRequested += RefillLives;
            view.MiniShopRequested += OpenBoosterMiniShop;
            view.ShopGoldRequested += () => { if (!storeUiBusy) { returnScreen = AppScreen.Home; PresentShop(Home, false, "Gold"); } };
        }
        public static bool HasCommandLine(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
        public static string CommandLineValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        private void SelectLevel(int index)
        {
            if (ScreenState is not (AppScreen.LevelSelect or AppScreen.Home) || index < 0 || index > progress.Data.UnlockedLevel || index >= Campaign.LevelCount) return;
            SelectedLevel = index; ScreenState = AppScreen.Home; view.ShowHome(progress.Data, index); Play(presentation.Tap);
        }
        private void PlaySelected()
        {
            if (ScreenState != AppScreen.Home) return;
            if (!progress.CanStartLevel(ProgressStore.UtcSeconds)) { OpenPage(FlowPage.Lives); return; }
            var saved = progress.Data.ActiveRun;
            if (saved != null && progress.Data.ActiveLevel == SelectedLevel)
            {
                try
                {
                    var model = CreateSession(SelectedLevel); model.RestoreState(saved); AttachSession(model, SelectedLevel);
                    if (model.Status != SessionStatus.Playing) { resultAt = Time.unscaledTime; CommitTerminal(); }
                    else OfferTutorial();
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Saved run could not be restored: " + error.Message); ScreenState = AppScreen.Error;
                    view.ShowConfirm("SAVED RUN UNAVAILABLE", "This run does not match current level data. Coins, inventory and completed levels are kept.", "START FRESH", () => Guard(() => { progress.DiscardRun(); StartLevelAfterDiscard(); }), Home, true);
                }
            }
            else if (saved != null)
            {
                ScreenState = AppScreen.Confirm;
                view.ShowConfirm("START A DIFFERENT LEVEL?", "The current run will be replaced. Completed levels, coins and inventory are kept.", "START LEVEL", () => StartLevel(SelectedLevel), Home);
            }
            else StartLevel(SelectedLevel);
        }
        private PuzzleSession CreateSession(int index) => new(Campaign.GetLevel(index), Campaign.LoopSeconds, Campaign.ShotInterval);
        private void StartLevelAfterDiscard()
        {
            var model = CreateSession(SelectedLevel); progress.Checkpoint(SelectedLevel, model.CaptureState()); AttachSession(model, SelectedLevel); OfferTutorial();
        }
        private void StartLevel(int index)
        {
            if (busy || Campaign == null || index < 0 || index >= Campaign.LevelCount || index > progress.Data.UnlockedLevel) return;
            if (!progress.CanStartLevel(ProgressStore.UtcSeconds)) { Home(); OpenPage(FlowPage.Lives); return; }
            Guard(() => { var model = CreateSession(index); progress.Checkpoint(index, model.CaptureState()); AttachSession(model, index); OfferTutorial(); });
        }
        private void AttachSession(PuzzleSession model, int index)
        {
            ClearSession(); SelectedLevel = index; Session = model; Session.Finished += OnFinished; Session.FeatureChanged += FeatureFeedback;
            board = new BoardPresenter(transform, worldCamera, presentation, Campaign, Session, Launch, PlayPop, SelectBoardColor);
            terminalCommitted = false; resultAt = model.Status == SessionStatus.Playing ? -1 : Time.unscaledTime + .1f;
            nextCheckpoint = Time.unscaledTime + 5; ScreenState = AppScreen.Playing; view.ShowGame(index); UpdateHud(); UpdateMusic(); Play(presentation.Tap);
        }
        private void OfferTutorial()
        {
            if (progress.Data.Tutorials.Contains("basics-v2")) { OfferFeatureTutorial(); return; }
            ScreenState = AppScreen.Tutorial;
            view.ShowHelp(() => Guard(() => { progress.MarkTutorial("basics-v2"); ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); OfferFeatureTutorial(); }));
        }
        private void OfferFeatureTutorial()
        {
            foreach (string feature in Campaign.Entry(SelectedLevel).Features)
            {
                string key = "feature-v2-" + feature;
                if (progress.Data.Tutorials.Contains(key)) continue;
                ScreenState = AppScreen.Tutorial;
                Action close = () => Guard(() => { progress.MarkTutorial(key); ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); OfferFeatureTutorial(); });
                view.ShowConfirm("NEW: " + feature.ToUpperInvariant(), FeatureGuide.Text(feature), "GOT IT", close, close); return;
            }
        }
        private void Launch(int id)
        {
            if (ScreenState == AppScreen.Targeting)
            {
                if (targetBooster == BoosterKind.Hand && Session.Shooters.TryGetValue(id, out var pig) && pig.Location == ShooterLocation.Queue) ApplyTarget(id);
                return;
            }
            if (ScreenState != AppScreen.Playing || Session?.Status != SessionStatus.Playing) return;
            var result = Session.Launch(id);
            if (result == LaunchResult.Accepted) { Play(presentation.Launch, .5f); Haptic(); }
            else view.Toast(result == LaunchResult.ConveyorFull ? "Conveyor full. Wait for a pig to finish." : result == LaunchResult.FeatureBlocked ? "This pig is blocked. Clear its condition first." : "Choose an available front or waiting pig.");
        }
        private void Pause()
        {
            if (ScreenState == AppScreen.Targeting) view.Back();
            if (ScreenState != AppScreen.Playing || Session?.Status != SessionStatus.Playing) return;
            Guard(Checkpoint);
            if (ScreenState == AppScreen.Playing) PresentSettings();
        }
        private void Resume()
        {
            if (ScreenState != AppScreen.Paused) return;
            ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); Play(presentation.Tap);
        }
        private void Restart() { if (ScreenState is AppScreen.Paused or AppScreen.Won or AppScreen.Lost) StartLevel(SelectedLevel); }
        private void Home()
        {
            if (nativeShopView != null) { Dreamy.UI.PanelManager.Instance.Close<Dreamy.Feature.Shop.Integration.ShopPanel>().Forget(); return; }
            if (storeUiBusy) return;
            if (progress == null) return;
            Guard(() => { Checkpoint(); ClearSession(); ScreenState = AppScreen.Home; view.ShowHome(progress.Data, SelectedLevel); UpdateMusic(); });
        }
        private void Next()
        {
            if (ScreenState != AppScreen.Won) return;
            if (SelectedLevel + 1 >= Campaign.LevelCount) Home(); else StartLevel(SelectedLevel + 1);
        }
        private void CloseResult()
        {
            if (ScreenState == AppScreen.Won) { Home(); return; }
            if (ScreenState != AppScreen.Lost) return;
            Guard(() =>
            {
                progress.AbandonRun(ProgressStore.UtcSeconds); ClearSession();
                ScreenState = AppScreen.Home; view.ShowHome(progress.Data, SelectedLevel); UpdateMusic();
            });
        }
        private void OnFinished(SessionStatus status) => resultAt = Time.unscaledTime + .55f;
        private void CommitTerminal()
        {
            if (terminalCommitted || Session == null || Session.Status == SessionStatus.Playing) return;
            int reward = 0;
            if (Session.Status == SessionStatus.Won) reward = progress.RecordWin(SelectedLevel, Session.RunId);
            else progress.RecordLoss(SelectedLevel, Session.CaptureState());
            terminalReward = reward; terminalCommitted = true; ScreenState = Session.Status == SessionStatus.Won ? AppScreen.Won : AppScreen.Lost;
            view.ShowResult(ScreenState == AppScreen.Won, SelectedLevel + 1, terminalReward, Session.CanRevive, Campaign.Economy.ContinueCost, Session.LossReason);

            if (ScreenState == AppScreen.Won) Play(presentation.Win, .65f);
        }
        private void Checkpoint()
        {
            if (Session == null || terminalCommitted && Session.Status == SessionStatus.Won) return;
            if (Session.Status == SessionStatus.Playing) progress.Checkpoint(SelectedLevel, Session.CaptureState());
            else if (!terminalCommitted) CommitTerminal();
        }
        private void ContinueRun()
        {
            if (ScreenState != AppScreen.Lost || !Session.CanRevive) return;
            if (progress.Data.Coins < Campaign.Economy.ContinueCost) { PresentShop(() => { ScreenState = AppScreen.Lost; RestoreScreen(); }, true, "Gold"); return; }
            MutateRun(() => Session.Revive(), snapshot => progress.SpendContinue(SelectedLevel, snapshot));
        }
        private void UseBooster(BoosterKind kind)
        {
            if (ScreenState != AppScreen.Playing || Session?.Status != SessionStatus.Playing || busy) return;
            if ((int)kind < 4 && progress.Amount(kind) == 0) { Guard(Checkpoint); if (ScreenState != AppScreen.Playing) return; boosterBuyKind = kind; ScreenState = AppScreen.BoosterPurchase; ShowBoosterPurchase(); return; }
            if (kind is BoosterKind.Hand or BoosterKind.Color)
            {
                targetBooster = kind; ScreenState = AppScreen.Targeting; view.ShowTargets(Session, kind == BoosterKind.Hand, Campaign, () => { ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); }); return;
            }
            if (kind == BoosterKind.Unicorn && !progress.UnicornActive) { view.Toast("Win 10 levels in a row to activate Unicorn."); return; }
            MutateRun(() => kind switch { BoosterKind.Tray => Session.AddTray(), BoosterKind.Shuffle => Session.Shuffle(unchecked(Session.HitCount * 397 ^ Session.Launches ^ Environment.TickCount)), BoosterKind.Unicorn => Session.UseUnicorn(), _ => false }, snapshot => progress.SpendBooster(kind, SelectedLevel, snapshot));
        }
        private void SelectBoardColor(int x, int y)
        {
            if (ScreenState != AppScreen.Targeting || targetBooster != BoosterKind.Color) return;
            int color = Session.TargetColorAt(x, y);
            if (color >= 0) ApplyTarget(color);
        }
        private void ApplyTarget(int target)
        {
            if (ScreenState != AppScreen.Targeting) return;
            MutateRun(() => targetBooster == BoosterKind.Hand ? Session.Launch(target, true) == LaunchResult.Accepted : Session.ClearVisibleColor(target), snapshot => progress.SpendBooster(targetBooster, SelectedLevel, snapshot));
        }
        private void MutateRun(Func<bool> mutate, Func<SessionSnapshot, bool> persist)
        {
            if (busy || Session == null) return;
            var before = Session.CaptureState(); float previousResult = resultAt; busy = true;
            try
            {
                if (!mutate()) { view.Toast("No valid effect right now. Nothing spent."); return; }
                if (!persist(Session.CaptureState())) { Session.RestoreState(before); resultAt = previousResult; view.Toast("Nothing spent. Check your coins or inventory."); return; }
                terminalCommitted = false; ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); Play(presentation.Launch); Haptic();
            }
            catch (Exception error) { Session.RestoreState(before); resultAt = previousResult; SaveError(error, () => MutateRun(mutate, persist)); }
            finally { busy = false; board?.Render(0); UpdateHud(); }
        }
        private void OpenPage(FlowPage page)
        {
            if (storeUiBusy) return;
            if (page == FlowPage.Shop && ScreenState == AppScreen.HomePopup)
            {
                if (livesPanel != null) { LivesShop(); return; }
                var destination = homePopup;
                PresentShop(() => { homePopup = destination; ScreenState = AppScreen.HomePopup; ShowHomePopup(); }, true, "Gold"); return;
            }
            bool fromHome = ScreenState == AppScreen.Home || ScreenState == AppScreen.Shop && returnScreen == AppScreen.Home || ScreenState == AppScreen.HomePopup;
            if (fromHome && page == FlowPage.NoAds) { returnScreen = AppScreen.Home; PresentShop(Home, false, "NoAds"); return; }
            if (fromHome && page is FlowPage.Lives or FlowPage.Rank or FlowPage.Pass or FlowPage.FireQuest or FlowPage.Arena)
            {
                homePopupReturn = ScreenState == AppScreen.Shop ? AppScreen.Shop : AppScreen.Home;
                returnScreen = AppScreen.Home; homePopup = page; ScreenState = AppScreen.HomePopup; ShowHomePopup(); return;
            }
            if (page == FlowPage.Settings && ScreenState is AppScreen.Home or AppScreen.Shop or AppScreen.Playing or AppScreen.Paused or AppScreen.Won or AppScreen.Lost)
            { PresentSettings(); return; }
            if (fromHome && page == FlowPage.Shop)
            {
                returnScreen = AppScreen.Home; PresentShop(Home, false, "Top"); return;
            }
            if (ScreenState is not (AppScreen.Home or AppScreen.Playing or AppScreen.Paused or AppScreen.Won or AppScreen.Lost)) return;
            returnScreen = ScreenState; Action close = () => { ScreenState = returnScreen; RestoreScreen(); };
            switch (page)
            {
                case FlowPage.Levels: ScreenState = AppScreen.LevelSelect; view.ShowLevels(progress, Campaign, SelectedLevel, close); break;
                case FlowPage.Shop: PresentShop(close, true, "Gold"); break;
                case FlowPage.Daily: ScreenState = AppScreen.Daily; view.ShowDaily(progress, ProgressStore.UtcDay(DateTime.UtcNow), close); break;
                case FlowPage.Help: ScreenState = AppScreen.Tutorial; view.ShowHelp(close); break;
            }
        }
        private void RestoreScreen()
        {
            switch (ScreenState)
            {
                case AppScreen.Home: view.ShowHome(progress.Data, SelectedLevel); break;
                case AppScreen.Playing: view.ShowGame(SelectedLevel); break;
                case AppScreen.Paused: ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); break;
                case AppScreen.Won: case AppScreen.Lost:
                    view.ShowResult(ScreenState == AppScreen.Won, SelectedLevel + 1, terminalReward, Session.CanRevive, Campaign.Economy.ContinueCost, Session.LossReason); break;
                case AppScreen.Shop: RestoreFoundationShop(); break;
                case AppScreen.BoosterPurchase: ShowBoosterPurchase(); break;
                case AppScreen.Daily: view.ShowDaily(progress, ProgressStore.UtcDay(DateTime.UtcNow), () => { ScreenState = returnScreen; RestoreScreen(); }); break;
                case AppScreen.Settings: view.HideFoundationModal(); OpenSettingsAsync().Forget(); break;
                case AppScreen.HomePopup: ShowHomePopup(); break;
                case AppScreen.Targeting: view.ShowTargets(Session, targetBooster == BoosterKind.Hand, Campaign, () => { ScreenState = AppScreen.Playing; view.ShowGame(SelectedLevel); }); break;
                case AppScreen.Tutorial: OfferTutorial(); break;
                case AppScreen.Confirm: ScreenState = Session == null ? AppScreen.Home : AppScreen.Paused; RestoreScreen(); break;
            }
        }
        private void ShowHomePopup()
        {
            if (homePopupReturn == AppScreen.Home) view.ShowHome(progress.Data, SelectedLevel);
            Action close = () => { ScreenState = homePopupReturn; RestoreScreen(); };
            if (homePopup == FlowPage.Lives) { if (livesPanel == null) OpenLivesAsync().Forget(); }
            else view.ShowHomeDestination(homePopup, progress, close);
        }
        public void RefillLives()
        {
            if (ScreenState != AppScreen.HomePopup || homePopup != FlowPage.Lives || openingLives || closingLives) return;
            Guard(() => { bool refill = progress.RefillLives(ProgressStore.UtcSeconds); view.UpdateHomeProfile(progress.Data); ShowHomePopup(); if (!refill) view.Toast("Lives are full or more coins are needed."); });
        }
        private void Buy(BoosterKind kind)
        {
            if (ScreenState == AppScreen.BoosterPurchase) { BuyBoosterFromPopup(kind); return; }
            if (ScreenState != AppScreen.Shop) return;
            Guard(() => { bool bought = progress.Buy(kind); RestoreScreen(); view.Toast(bought ? "Booster added to inventory." : "Not enough coins or inventory full."); });
        }
        private void ClaimDaily()
        {
            if (ScreenState != AppScreen.Daily) return;
            Guard(() => { bool claimed = progress.ClaimDaily(ProgressStore.UtcDay(DateTime.UtcNow)); RestoreScreen(); view.Toast(claimed ? "Daily coins received!" : "Already claimed today."); });
        }
        private void Guard(Action action)
        {
            if (busy) return; busy = true;
            try { action(); }
            catch (Exception error) { SaveError(error, () => { busy = false; Guard(action); }); }
            finally { busy = false; }
        }
        private void SaveError(Exception error, Action retry)
        {
            Debug.LogWarning("Pixel Flow operation failed: " + error.Message);
            var previous = ScreenState; ScreenState = AppScreen.Error;
            view.ShowConfirm("COULD NOT SAVE", "The action was not committed. Previous saved progress is kept. Check storage and try again.", "RETRY", () => { ScreenState = previous; retry(); if (ScreenState == previous && !closingSettings) RestoreScreen(); }, () =>
            {
                if (Session != null && Session.Status != SessionStatus.Playing && !terminalCommitted) { view.Toast("Retry saving to record this result."); return; }
                ScreenState = previous; RestoreScreen();
            }, true);
        }
        private void Update()
        {
            if (worldCamera != null) worldCamera.orthographicSize = Mathf.Max(8.4f, 4.55f / worldCamera.aspect);
            view?.Tick();
            if (storeUiBusy && Shop?.Gateway.HasDeferred == true) view?.UpdateStoreWaiting();
            if (progress != null && (ScreenState is AppScreen.Home or AppScreen.Shop or AppScreen.HomePopup))
            {
                long second = ProgressStore.UtcSeconds;
                if (lastHomeSecond != second)
                {
                    lastHomeSecond = second;
                    RefreshFoundationShopHeader();
                    if (Shop != null && !Shop.Busy && Shop.Gateway.Unfulfilled.Count > 0)
                        Guard(() => { Shop.Recover(); view.UpdateHomeProfile(progress.Data); });
                    Guard(() => { if (progress.RefreshLives(second)) { view.UpdateHomeProfile(progress.Data); if (ScreenState == AppScreen.HomePopup && homePopup == FlowPage.Lives) ShowHomePopup(); } });
                }
            }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (ScreenState == AppScreen.Shop && nativeShopView != null && !view.IsHomeModalVisible)
                    Dreamy.UI.PanelManager.Instance.Close<Dreamy.Feature.Shop.Integration.ShopPanel>().Forget();
                else if (ScreenState == AppScreen.HomePopup && livesPanel != null && !view.IsHomeModalVisible) CloseLives();
                else if (ScreenState == AppScreen.Settings && settingsPanel != null && !view.IsHomeModalVisible) settingsPanel.Back();
                else if (ScreenState == AppScreen.Playing) Pause(); else if (ScreenState == AppScreen.Paused) Resume();
                else if (ScreenState is AppScreen.Won or AppScreen.Lost) CloseResult(); else if (ScreenState != AppScreen.Home) view?.Back();
            }
            if (Session == null || view == null) return;
            if (ScreenState == AppScreen.Playing) Session.Tick(Time.deltaTime);
            board?.SetTargeting(ScreenState == AppScreen.Targeting && targetBooster == BoosterKind.Hand, ScreenState == AppScreen.Targeting && targetBooster == BoosterKind.Color);
            board?.Render(ScreenState == AppScreen.Playing ? Time.deltaTime : 0); UpdateHud();
            if (ScreenState == AppScreen.Playing && Session.Status != SessionStatus.Playing && resultAt >= 0 && Time.unscaledTime >= resultAt) Guard(CommitTerminal);
            else if (ScreenState == AppScreen.Playing && Time.unscaledTime >= nextCheckpoint)
            { nextCheckpoint = Time.unscaledTime + 5; Guard(Checkpoint); }
        }
        private void UpdateHud() { if (Session != null && view != null) { view.UpdateGame(Session, worldCamera); view.UpdateBoosters(progress, Session); } }
        private void FeatureFeedback(string message) { view.Toast(message); Play(Resources.Load<AudioClip>("PixelFlow/Audio/Feature"), .35f); }
        private void PlayPop() { if (Time.unscaledTime - lastPop < .045f) return; lastPop = Time.unscaledTime; Play(presentation.Pop, .35f); }
        private void Play(AudioClip clip, float volume = .55f)
        {
            if (clip == null || foundation == null || progress == null || !progress.Data.SoundEnabled) return;
            var cue = foundation.AudioCues.Resolve(clip, volume);
            if (cue != null) foundation.Audio.Play(cue);
        }
        private void UpdateMusic()
        {
            if (foundation == null) return;
            var cue = Session == null ? foundation.AudioCues.MenuMusic : foundation.AudioCues.GameMusic;
            if (cue == currentMusic && musicHandle.IsValid) return;
            if (musicHandle.IsValid) foundation.Audio.Stop(musicHandle);
            currentMusic = cue; musicHandle = cue == null ? AudioHandle.Invalid : foundation.Audio.PlayMusic(cue, new AudioTransition(.2f));
        }
        private void ApplyAudioSettings()
        {
            if (foundation == null) return;
            foundation.Audio.SetVolume(AudioBusId.Music, progress.Data.MasterVolume * (progress.Data.MusicEnabled ? progress.Data.MusicVolume : 0), false);
            foundation.Audio.SetVolume(AudioBusId.Sfx, progress.Data.MasterVolume * (progress.Data.SoundEnabled ? progress.Data.SoundVolume : 0), false);
            UpdateMusic();
        }
        private void Haptic()
        {
            if (progress.Data.HapticsEnabled) foundation?.Context.Feedback.Haptic?.Play(HapticType.Light);
        }
        private void ClearSession()
        {
            if (Session != null) { Session.Finished -= OnFinished; Session.FeatureChanged -= FeatureFeedback; }
            board?.Dispose(); board = null; Session = null;
            foundation?.Audio.StopBus(AudioBusId.Sfx);
            lastPop = -100; resultAt = -1; terminalCommitted = false;
        }
        private void SaveProgress() { try { Checkpoint(); progress?.Save(); } catch (Exception error) { Debug.LogWarning("Could not save progress: " + error.Message); } }
        private void OnApplicationPause(bool paused) { if (paused) { Pause(); SaveProgress(); } }
        private void OnApplicationQuit() => SaveProgress();
        private void OnDestroy()
        {
            Application.wantsToQuit -= ExitWindowsCompatibility; foundation?.ReleaseSceneController(this); lifetime?.Cancel(); ClearSession(); view?.Dispose(); lifetime?.Dispose(); lifetime = null;
        }
        private void OnGUI()
        {
            if (initializationError == null || view != null) return;
            GUI.Box(new Rect(20, 20, UnityEngine.Screen.width - 40, 120), initializationError);
            if (GUI.Button(new Rect(40, 70, 220, 50), "RETRY")) InitializeAsync(lifetime.Token).Forget();
        }
    }
}
