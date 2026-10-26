using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dreamy.Datasave;
using NUnit.Framework;
using PixelFlow.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PixelFlow.EditorReview
{
    // These are Editor fixtures. EnterPlayMode exercises the actual scene without
    // building a player. Offscreen captures explicitly change canvas render mode;
    // they are layout previews, not Game View, device or original-game evidence.
    public sealed class PresentationReviewTests
    {
        [SerializeField] private string previousScope;
        [SerializeField] private string scope;
        private Mouse pointer;
        private readonly List<InputDevice> suspendedDevices = new();
        private InputSettings previousInputSettings, ownedInputSettings;
        private PixelFlowApp app;
        private readonly List<string> checks = new();
        private string folder;

        [SetUp]
        public void PrepareScene()
        {
            // EnterPlayMode/ExitPlayMode reload the domain and replay NUnit setup.
            // SessionState survives that transition; never reopen/reset a live scene.
            if (SessionState.GetBool("PixelFlow.Review.Prepared", false))
            {
                scope = SessionState.GetString("PixelFlow.Review.Scope", "");
                previousScope = SessionState.GetString("PixelFlow.Review.PreviousScope", "");
                return;
            }
            Assert.That(Application.isPlaying, Is.False);
            // Device Simulator intercepts synthetic Mouse events even with a
            // floating Game View. Temporarily release it, then restore its window.
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.GetType().FullName.Contains("SimulatorWindow"))
                {
                    SessionState.SetString("PixelFlow.Review.SimulatorType", window.GetType().AssemblyQualifiedName);
                    window.Close();
                }
            previousScope = Environment.GetEnvironmentVariable("PIXEL_FLOW_EDITOR_SAVE_SCOPE");
            scope = "Review_" + Guid.NewGuid().ToString("N");
            SessionState.SetBool("PixelFlow.Review.Prepared", true);
            SessionState.SetString("PixelFlow.Review.Scope", scope);
            SessionState.SetString("PixelFlow.Review.PreviousScope", previousScope ?? "");
            Environment.SetEnvironmentVariable("PIXEL_FLOW_EDITOR_SAVE_SCOPE", scope);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/BootstrapScene.unity");
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }

        private IEnumerator Ready()
        {
            // Interactive Editor checks also need Update while the controlling
            // terminal owns focus. This runtime flag resets when Play Mode ends.
            Application.runInBackground = true;
            // Restored Device Simulator windows can switch the Game View preset.
            // Establish the owner's reference resolution for every fixture before
            // creating its pointer, rather than inheriting a previous window size.
            Type.GetType("PixelFlow.Editor.HomeReviewCommands, PixelFlow.Editor", true).GetMethod("Portrait").Invoke(null, null);
            float resolutionDeadline = Time.realtimeSinceStartup + 10;
            while ((Screen.width != 1080 || Screen.height != 1920) && Time.realtimeSinceStartup < resolutionDeadline) yield return null;
            Assert.That(new Vector2Int(Screen.width, Screen.height), Is.EqualTo(new Vector2Int(1080, 1920)), "Game View reference resolution settled");
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "Editor review requires a graphics device; -nographics cannot verify rendered pixels.");
            float deadline = Time.realtimeSinceStartup + 30;
            while (Time.realtimeSinceStartup < deadline)
            {
                app = Object.FindFirstObjectByType<PixelFlowApp>();
                if (app != null && app.ScreenState == AppScreen.Home && Dreamy.Template.SceneLoader.HasInstance && !Dreamy.Template.SceneLoader.Instance.IsPresenting) break;
                yield return null;
            }
            Assert.That(app, Is.Not.Null, "Scene bootstrap exists");
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home), "Scene reaches Home");
            Assert.That(Dreamy.Template.SceneLoader.Instance.IsPresenting, Is.False, "Template loading completes before input");
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, Is.EqualTo("MainScene"));
            Assert.That(Object.FindObjectsByType<Dreamy.Template.GameInstaller>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(app.SaveDirectory, Is.EqualTo("PixelFlowCloneSmoke_" + scope), "Never touch the owner's normal save");
            Assert.That(app.Progress.Data.UnlockedLevel, Is.Zero, "Fixture starts with a fresh save");
            string run = Environment.GetEnvironmentVariable("PIXEL_FLOW_REVIEW_RUN") ?? "editor-review";
            Assert.That(System.Text.RegularExpressions.Regex.IsMatch(run, "^[A-Za-z0-9_-]{1,64}$"), Is.True);
            folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Evidence/editor-review", run));
            Directory.CreateDirectory(folder);
            previousInputSettings = InputSystem.settings;
            ownedInputSettings = Object.Instantiate(previousInputSettings);
            ownedInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            // InputSystemUIInputModule otherwise discards pointer state while a
            // batch Editor has no focused Game View. Change only the owned clone.
            ownedInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            // EditMode's EnterPlayMode runner does not pump the queued device
            // events like a normally launched player. Own and pump them explicitly.
            ownedInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = ownedInputSettings;
            // Desktop input must not move the UI pointer or trigger Escape while
            // a fixture runs. This affects only this batch Editor's Input System,
            // never OS devices; restore every device below.
            foreach (var device in InputSystem.devices)
                if (device.enabled)
                {
                    InputSystem.DisableDevice(device);
                    suspendedDevices.Add(device);
                }
            pointer = InputSystem.AddDevice<Mouse>("PixelFlowEditorReviewPointer");
            checks.Clear();
            checks.Add("Actual scene bootstrap reaches Home using an isolated fresh save");
            yield return null;
        }

        private IEnumerator Click(Vector2 position)
        {
            EnsureBatchUiRendered();
            InputSystem.QueueStateEvent(pointer, new MouseState { position = position }.WithButton(MouseButton.Left));
            InputSystem.Update();
            EventSystem.current.currentInputModule.Process();
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(pointer, new MouseState { position = position });
            InputSystem.Update();
            EventSystem.current.currentInputModule.Process();
            yield return null;
            yield return null;
        }

        private IEnumerator Click(Button button)
        {
            // A render-preview capture temporarily changes root-canvas geometry.
            // Let the normal overlay layout settle before deriving pointer coords.
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            bool invoked = false;
            UnityEngine.Events.UnityAction observe = () => invoked = true;
            button.onClick.AddListener(observe);
            yield return Click(point);
            button.onClick.RemoveListener(observe);
            // Shop Hide can start Lives Show in its return callback. Await the
            // whole handoff, not two one-time snapshots in the opposite order.
            float transitionDeadline = Time.realtimeSinceStartup + 10;
            while ((app.IsLivesTransitioning || app.IsShopTransitioning) && Time.realtimeSinceStartup < transitionDeadline) yield return null;
            Assert.That(app.IsLivesTransitioning, Is.False, "Authored Lives Show/Hide completed");
            Assert.That(app.IsShopTransitioning, Is.False, "Shop show/hide finished before the next pointer action");
            if (app.ScreenState == AppScreen.Shop)
            {
                float readyDeadline = Time.realtimeSinceStartup + 10;
                while (!app.IsShopReady && Time.realtimeSinceStartup < readyDeadline) yield return null;
                Assert.That(app.IsShopReady, Is.True, "Addressables panel and Show tween completed");
                var shopPanel = Object.FindFirstObjectByType<Dreamy.Feature.Shop.Integration.ShopPanel>();
                foreach (var group in shopPanel.GetComponentsInParent<CanvasGroup>())
                    Assert.That(group.alpha, Is.GreaterThan(.99f), "Shown Shop ancestors are visible");
            }
            if (app.ScreenState == AppScreen.Settings)
            {
                float deadline = Time.realtimeSinceStartup + 10;
                while (app.ScreenState == AppScreen.Settings && !app.IsSettingsReady && Time.realtimeSinceStartup < deadline) yield return null;
                if (app.ScreenState == AppScreen.Settings) Assert.That(app.IsSettingsReady, Is.True, "Authored Settings Show/Hide completed");
            }
            float gameplayDeadline = Time.realtimeSinceStartup + 10;
            while (app.ScreenState == AppScreen.Playing && !app.View.Gameplay.IsReady && Time.realtimeSinceStartup < gameplayDeadline) yield return null;
            if (app.ScreenState == AppScreen.Playing) Assert.That(app.View.Gameplay.IsReady, Is.True, "Authored gameplay HUD Show completed");
            if (!invoked)
            {
                var system = EventSystem.current;
                var hits = new List<RaycastResult>();
                system.RaycastAll(new PointerEventData(system) { position = point }, hits);
                var module = system.GetComponent<InputSystemUIInputModule>();
                Debug.Log("[PixelFlowReviewInput] button=" + button.name + " screen=" + Screen.width + "x" + Screen.height + " frame=" + Time.frameCount +
                    " requested=" + point + " mouse=" + pointer.position.ReadValue() + " focused=" + system.isFocused +
                    " module=" + system.currentInputModule + " pointEnabled=" + module.point.action.enabled +
                    " clickEnabled=" + module.leftClick.action.enabled + " actionPoint=" + module.point.action.ReadValue<Vector2>() +
                    " hits=" + string.Join(",", hits.ConvertAll(hit => hit.gameObject.name)) +
                    " app=" + app.ScreenState + " buttonActive=" + button.gameObject.activeInHierarchy +
                    " pointerAdded=" + pointer.added + " pointerEnabled=" + pointer.enabled +
                    " settings=" + InputSystem.settings.name + " mode=" + InputSystem.settings.updateMode +
                    " apps=" + Object.FindObjectsByType<PixelFlowApp>(FindObjectsSortMode.None).Length +
                    " depth=" + button.image.depth + " culled=" + button.image.canvasRenderer.cull);
            }
        }

        private IEnumerator ClickPig(int id)
        {
            Physics.SyncTransforms();
            yield return Click(app.WorldCamera.WorldToScreenPoint(app.Board.PigPosition(id) + Vector3.up * .55f));
        }

        private IEnumerator ClickColor()
        {
            yield return null;
            Physics.SyncTransforms();
            var colors = app.Session.VisibleColors();
            var pixel = app.Session.Level.Pixels.First(p => app.Session.PixelHealthAt(p.X, p.Y) > 0 && colors.Contains(app.Session.TargetColorAt(p.X, p.Y)));
            yield return Click(app.WorldCamera.WorldToScreenPoint(app.Board.PixelPosition(pixel.X, pixel.Y)));
        }

        private IEnumerator FinishOnboarding()
        {
            int safety = 0;
            while (app.ScreenState == AppScreen.Tutorial && safety++ < 45)
                yield return Click(app.View.ConfirmButton.gameObject.activeInHierarchy ? app.View.ConfirmButton : app.View.PageNextButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing));
        }

        private IEnumerator WaitShopResult()
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while ((app.Shop.Busy || app.View.HomeModalPrimary == null) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(app.Shop.Busy, Is.False); Assert.That(app.View.HomeModalPrimary, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredSettingsSaveFailureRetryAndAttemptRecovery()
        {
            yield return new EnterPlayMode(); yield return Ready();
            var original = app.Progress;
            var fixture = new PixelFlow.Tests.ProgressFlowTests.SaveFixture { Value = original.Data };
            var failing = new ProgressStore(fixture, app.Campaign.LevelCount, app.Campaign.Economy, app.Campaign.Home);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(PixelFlowApp).GetField("progress", flags).SetValue(app, failing);
            typeof(PixelFlowInstallerProfile).GetField("<Progress>k__BackingField", flags).SetValue(Dreamy.Core.ServiceLocator.Get<PixelFlowInstallerProfile>(), failing);
            yield return Click(app.View.SettingsButton); var panel = app.View.Settings;
            bool haptic = failing.Data.HapticsEnabled; fixture.Fail = true;
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, panel.HapticsToggle.transform.position));
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Error)); Assert.That(failing.Data.HapticsEnabled, Is.EqualTo(haptic));
            Assert.That(panel.HapticsToggle.isOn, Is.EqualTo(haptic), "Failed preference is rendered back from the committed owner");
            fixture.Fail = false; yield return Click(app.View.ConfirmButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Settings)); Assert.That(failing.Data.HapticsEnabled, Is.EqualTo(!haptic));
            yield return Click(panel.CloseButton);
            failing.Data.Coins = 2000; failing.Data.Lives.Hearts = 1; failing.Data.Lives.NextHeartUtc = ProgressStore.UtcSeconds + 1800; failing.Save();
            yield return Click(app.View.HomeHeartsButton); fixture.Fail = true;
            yield return Click(app.View.HomeModalPrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Error)); Assert.That(failing.Data.Coins, Is.EqualTo(2000)); Assert.That(failing.Data.Lives.Hearts, Is.EqualTo(1));
            fixture.Fail = false; yield return Click(app.View.ConfirmButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.HomePopup)); Assert.That(failing.Data.Lives.Hearts, Is.EqualTo(5));
            Assert.That(failing.Data.Coins, Is.EqualTo(2000 - app.Campaign.Home.HeartRefillCoins));
            yield return Click(app.View.HomeModalClose);
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            yield return Click(app.View.PauseButton);
            panel = app.View.Settings; string run = app.Session.CaptureState().RunId; int hearts = failing.Data.Lives.Hearts;
            fixture.Fail = true;
            yield return Click(panel.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.Restart)); yield return Click(panel.NoticePrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Error)); Assert.That(failing.Data.ActiveRun.RunId, Is.EqualTo(run));
            Assert.That(failing.Data.Lives.Hearts, Is.EqualTo(hearts));
            fixture.Fail = false; yield return Click(app.View.ConfirmButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing)); Assert.That(app.Session.CaptureState().RunId, Is.Not.EqualTo(run));
            Assert.That(failing.Data.Lives.Hearts, Is.EqualTo(hearts - 1));
            checks.Add("Explicit failing save fixture: preference rollback/Retry, atomic Lives refill recovery and Restart retry completes exactly one life spend and one new attempt");
            WriteEvidence("settings-save-recovery", "Injected save fixture in an isolated Editor session; no owner data changed.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AuthoredSettingsPreferencesContextAndAttemptConfirmation()
        {
            yield return new EnterPlayMode(); yield return Ready();
            yield return Click(app.View.SettingsButton);
            var panel = app.View.Settings;
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponentsInChildren<Dreamy.UI.SafeArea>(true).Length, Is.EqualTo(1));
            Assert.That(panel.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.Restart).gameObject.activeInHierarchy, Is.False);
            bool haptic = app.Progress.Data.HapticsEnabled, notification = app.Progress.Data.NotificationsEnabled, smooth = app.Progress.Data.SmoothMotion;
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, panel.MusicSlider.transform.position));
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, panel.SoundSlider.transform.position));
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, panel.HapticsToggle.transform.position));
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, panel.NotificationsToggle.transform.position));
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, panel.SmoothMotionToggle.transform.position));
            Assert.That(app.Progress.Data.MusicVolume, Is.EqualTo(.5f).Within(.05f));
            Assert.That(app.Progress.Data.SoundVolume, Is.EqualTo(.5f).Within(.05f));
            Assert.That(app.Progress.Data.HapticsEnabled, Is.EqualTo(!haptic));
            Assert.That(app.Progress.Data.NotificationsEnabled, Is.EqualTo(!notification));
            Assert.That(app.Progress.Data.SmoothMotion, Is.EqualTo(!smooth));
            foreach (var command in new[] { Dreamy.Feature.Settings.Integration.SettingsCommand.Privacy, Dreamy.Feature.Settings.Integration.SettingsCommand.SaveProgress, Dreamy.Feature.Settings.Integration.SettingsCommand.Feedback })
            {
                yield return Click(panel.ButtonFor(command)); Assert.That(panel.IsNoticeVisible, Is.True);
                yield return Click(panel.NoticeClose); Assert.That(panel.IsNoticeVisible, Is.False);
            }
            Capture("settings-authored-home", 1080, 1920);
            Capture("settings-authored-long", 1080, 2340);
            Capture("settings-authored-small", 720, 1280);
            yield return Click(panel.CloseButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            yield return Click(app.View.SettingsButton); panel = app.View.Settings;
            Assert.That(panel.HapticsToggle.isOn, Is.EqualTo(!haptic));
            var saved = new DatasaveService(new DatasaveOptions { DirectoryName = app.SaveDirectory }).Load<PixelFlowProgress>(PixelFlowProgress.Key);
            Assert.That(saved.NotificationsEnabled, Is.EqualTo(!notification)); Assert.That(saved.SmoothMotion, Is.EqualTo(!smooth));
            yield return Click(panel.CloseButton);
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            yield return Click(app.View.PauseButton);
            panel = app.View.Settings; var session = app.Session; string run = session.CaptureState().RunId; int hearts = app.Progress.Data.Lives.Hearts;
            Assert.That(panel.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.Restart).gameObject.activeInHierarchy, Is.True);
            Capture("settings-authored-level", 1080, 1920);
            yield return Click(panel.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.Restart));
            Assert.That(panel.IsNoticeVisible, Is.True); yield return Click(panel.NoticeClose);
            Assert.That(app.Session, Is.SameAs(session)); Assert.That(app.Progress.Data.Lives.Hearts, Is.EqualTo(hearts));
            yield return Click(panel.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.Restart)); yield return Click(panel.NoticePrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing));
            Assert.That(app.Session.CaptureState().RunId, Is.Not.EqualTo(run)); Assert.That(app.Progress.Data.Lives.Hearts, Is.EqualTo(hearts - 1));
            yield return Click(app.View.PauseButton);
            panel = app.View.Settings;
            yield return Click(panel.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.GiveUp)); yield return Click(panel.NoticePrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home)); Assert.That(app.Progress.Data.ActiveRun, Is.Null);
            Assert.That(app.Progress.Data.Lives.Hearts, Is.EqualTo(hearts - 2));
            checks.Add("Authored sample Settings: separate audio commits, three persistent toggles, unavailable source actions, cached reopen, context-only restart/give-up confirmation and single life spend");
            WriteEvidence("authored-settings", "Isolated save, authored sample prefab and source art. Account/privacy/support providers are unavailable; no native screenshot or device parity claim."); RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AuthoredLivesFullEmptyUnlimitedAndResponsive()
        {
            yield return new EnterPlayMode(); yield return Ready();
            yield return Click(app.View.HomeHeartsButton);
            var panel = Dreamy.UI.PanelManager.Instance.Get<LivesPanel>();
            Assert.That(panel.GetComponentsInChildren<Dreamy.UI.SafeArea>(true).Length, Is.EqualTo(1));
            Assert.That(panel.RefillButton.interactable, Is.False); Assert.That(panel.CoinsButton.gameObject.activeInHierarchy, Is.False);
            CaptureAspects("lives-authored-full"); yield return Click(panel.CloseButton);
            // Explicit isolated life/coin/time fixtures; subsequent interaction is real pointer input.
            app.Progress.Data.Coins = 0; app.Progress.Data.Lives.Hearts = 0; app.Progress.Data.Lives.NextHeartUtc = ProgressStore.UtcSeconds + 1800; app.Progress.Save();
            yield return Click(app.View.HomeHeartsButton); panel = Dreamy.UI.PanelManager.Instance.Get<LivesPanel>();
            Assert.That(panel.RefillButton.interactable, Is.False); Assert.That(panel.CoinsButton.gameObject.activeInHierarchy, Is.True);
            Capture("lives-authored-empty", 1080, 1920); yield return Click(panel.CloseButton);
            app.Progress.Data.Lives.UnlimitedUntilUtc = ProgressStore.UtcSeconds + 7200; app.Progress.Save();
            yield return Click(app.View.HomeHeartsButton); panel = Dreamy.UI.PanelManager.Instance.Get<LivesPanel>();
            Assert.That(panel.RefillButton.interactable, Is.False); Assert.That(panel.CoinsButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(panel.GetComponentsInChildren<TMPro.TMP_Text>().Any(text => text.text == "Unlimited lives"), Is.True);
            Assert.That(panel.GetComponentsInChildren<TMPro.TMP_Text>().Any(text => text.text.Contains(":")), Is.True);
            Capture("lives-authored-unlimited", 1080, 1920); yield return Click(panel.CloseButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            checks.Add("Authored BaseFeaturePanel Lives variant: full/empty/unlimited, guarded refill, countdown and three layout preview aspects with labeled isolated fixtures");
            WriteEvidence("authored-lives", "Original imported heart/popup/button/font assets; native refill screenshot/config values unavailable, no device or ad provider claim.");
            RestoreInput(); yield return new ExitPlayMode();
        }
        private IEnumerator DragShop()
        {
            var rect = app.View.ShopScroll.viewport;
            var center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            var start = center + new Vector2(230, -160); var end = center + new Vector2(230, 240);
            InputSystem.QueueStateEvent(pointer, new MouseState { position = start }.WithButton(MouseButton.Left)); InputSystem.Update(); EventSystem.current.currentInputModule.Process(); yield return null;
            for (int i = 1; i <= 8; i++)
            {
                InputSystem.QueueStateEvent(pointer, new MouseState { position = Vector2.Lerp(start, end, i / 8f) }.WithButton(MouseButton.Left));
                InputSystem.Update(); EventSystem.current.currentInputModule.Process(); yield return null;
            }
            InputSystem.QueueStateEvent(pointer, new MouseState { position = end }); InputSystem.Update(); EventSystem.current.currentInputModule.Process(); yield return null;
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator SourceShopPurchasesCancelFailurePendingAndRepeat()
        {
            yield return new EnterPlayMode(); yield return Ready();
            yield return Click(app.View.ShopButton); Assert.That(app.View.ShopOfferButtons.Count, Is.EqualTo(6));
            Assert.That(app.View.HomeRankButton.image.sprite.name, Is.EqualTo("Lock"));
            yield return Click(app.View.HomeRankButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.HomePopup));
            yield return Click(app.View.HomeModalClose); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            CaptureAspects("shop-early"); int coins = app.Progress.Data.Coins;
            var gateway = (FakeShopGateway)app.Shop.Gateway;
            gateway.NextOutcome = FakeShopOutcome.Cancelled;
            yield return Click(app.View.ShopOfferButtons["gold-1000"]); yield return WaitShopResult();
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins)); Capture("shop-purchase-cancel", 720, 1280); yield return Click(app.View.HomeModalPrimary);
            gateway.NextOutcome = FakeShopOutcome.Failed;
            yield return Click(app.View.ShopOfferButtons["gold-1000"]); yield return WaitShopResult();
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins)); Capture("shop-purchase-failed", 720, 1280);
            yield return Click(app.View.HomeModalPrimary); yield return WaitShopResult();
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + 1000)); Capture("shop-purchase-success", 720, 1280);
            yield return Click(app.View.HomeModalPrimary);
            gateway.NextOutcome = FakeShopOutcome.Deferred;
            yield return Click(app.View.ShopOfferButtons["gold-1000"]);
            float deadline = Time.realtimeSinceStartup + 10;
            while (!gateway.HasDeferred && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(gateway.HasDeferred, Is.True); Assert.That(app.Shop.Busy, Is.True);
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + 1000)); Capture("shop-purchase-pending", 720, 1280);
            gateway.ResolveDeferred(true); yield return WaitShopResult();
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + 2000)); Assert.That(app.Progress.Data.ShopTransactions.Count, Is.EqualTo(2));
            yield return Click(app.View.HomeModalPrimary); yield return Click(app.View.HomeStartButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            checks.Add("Pointer Shop/gold purchase -> fake gateway cancel/failure/Retry/success/repeat/deferred; approval fixture is explicit; receipts grant exactly once");
            WriteEvidence("shop-gold-transactions", "Owner-authorized fake gateway, no real billing. Pending approval is a labeled provider fixture, not a real SDK callback.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator ShopAuthoredProductsPurchaseCorrectRewards()
        {
            yield return new EnterPlayMode(); yield return Ready();
            // Explicit review fixture; native unlock tuning is still unverified.
            app.Progress.Data.UnlockedLevel = 35; app.Progress.Save();
            typeof(PixelFlowApp).GetMethod("SelectLevel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, new object[] { 35 });
            yield return Click(app.View.ShopButton);
            Assert.That(app.View.ShopOfferButtons.Count, Is.EqualTo(11));
            Assert.That(app.View.ShopScroll.content.GetComponentsInChildren<Button>(true).Any(button => button.name.Contains("Restore")), Is.False);
            Capture("shop-complete-top", 1080, 1920);
            var ids = new[] { "gold-1000", "gold-5000", "gold-10000", "gold-25000", "gold-50000", "gold-100000", "small-bundle", "medium-bundle", "ultra-bundle", "no-ads" };
            foreach (string id in ids)
            {
                var offer = app.Shop.Catalog.Find(id);
                var button = app.View.ShopOfferButtons[id];
                var item = (RectTransform)button.GetComponentInParent<Dreamy.Feature.Shop.Integration.ShopOfferItem>().transform;
                var scroll = app.View.ShopScroll;
                scroll.StopMovement();
                scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(-item.anchoredPosition.y - 12, 0, Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height)));
                Canvas.ForceUpdateCanvases(); yield return null;
                if (offer.Section is "Bundle" or "NoAds") Capture("shop-offer-" + id, 1080, 1920);
                int coins = app.Progress.Data.Coins;
                int[] inventory = (int[])app.Progress.Data.Boosters.Clone();
                long started = ProgressStore.UtcSeconds;
                long previousUnlimited = app.Progress.Data.Lives.UnlimitedUntilUtc;
                yield return Click(button); yield return WaitShopResult();
                Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + offer.Coins), id);
                for (int i = 0; i < 4; i++) Assert.That(app.Progress.Data.Boosters[i], Is.EqualTo(inventory[i] + offer.Boosters), id);
                if (offer.UnlimitedSeconds > 0)
                    Assert.That(app.Progress.Data.Lives.UnlimitedUntilUtc, Is.GreaterThanOrEqualTo(Math.Max(started, previousUnlimited) + offer.UnlimitedSeconds), id);
                Assert.That(app.Progress.Data.NoAds, Is.EqualTo(offer.NoAds), id);
                yield return Click(app.View.HomeModalPrimary);
            }
            Assert.That(app.Progress.Data.ShopTransactions.Count, Is.EqualTo(ids.Length));
            Assert.That(app.View.ShopOfferButtons.ContainsKey("no-ads"), Is.False);
            Assert.That(app.View.ShopOfferButtons.ContainsKey("no-ads-bundle"), Is.False);
            yield return Click(app.View.HomeStartButton);
            checks.Add("Pointer purchases all six Gold packs, Small/Medium/Ultra bundles and standalone No Ads from authored cards; correct coin/four-booster/timer/ownership deltas and distinct receipts. No Ads Bundle is exercised by the separate bundle/restore route.");
            WriteEvidence("shop-every-product", "Isolated level36 fixture and owner-authorized fake gateway; no real payment or measured native unlock claim.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator SourceShopBundlesScrollOwnedNoAdsAndRestore()
        {
            yield return new EnterPlayMode(); yield return Ready();
            app.Progress.Data.UnlockedLevel = 35; app.Progress.Save();
            typeof(PixelFlowApp).GetMethod("SelectLevel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, new object[] { 35 });
            yield return Click(app.View.ShopButton); Assert.That(app.View.ShopOfferButtons.Count, Is.EqualTo(11));
            Assert.That(app.View.ShopScroll.content.GetComponentsInChildren<Button>(true).Any(button => button.name.Contains("Restore")), Is.False, "Restore is not a Shop control");
            CaptureAspects("shop-full-top"); yield return DragShop(); yield return DragShop();
            Assert.That(app.View.ShopScroll.content.anchoredPosition.y, Is.GreaterThan(300)); Capture("shop-pointer-scrolled", 720, 1280);
            // Deterministic alignment after proving pointer scroll; not presented
            // as a pointer search through the full feed.
            app.View.ScrollShopTo("NoAdsBundle"); yield return null; Capture("shop-noads-bundle", 720, 1280);
            int coins = app.Progress.Data.Coins, boosters = app.Progress.Amount(BoosterKind.Tray);
            yield return Click(app.View.ShopOfferButtons["no-ads-bundle"]); yield return WaitShopResult();
            Assert.That(app.Progress.Data.NoAds, Is.True); Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + 1000));
            Assert.That(app.Progress.Amount(BoosterKind.Tray), Is.EqualTo(boosters + 1));
            long unlimited = app.Progress.Data.Lives.UnlimitedUntilUtc;
            Assert.That(unlimited, Is.GreaterThan(ProgressStore.UtcSeconds + 3500));
            yield return Click(app.View.HomeModalPrimary);
            Assert.That(app.View.ShopOfferButtons.ContainsKey("no-ads"), Is.False); Assert.That(app.View.ShopOfferButtons.ContainsKey("no-ads-bundle"), Is.False);
            app.View.ScrollShopTo("Bundle"); yield return null; CaptureAspects("shop-bundles-owned");
            yield return Click(app.View.SettingsButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Settings));
            var gateway = (FakeShopGateway)app.Shop.Gateway; gateway.FailRestore = true;
            var restore = Array.Find(Object.FindObjectsByType<Button>(FindObjectsSortMode.None), button => button.name == "Restore");
            yield return Click(restore); yield return WaitShopResult(); Capture("shop-restore-error", 720, 1280);
            gateway.FailRestore = false; yield return Click(app.View.HomeModalPrimary); yield return WaitShopResult();
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + 1000)); Assert.That(app.Progress.Data.Lives.UnlimitedUntilUtc, Is.EqualTo(unlimited));
            Capture("shop-restored", 720, 1280); yield return Click(app.View.HomeModalPrimary);
            yield return Click(app.View.HomeModalClose); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            yield return Click(app.View.HomeStartButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            checks.Add("Explicit isolated level36 fixture -> Shop11offers without Restore -> pointer scroll -> bundle purchase/owned hiding -> existing Settings restore failure/Retry without replaying rewards");
            WriteEvidence("shop-bundles-restore", "Owner-authorized fake gateway; section alignment/progression are labeled fixtures. No original SDK/remote unlock equivalence is claimed.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator MiniShopFromLivesAndEmptyBoosterReturnsToOrigin()
        {
            yield return new EnterPlayMode(); yield return Ready();
            app.Progress.Data.Coins = 0; app.Progress.Data.Lives.Hearts = 0; app.Progress.Data.Lives.NextHeartUtc = ProgressStore.UtcSeconds + 1800; app.Progress.Save();
            yield return Click(app.View.HomeHeartsButton);
            Button getCoins = Array.Find(Object.FindObjectsByType<Button>(FindObjectsSortMode.None), b => b.name == "Get Coins");
            yield return Click(getCoins); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop)); Assert.That(app.View.ShopCloseButton.gameObject.activeInHierarchy, Is.True);
            Capture("mini-shop-from-lives", 720, 1280);
            yield return Click(app.View.ShopOfferButtons["gold-1000"]); yield return WaitShopResult(); yield return Click(app.View.HomeModalPrimary);
            yield return Click(app.View.ShopCloseButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.HomePopup));
            yield return Click(app.View.HomeModalPrimary); Assert.That(app.Progress.Data.Lives.Hearts, Is.EqualTo(5)); Assert.That(app.Progress.Data.Coins, Is.EqualTo(100));
            yield return Click(app.View.HomeModalClose);
            app.Progress.Data.Coins = 0; app.Progress.Data.Boosters[0] = 0; app.Progress.Save();
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            yield return Click(app.View.BoosterButton(BoosterKind.Tray)); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.BoosterPurchase));
            yield return Click(app.View.HomeModalPrimary); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            yield return Click(app.View.ShopOfferButtons["gold-1000"]); yield return WaitShopResult(); yield return Click(app.View.HomeModalPrimary);
            yield return Click(app.View.ShopCloseButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.BoosterPurchase));
            yield return Click(app.View.HomeModalPrimary); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing));
            Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(6)); Assert.That(app.Progress.Amount(BoosterKind.Tray), Is.Zero);
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(1000 - app.Progress.Price(BoosterKind.Tray)));
            checks.Add("Explicit empty wallet/lives/inventory fixture -> pointer Mini Shop -> fake gold purchase -> original lives refill or booster purchase/use, with correct return context");
            WriteEvidence("shop-mini-return", "Resource exhaustion is fixture setup; all subsequent routes/purchases/apply/return use real UI pointers and fake gateway.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator SourceHomeHeaderTabsLivesAndLockedRank()
        {
            yield return new EnterPlayMode(); yield return Ready();
            Assert.That(app.View.HomeNodes.Count, Is.EqualTo(10));
            Assert.That(app.View.PlayButton.image.sprite.name, Is.EqualTo("Play"));
            yield return Click(app.View.HomeHeartsButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.HomePopup));
            Assert.That(app.View.HomeModalPrimary.interactable, Is.False, "Full lives cannot charge refill coins");
            Capture("home-full-lives", 720, 1280); yield return Click(app.View.HomeModalClose);
            yield return Click(app.View.HomeRankButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.HomePopup));
            Assert.That(app.View.HomeModalPrimary.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("Keep playing"));
            Capture("home-rank-locked", 720, 1280); yield return Click(app.View.HomeModalPrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            yield return Click(app.View.HomeCoinsButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            yield return Click(app.View.SettingsButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Settings));
            yield return Click(app.View.HomeModalClose); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            yield return Click(app.View.HomeStartButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            // Explicit isolated fixture: unlock docks/rank and seed refill funds.
            app.Progress.Data.UnlockedLevel = 100; app.Progress.Data.Coins = 2000;
            app.Progress.Data.Lives.Hearts = 1; app.Progress.Data.Lives.NextHeartUtc = ProgressStore.UtcSeconds + 1800;
            app.Progress.Save();
            typeof(PixelFlowApp).GetMethod("SelectLevel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, new object[] { 100 });
            yield return Click(app.View.HomeHeartsButton); yield return Click(app.View.HomeModalPrimary);
            Assert.That(app.Progress.Data.Lives.Hearts, Is.EqualTo(5)); Assert.That(app.Progress.Data.Coins, Is.EqualTo(1100));
            Assert.That(app.View.HomeModalPrimary.interactable, Is.False);
            Capture("home-refilled-lives", 720, 1280); yield return Click(app.View.HomeModalClose);
            yield return Click(app.View.HomeRankButton); int coins = app.Progress.Data.Coins;
            Capture("home-rank-unavailable", 720, 1280); yield return Click(app.View.HomeModalPrimary);
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins)); yield return Click(app.View.HomeModalClose);
            Button noAds = Array.Find(Object.FindObjectsByType<Button>(FindObjectsSortMode.None), b => b.name == "Home NoAds");
            yield return Click(noAds); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            Assert.That(app.View.ShopOfferButtons.ContainsKey("no-ads"), Is.True); yield return Click(app.View.HomeStartButton);
            foreach (string dock in new[] { "Pass", "FireQuest", "Arena" })
            {
                Button button = Array.Find(Object.FindObjectsByType<Button>(FindObjectsSortMode.None), b => b.name == "Home " + dock);
                yield return Click(button); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.HomePopup));
                yield return Click(app.View.HomeModalPrimary); Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins));
                yield return Click(app.View.HomeModalClose);
            }
            checks.Add("Original Home assets; heart/full refill disabled; locked rank; coin -> Shop -> Settings -> Shop -> Start through pointer controls");
            WriteEvidence("source-home", "Fresh isolated save. Remote rank/payment parity is not claimed.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator CaptureRepresentativeStates()
        {
            yield return new EnterPlayMode();
            yield return Ready();
            CaptureAspects("home");
            yield return Click(app.View.PlayButton);
            yield return FinishOnboarding();
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing));
            yield return null;
            CaptureAspects("level-1");
            yield return Click(app.View.PauseButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Settings));
            Capture("pause", 720, 1280);
            WriteEvidence("captures", "Scene Home/Play/Pause reached through queued pointer input; offscreen layout previews at three portrait aspects.");
            RestoreInput();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AuthoredGameplayHudGearWalletAndFourBoosters()
        {
            yield return new EnterPlayMode(); yield return Ready();
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            var hud = app.View.Gameplay;
            Assert.That(hud.Boosters.Count, Is.EqualTo(4));
            Assert.That(hud.GetComponentsInChildren<Dreamy.UI.SafeArea>(true).Length, Is.EqualTo(1));
            Assert.That(hud.LevelLabel.text, Is.EqualTo("Level 1"));
            Assert.That(hud.BeltLabel.text, Is.EqualTo("5/5"));
            Assert.That(hud.Boosters.All(slot => slot.Icon.type == Image.Type.Simple && slot.Icon.preserveAspect), Is.True);
            string run = app.Session.RunId; int hearts = app.Progress.Data.Lives.Hearts;
            CaptureAspects("gameplay-authored");
            yield return Click(hud.SettingsButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Settings)); Assert.That(app.Session.RunId, Is.EqualTo(run));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(app.Session.Launches, Is.Zero); yield return Click(app.View.Settings.CloseButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing)); Assert.That(app.Progress.Data.Lives.Hearts, Is.EqualTo(hearts));
            yield return Click(hud.CoinsButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            yield return Click(app.View.ShopCloseButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing));
            Assert.That(app.Session.RunId, Is.EqualTo(run));
            int amount = app.Progress.Amount(BoosterKind.Tray);
            yield return Click(hud.Boosters[0].Button);
            Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(6)); Assert.That(app.Progress.Amount(BoosterKind.Tray), Is.EqualTo(amount - 1));
            Assert.That(hud.BeltLabel.text, Is.EqualTo("6/6"));
            checks.Add("Original-art authored HUD: four booster slots, available conveyor capacity, gear opens Settings directly, close preserves attempt/lives, wallet Mini Shop returns to same run and Tray updates actual capacity/stock");
            WriteEvidence("gameplay-authored", "Original source hierarchy/art and runtime screenshot composition; current tuning/unlocks and special Unicorn UI remain explicit target-version gaps.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AuthoredBoosterPurchaseFailureRetryCompletesEffectOnce()
        {
            yield return new EnterPlayMode(); yield return Ready();
            var fixture = new PixelFlow.Tests.ProgressFlowTests.SaveFixture { Value = app.Progress.Data };
            var progress = new ProgressStore(fixture, app.Campaign.LevelCount, app.Campaign.Economy, app.Campaign.Home);
            progress.Data.Coins = 2000; progress.Data.Boosters[0] = 0; progress.Save();
            typeof(PixelFlowApp).GetField("progress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(app, progress);
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            yield return Click(app.View.BoosterButton(BoosterKind.Tray));
            Assert.That(app.View.Gameplay.IsOfferVisible, Is.True); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.BoosterPurchase));
            Capture("booster-authored-purchase", 1080, 1920); fixture.Fail = true;
            yield return Click(app.View.HomeModalPrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Error)); Assert.That(progress.Data.Coins, Is.EqualTo(2000));
            Assert.That(progress.Amount(BoosterKind.Tray), Is.Zero); Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(5));
            fixture.Fail = false; yield return Click(app.View.ConfirmButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing)); Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(6));
            Assert.That(progress.Data.Coins, Is.EqualTo(2000 - progress.Price(BoosterKind.Tray))); Assert.That(progress.Amount(BoosterKind.Tray), Is.Zero);
            Assert.That(app.View.Gameplay.IsOfferVisible, Is.False);
            checks.Add("Explicit isolated save failure: authored zero-stock booster offer -> failed buy preserves currency/stock/model -> Retry buys and applies once");
            WriteEvidence("booster-purchase-recovery", "Virtual currency purchase with an injected failing save fixture; existing ProgressStore is the only transaction owner.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator AuthoredResultContinueRetryAndWinCommitOnce()
        {
            yield return new EnterPlayMode(); yield return Ready();
            var fixture = new PixelFlow.Tests.ProgressFlowTests.SaveFixture { Value = app.Progress.Data };
            var progress = new ProgressStore(fixture, app.Campaign.LevelCount, app.Campaign.Economy, app.Campaign.Home);
            typeof(PixelFlowApp).GetField("progress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(app, progress);
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            string run = app.Session.RunId;
            // A timer fixture forces the terminal state; native timing is not being measured.
            app.Session.Level.TimeLimit = .01f;
            float deadline = Time.realtimeSinceStartup + 10;
            while (app.ScreenState == AppScreen.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            app.Session.Level.TimeLimit = 0;
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Lost));
            Assert.That(app.View.Gameplay.IsResultVisible, Is.True);
            Assert.That(GameObject.Find("Pause and Result"), Is.Null);
            Assert.That(app.View.Gameplay.RewardedButton.interactable, Is.False);
            int coins = progress.Data.Coins; fixture.Fail = true;
            yield return Click(app.View.ContinueButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Error)); Assert.That(app.Session.Revives, Is.Zero);
            Assert.That(progress.Data.Coins, Is.EqualTo(coins)); Assert.That(app.Session.RunId, Is.EqualTo(run));
            fixture.Fail = false; yield return Click(app.View.ConfirmButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing)); Assert.That(app.Session.Revives, Is.EqualTo(1));
            Assert.That(progress.Data.Coins, Is.EqualTo(coins - app.Campaign.Economy.ContinueCost));
            app.View.ContinueButton.onClick.Invoke(); Assert.That(app.Session.Revives, Is.EqualTo(1), "Closed result has no stale continuation");
            // Deterministic rule fixture clears source level; result/save/navigation remain real.
            int safety = 0;
            while (app.Session.Status == SessionStatus.Playing && safety++ < 1000)
                foreach (int color in app.Session.VisibleColors()) app.Session.ClearVisibleColor(color);
            deadline = Time.realtimeSinceStartup + 10;
            while (app.ScreenState == AppScreen.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Won));
            Assert.That(app.View.Gameplay.WinReward.text, Is.EqualTo(app.Campaign.Economy.FirstClearCoins.ToString("N0")));
            coins = progress.Data.Coins;
            typeof(PixelFlowApp).GetMethod("RestoreScreen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, null);
            Assert.That(progress.Data.Coins, Is.EqualTo(coins), "Reopening a committed result grants nothing");
            CaptureAspects("result-win-authored"); yield return Click(app.View.NextButton);
            Assert.That(app.SelectedLevel, Is.EqualTo(1)); Assert.That(app.Session.RunId, Is.Not.EqualTo(run));
            checks.Add("Authored terminal UI: failed coin continue rolls back; Retry consumes once; stale callback inert; committed win reopen grants nothing; next creates new run");
            WriteEvidence("result-transaction-recovery", "Timer and clear-state setup are deterministic fixtures; pointer navigation and persistence are real client routes, no player or rewarded provider.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator AuthoredResultMiniShopReturnsToFailedAttemptAndPeekRestores()
        {
            yield return new EnterPlayMode(); yield return Ready();
            app.Progress.Data.Coins = 0; app.Progress.Save();
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            app.Session.Level.TimeLimit = .01f;
            float deadline = Time.realtimeSinceStartup + 10;
            while (app.ScreenState == AppScreen.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            app.Session.Level.TimeLimit = 0;
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Lost)); string run = app.Session.RunId;
            var panel = app.View.Gameplay;
            var hold = panel.HoldPreview;
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            hold.OnPointerDown(data);
            Assert.That(panel.GetComponentsInChildren<CanvasGroup>(true).Any(g => g.name == "Fail Result" && g.alpha == 0), Is.True);
            hold.OnPointerUp(data);
            Assert.That(panel.GetComponentsInChildren<CanvasGroup>(true).Any(g => g.name == "Fail Result" && g.alpha == 1), Is.True);
            CaptureAspects("result-fail-authored");
            yield return Click(app.View.ContinueButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            yield return Click(app.View.ShopCloseButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Lost));
            Assert.That(app.Session.RunId, Is.EqualTo(run)); Assert.That(app.Session.Revives, Is.Zero);
            yield return Click(app.View.ContinueButton);
            yield return Click(app.View.ShopOfferButtons["gold-1000"]); yield return WaitShopResult(); yield return Click(app.View.HomeModalPrimary);
            yield return Click(app.View.ShopCloseButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Lost));
            yield return Click(app.View.ContinueButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing)); Assert.That(app.Session.RunId, Is.EqualTo(run));
            Assert.That(app.Session.Revives, Is.EqualTo(1)); Assert.That(app.Progress.Data.Coins, Is.EqualTo(1000 - app.Campaign.Economy.ContinueCost));
            // Closing a committed loss must discard that attempt without charging a second life.
            app.Session.Level.TimeLimit = .01f; deadline = Time.realtimeSinceStartup + 10;
            while (app.ScreenState == AppScreen.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            app.Session.Level.TimeLimit = 0;
            int hearts = app.Progress.Data.Lives.Hearts;
            yield return Click(app.View.Gameplay.ResultCloseButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home)); Assert.That(app.Progress.Data.ActiveRun, Is.Null);
            Assert.That(app.Progress.Data.Lives.Hearts, Is.EqualTo(hearts));
            yield return Click(app.View.PlayButton); Assert.That(app.Session.RunId, Is.Not.EqualTo(run));
            checks.Add("Hold preview down/up restores fail UI; failed-attempt Mini Shop close and fake purchase preserve run; explicit continue applies once");
            WriteEvidence("result-mini-shop-return", "Hold gesture uses EventSystem handlers directly; shop and continue use queued UI pointer. Timer/wallet are fixtures; billing gateway is fake.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator PointerInputResetAndProgressionRoute()
        {
            yield return new EnterPlayMode();
            yield return Ready();
            yield return Click(app.View.PlayButton);
            yield return FinishOnboarding();
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing));
            var first = app.Session;
            yield return ClickPig(first.QueueAt(0, 1).Id);
            Assert.That(first.Launches, Is.Zero, "Rear queue pig is unavailable");
            checks.Add("Pointer click on rear queue pig cannot launch it");

            yield return Click(app.View.PauseButton);
            int pixels = first.RemainingPixels;
            yield return ClickPig(first.QueueAt(0).Id);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Settings));
            Assert.That(first.Launches, Is.Zero, "Pause modal blocks world pointer clicks");
            Assert.That(first.RemainingPixels, Is.EqualTo(pixels));
            // The same screen coordinate can legitimately hit a Settings action
            // in front of the pig. Close its secondary notice before the slider.
            if (app.View.Settings.IsNoticeVisible) yield return Click(app.View.Settings.NoticeClose);
            float sound = app.Progress.Data.SoundVolume;
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, app.View.Settings.SoundSlider.transform.position));
            Assert.That(app.Progress.Data.SoundVolume, Is.LessThan(sound));
            yield return Click(app.View.Settings.CloseButton);
            checks.Add("Pause freezes puzzle, blocks world click-through and permits sound/resume controls");

            // This segment tests pointer/capacity rejection, not conveyor timing.
            // Freeze simulation so a pig cannot finish between input frames.
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            try
            {
            int selected = first.QueueAt(0).Id;
            yield return ClickPig(selected);
            Assert.That(first.Launches, Is.EqualTo(1));
            yield return ClickPig(selected);
            Assert.That(first.Launches, Is.EqualTo(1), "Clicking the same moving pig does not duplicate it");
            checks.Add("Repeated pointer selection of a moving pig cannot duplicate launch");
            for (int i = 0; i < 4; i++)
            {
                ShooterState candidate = null;
                for (int column = 0; column < first.ColumnCount && candidate == null; column++) candidate = first.QueueAt(column);
                Assert.That(candidate, Is.Not.Null);
                yield return ClickPig(candidate.Id);
            }
            Assert.That(first.Active.Count, Is.EqualTo(first.Level.ConveyorLimit));
            ShooterState remaining = null;
            for (int column = 0; column < first.ColumnCount && remaining == null; column++) remaining = first.QueueAt(column);
            Assert.That(remaining, Is.Not.Null);
            int launches = first.Launches, ammo = remaining.Ammo;
            yield return ClickPig(remaining.Id);
            Assert.That(first.Launches, Is.EqualTo(launches));
            Assert.That(remaining.Location, Is.EqualTo(ShooterLocation.Queue));
            Assert.That(remaining.Ammo, Is.EqualTo(ammo));
            checks.Add("Full-belt pointer rejection preserves queue membership and ammunition");
            // The acceptance segment is complete. Restore the clock before
            // opening authored UI transitions; gameplay itself pauses by state.
            Time.timeScale = previousTimeScale;
            yield return Click(app.View.PauseButton);
            Capture("full-belt-paused", 720, 1280);

            var oldPig = app.Board.Pig(selected);
            // Invoke the actual button event synchronously for this same-frame
            // lifecycle assertion; other route actions above/below use pointer input.
            yield return Click(app.View.RestartButton);
            Assert.That(oldPig.gameObject.activeInHierarchy, Is.True, "Restart confirmation preserves the current attempt");
            yield return Click(app.View.Settings.NoticePrimary);
            Assert.That(oldPig == null || !oldPig.gameObject.activeInHierarchy, Is.True, "Confirmed restart retires the previous board");
            Assert.That(app.Session, Is.Not.SameAs(first));
            Assert.That(app.Session.Launches, Is.Zero);
            Assert.That(app.Session.RemainingPixels, Is.EqualTo(180));
            yield return null;
            checks.Add("Restart confirmation keeps the old board until accepted, then retires it and resets the session; capacity segment uses a frozen simulation clock");
            }
            finally { Time.timeScale = previousTimeScale; }

            float deadline = Time.realtimeSinceStartup + 150;
            bool waiting = false;
            while (app.ScreenState == AppScreen.Playing && Time.realtimeSinceStartup < deadline)
            {
                foreach (var pig in app.Session.Waiting) if (pig != null) waiting = true;
                int candidate = PixelFlowSmoke.Choose(app.Session);
                if (candidate >= 0 && app.Session.Active.Count < app.Session.Level.ConveyorLimit)
                {
                    yield return new WaitForSecondsRealtime(.20f);
                    if (app.Session.CanSelect(candidate)) yield return ClickPig(candidate);
                }
                else yield return null;
            }
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Won), app.Session?.LossReason);
            Assert.That(waiting, Is.True, "Actual route includes a return to waiting");
            var reload = new DatasaveService(new DatasaveOptions { DirectoryName = app.SaveDirectory });
            var saved = reload.Load<PixelFlowProgress>(PixelFlowProgress.Key);
            Assert.That(saved.CompletedLevels, Does.Contain(0));
            Assert.That(saved.UnlockedLevel, Is.EqualTo(1));
            checks.Add("Pointer-driven source level1 wins, includes waiting return and persists completion/unlock");
            Capture("win", 720, 1280);
            yield return Click(app.View.NextButton);
            Assert.That(app.SelectedLevel, Is.EqualTo(1));
            Assert.That(app.Session.RemainingPixels, Is.EqualTo(400));
            yield return Click(app.View.PauseButton);
            yield return Click(app.View.RestartButton); yield return Click(app.View.Settings.NoticePrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing), "Pointer Retry actually leaves the pause modal");
            Assert.That(app.Session.Launches, Is.Zero);
            Assert.That(app.Session.RemainingPixels, Is.EqualTo(400));
            yield return Click(app.View.PauseButton);
            var audio = Dreamy.Core.ServiceLocator.Get<Dreamy.Audio.IAudioService>();
            Assert.That(Object.FindObjectsByType<Dreamy.Audio.AudioBootstrap>(FindObjectsSortMode.None), Is.Empty, "No parallel sample audio initializer");
            var key = new Dreamy.Audio.AudioKey("pixel-flow", "ui-tap");
            var probe = audio.Play(key);
            Assert.That(probe.Succeeded, Is.True, probe.Status + ": " + probe.Message);
            audio.Stop(probe.Handle);
            var loop = audio.PlayLoop(key);
            Assert.That(loop.IsValid, Is.True, "Package audio owns the deliberate ongoing session sound");
            yield return Click(app.View.MenuButton); yield return Click(app.View.Settings.NoticePrimary);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            Assert.That(audio.Stop(loop), Is.False, "Home already stopped the Sfx bus; music has its own owner");
            Capture("home-progress", 720, 1280);
            checks.Add("Next and pointer Retry rebuild source level2; Home clears session and ongoing audio");
            app.Progress.Data.Lives.UnlimitedUntilUtc = ProgressStore.UtcSeconds + 7200; app.Progress.Save();
            for (int i = 0; i < 3; i++)
            {
                yield return Click(app.View.PlayButton);
                Assert.That(app.Session.Launches, Is.Zero);
                yield return Click(app.View.PauseButton);
                yield return Click(app.View.MenuButton); yield return Click(app.View.Settings.NoticePrimary);
                Assert.That(Object.FindObjectsByType<PigView>(FindObjectsSortMode.None), Is.Empty);
            }
            checks.Add("Three repeated Home/Play cycles leave no active stale pig objects");
            WriteEvidence("route", "Actual Editor pointer gear/Settings/confirmed restart; isolated unlimited-life teardown fixture. No player build, device, listening, performance or original-game fidelity acceptance.");
            RestoreInput();
            yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator FullOfflineMenusBoostersResumeAndContinueRoute()
        {
            yield return new EnterPlayMode(); yield return Ready();
            // Explicit fixture unlock for the existing daily route; native unlock
            // tuning remains a source gap. This is not earned progression.
            app.Campaign.Home.DailyLevel = 1;
            yield return Click(app.View.ShopButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Shop));
            int coins = app.Progress.Data.Coins, trays = app.Progress.Amount(BoosterKind.Tray);
            Capture("shop", 720, 1280);
            yield return Click(app.View.ShopOfferButtons["gold-1000"]); yield return WaitShopResult();
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + 1000));
            Assert.That(app.Progress.Amount(BoosterKind.Tray), Is.EqualTo(trays));
            yield return Click(app.View.HomeModalPrimary); yield return Click(app.View.HomeStartButton);
            yield return Click(app.View.DailyButton); coins = app.Progress.Data.Coins;
            yield return Click(app.View.PageItems[0]);
            Assert.That(app.Progress.Data.Coins, Is.GreaterThan(coins));
            Assert.That(app.View.PageItems[0].interactable, Is.False);
            Capture("daily-claimed", 720, 1280); yield return Click(app.View.CloseFlowButton);
            yield return Click(app.View.SettingsButton);
            bool haptics = app.Progress.Data.HapticsEnabled;
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, app.View.HomeMusicSlider.transform.position));
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, app.View.HomeSoundSlider.transform.position));
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, app.View.Settings.HapticsToggle.transform.position));
            Assert.That(app.Progress.Data.MusicVolume, Is.LessThan(.9f)); Assert.That(app.Progress.Data.SoundVolume, Is.LessThan(.9f));
            Assert.That(app.Progress.Data.HapticsEnabled, Is.EqualTo(!haptics));
            var reload = new DatasaveService(new DatasaveOptions { DirectoryName = app.SaveDirectory });
            Assert.That(reload.Load<PixelFlowProgress>(PixelFlowProgress.Key).MusicVolume, Is.EqualTo(app.Progress.Data.MusicVolume));
            Capture("settings", 720, 1280); yield return Click(app.View.HomeModalClose);
            yield return Click(app.View.HomeNodes[1]);
            Assert.That(app.SelectedLevel, Is.Zero); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home));
            Capture("locked-levels", 720, 1280);
            checks.Add("Pointer shop purchase, daily claim with labeled unlock fixture, Home audio sliders/toggle persistence and locked map node");

            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            trays = app.Progress.Amount(BoosterKind.Tray);
            yield return Click(app.View.BoosterButton(BoosterKind.Tray));
            Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(6)); Assert.That(app.Progress.Amount(BoosterKind.Tray), Is.EqualTo(trays - 1));
            int hand = app.Progress.Amount(BoosterKind.Hand);
            yield return Click(app.View.BoosterButton(BoosterKind.Hand)); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Targeting));
            Capture("hand-targets", 720, 1280); app.View.Back();
            Assert.That(app.Progress.Amount(BoosterKind.Hand), Is.EqualTo(hand));
            yield return Click(app.View.BoosterButton(BoosterKind.Hand)); yield return ClickPig(app.Session.QueueAt(0, 1).Id);
            Assert.That(app.Progress.Amount(BoosterKind.Hand), Is.EqualTo(hand - 1)); Assert.That(app.Session.Launches, Is.EqualTo(1));
            int color = app.Progress.Amount(BoosterKind.Color);
            yield return Click(app.View.BoosterButton(BoosterKind.Color)); app.View.Back();
            Assert.That(app.Progress.Amount(BoosterKind.Color), Is.EqualTo(color));
            yield return Click(app.View.BoosterButton(BoosterKind.Color)); int remaining = app.Session.RemainingHealth;
            yield return ClickColor(); Assert.That(app.Session.RemainingHealth, Is.LessThan(remaining));
            Assert.That(app.Progress.Amount(BoosterKind.Color), Is.EqualTo(color - 1));
            string run = app.Session.RunId;
            yield return Click(app.View.PauseButton); yield return Click(app.View.Settings.CloseButton);
            // Test-only Home fixture preserves the existing checkpoint/resume contract.
            // Native Give Up is covered separately and deliberately abandons the attempt.
            typeof(PixelFlowApp).GetMethod("Home", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, null);
            Assert.That(app.Progress.Data.ActiveRun.RunId, Is.EqualTo(run));
            yield return Click(app.View.PlayButton); Assert.That(app.Session.RunId, Is.EqualTo(run)); Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(6));
            checks.Add("Pointer Tray/Hand/Super effects and target cancel; explicitly labeled internal Home checkpoint fixture restores the same run/capacity");

            // Explicit deterministic fixture trigger: source timing is not being
            // measured. Runtime loss/result/paid continue/retry controls stay real.
            app.Session.Level.TimeLimit = .01f;
            float deadline = Time.realtimeSinceStartup + 10;
            while (app.ScreenState == AppScreen.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            app.Session.Level.TimeLimit = 0;
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Lost));
            Assert.That(app.View.ContinueButton.gameObject.activeInHierarchy, Is.True);
            Capture("loss", 720, 1280); coins = app.Progress.Data.Coins;
            yield return Click(app.View.ContinueButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing)); Assert.That(app.Session.Revives, Is.EqualTo(1));
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins - app.Campaign.Economy.ContinueCost)); Assert.That(app.Session.Waiting.Count, Is.EqualTo(6));
            yield return Click(app.View.PauseButton); yield return Click(app.View.RestartButton); yield return Click(app.View.Settings.NoticePrimary);
            Assert.That(app.Session.RunId, Is.Not.EqualTo(run)); Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(5)); Assert.That(app.Session.Revives, Is.Zero);
            yield return Click(app.View.PauseButton); yield return Click(app.View.MenuButton); yield return Click(app.View.Settings.NoticePrimary);
            checks.Add("Fixture timer loss -> pointer coin continue -> confirmed Restart resets run/boosters/revive -> confirmed Give Up returns Home");
            WriteEvidence("full-flow", "Real Editor UI pointer flows. One test-only timer mutation forces loss; this is not original-game timing or device evidence.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator RepresentativeAdvancedLevelPresentationRoute()
        {
            yield return new EnterPlayMode(); yield return Ready();
            // Fixture-only unlock setup, never presented as earned progression.
            app.Progress.Data.UnlockedLevel = 2599; app.Progress.Data.Lives.UnlimitedUntilUtc = ProgressStore.UtcSeconds + 7200; app.Progress.MarkTutorial("basics-v2");
            foreach (int number in new[] { 14, 230, 600, 1001, 1401, 1701, 2001, 2401 })
            {
                // Explicit selection fixture for broad board rendering; the
                // source Home map no longer has the invented pagination menu.
                typeof(PixelFlowApp).GetMethod("SelectLevel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, new object[] { number - 1 });
                yield return null;
                yield return Click(app.View.PlayButton);
                if (app.ScreenState == AppScreen.Confirm) yield return Click(app.View.ConfirmButton);
                yield return FinishOnboarding();
                Assert.That(app.Session.Level.SourceNumber, Is.EqualTo(number));
                Assert.That(app.Board, Is.Not.Null); Capture("source-" + number, 720, 1280);
                yield return Click(app.View.PauseButton); yield return Click(app.View.MenuButton); yield return Click(app.View.Settings.NoticePrimary);
                checks.Add("Selected and rendered source level " + number + " with typed feature state and tutorial");
            }
            WriteEvidence("advanced-presentation", "Explicit fixture unlock/selection; pointer start/tutorial/pause/Home. Rendering is not a full solve or native behavior comparison.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        private void CaptureAspects(string state)
        {
            Capture(state, 1080, 1920);
            Capture(state, 720, 1280);
            Capture(state, 1080, 2340);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator SaveFailureRollsBackBoosterAndRetryRecoversThroughUi()
        {
            yield return new EnterPlayMode(); yield return Ready();
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            // Inject a failing service only into this isolated fixture's owner.
            // The production rule mutation, rollback, error UI and retry are real.
            var original = app.Progress;
            var fake = new PixelFlow.Tests.ProgressFlowTests.SaveFixture { Value = original.Data };
            var failing = new ProgressStore(fake, app.Campaign.LevelCount, app.Campaign.Economy);
            var field = typeof(PixelFlowApp).GetField("progress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(app, failing); fake.Fail = true;
            int trays = failing.Amount(BoosterKind.Tray);
            yield return Click(app.View.BoosterButton(BoosterKind.Tray));
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Error)); Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(5));
            Assert.That(failing.Amount(BoosterKind.Tray), Is.EqualTo(trays));
            Capture("save-error", 720, 1280);
            fake.Fail = false; yield return Click(app.View.ConfirmButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing)); Assert.That(app.Session.ConveyorCapacity, Is.EqualTo(6));
            Assert.That(failing.Amount(BoosterKind.Tray), Is.EqualTo(trays - 1));
            fake.Fail = true;
            yield return Click(app.View.BoosterButton(BoosterKind.Color));
            int remaining = app.Session.RemainingHealth, amount = failing.Amount(BoosterKind.Color);
            yield return ClickColor(); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Error));
            Assert.That(app.Session.RemainingHealth, Is.EqualTo(remaining)); Assert.That(failing.Amount(BoosterKind.Color), Is.EqualTo(amount));
            yield return Click(app.View.CloseFlowButton); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Targeting));
            app.View.Back(); Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Playing));
            fake.Fail = false; field.SetValue(app, original);
            yield return Click(app.View.PauseButton); yield return Click(app.View.MenuButton); yield return Click(app.View.Settings.NoticePrimary);
            checks.Add("Injected disk failure rolls back Tray and Super effects/inventory; pointer Retry commits once; cancel restores target page and gameplay");
            WriteEvidence("save-recovery", "Explicit failing IDatasaveService fixture with real app/UI transaction and rollback. No corruption or modification of owner saves.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator FinalCampaignWinReturnsHomeAndRewardsOnce()
        {
            yield return new EnterPlayMode(); yield return Ready();
            // One-level catalog is an explicit fixture for end-of-campaign routing.
            app.Campaign.Entries.RemoveRange(1, app.Campaign.Entries.Count - 1);
            app.Campaign.Levels.RemoveRange(1, app.Campaign.Levels.Count - 1);
            app.View.SetCampaignCount(1);
            yield return Click(app.View.PlayButton); yield return FinishOnboarding();
            int coins = app.Progress.Data.Coins;
            // Deterministic model clear trigger; result persistence, Next and Home
            // execute in the real scene. The source1 pointer solve is a separate test.
            int safety = 0;
            while (app.Session.Status == SessionStatus.Playing && safety++ < 100)
            {
                var colors = app.Session.VisibleColors(); Assert.That(colors, Is.Not.Empty);
                foreach (int color in colors) app.Session.ClearVisibleColor(color);
            }
            float deadline = Time.realtimeSinceStartup + 10;
            while (app.ScreenState == AppScreen.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Won));
            Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + app.Campaign.Economy.FirstClearCoins));
            yield return new WaitForSecondsRealtime(.1f); Assert.That(app.Progress.Data.Coins, Is.EqualTo(coins + app.Campaign.Economy.FirstClearCoins));
            Capture("campaign-end", 720, 1280); yield return Click(app.View.NextButton);
            Assert.That(app.ScreenState, Is.EqualTo(AppScreen.Home)); Assert.That(app.Progress.Data.ActiveRun, Is.Null);
            checks.Add("Explicit one-level catalog and deterministic clear fixtures -> real result/reward once -> pointer end-of-campaign Home");
            WriteEvidence("campaign-end", "Fixture catalog/clear trigger are labeled; this does not claim a solved original level2600.");
            RestoreInput(); yield return new ExitPlayMode();
        }

        private void EnsureBatchUiRendered()
        {
            // A batch Editor has no drawing Game View. Re-enabled modal graphics
            // can retain depth=-1, which GraphicRaycaster correctly excludes.
            // Render the actual owned canvas rather than bypassing its raycaster.
            Canvas.ForceUpdateCanvases();
            var canvas = app.View.PlayButton.GetComponentInParent<Canvas>();
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>())
                if (graphic.raycastTarget && (graphic.depth < 0 || graphic.canvasRenderer.cull))
                {
                    Capture("ui-warmup", Screen.width, Screen.height, false);
                    break;
                }
        }

        private void Capture(string state, int width, int height, bool writeFile = true)
        {
            var camera = app.WorldCamera;
            var canvas = app.View.PlayButton.GetComponentInParent<Canvas>();
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousPlane = canvas.planeDistance, previousAspect = camera.aspect, previousSize = camera.orthographicSize;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            bool previousAsync = ShaderUtil.allowAsyncCompilation;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                ShaderUtil.allowAsyncCompilation = false;
                camera.targetTexture = target;
                camera.aspect = (float)width / height;
                camera.orthographicSize = Mathf.Max(8.4f, 4.55f / camera.aspect);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = .5f;
                Canvas.ForceUpdateCanvases();
                if (app.Session != null) app.View.UpdateGame(app.Session, camera);
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                if (writeFile)
                {
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    pixels.Apply();
                    var palette = new HashSet<Color32>();
                    var colors = pixels.GetPixels32();
                    // Sparse text/error screens need enough samples to include glyph
                    // edges at the 1080 reference scale; retain the nonblank threshold.
                    for (int y = 0; y < height; y += Math.Max(1, height / 80))
                        for (int x = 0; x < width; x += Math.Max(1, width / 80)) palette.Add(colors[y * width + x]);
                    byte[] png = pixels.EncodeToPNG();
                    Assert.That(png.Length, Is.GreaterThan(1000), "Preview contains rendered pixels");
                    string path = Path.Combine(folder, state + "-" + width + "x" + height + ".png");
                    if (File.Exists(path)) throw new IOException("Review evidence already exists; select a fresh PIXEL_FLOW_REVIEW_RUN: " + path);
                    File.WriteAllBytes(path, png);
                    Assert.That(palette.Count, Is.GreaterThan(24), "Capture contains UI/world detail, not a blank background: " + path);
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
                ShaderUtil.allowAsyncCompilation = previousAsync;
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousPlane;
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                camera.orthographicSize = previousSize;
                Canvas.ForceUpdateCanvases();
                if (app.Session != null) app.View.UpdateGame(app.Session, camera);
                target.Release(); Object.Destroy(target); Object.Destroy(pixels);
            }
        }

        private void WriteEvidence(string name, string scopeDescription)
        {
            File.WriteAllText(Path.Combine(folder, name + ".json"), JsonUtility.ToJson(new Evidence
            {
                status = "pass", unity = Application.unityVersion, scope = scopeDescription,
                saveDirectory = app.SaveDirectory, checks = checks.ToArray(),
                captureMethod = "Offscreen URP camera request, temporarily converting the owned overlay canvas to ScreenSpaceCamera; full-rect preview, not simulated device safe area or Game View capture.",
                inputMethod = "Queued synthetic MouseState events with explicit InputSystem.Update and active EventSystem module Process, isolated devices/settings, and an offscreen UI warmup when graphics have no raycast depth in batchmode; UI/Physics processing, not physical-device input or normal frame scheduling proof."
            }, true));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            RestoreInput();
            if (Application.isPlaying) yield return new ExitPlayMode();
            Environment.SetEnvironmentVariable("PIXEL_FLOW_EDITOR_SAVE_SCOPE", string.IsNullOrEmpty(previousScope) ? null : previousScope);
            SessionState.EraseBool("PixelFlow.Review.Prepared");
            SessionState.EraseString("PixelFlow.Review.Scope");
            SessionState.EraseString("PixelFlow.Review.PreviousScope");
            // Restore the Simulator once after the whole run. Reopening it between
            // fixtures changes Game View geometry and resets the virtual pointer.
        }

        private void RestoreInput()
        {
            if (pointer != null && pointer.added) InputSystem.RemoveDevice(pointer);
            pointer = null;
            foreach (var device in suspendedDevices)
                if (device.added && !device.enabled) InputSystem.EnableDevice(device);
            suspendedDevices.Clear();
            if (ownedInputSettings != null)
            {
                InputSystem.settings = previousInputSettings;
                Object.Destroy(ownedInputSettings);
                ownedInputSettings = null;
            }
        }

        [Serializable]
        private sealed class Evidence
        {
            public string status, unity, scope, saveDirectory, captureMethod, inputMethod;
            public string[] checks;
        }
    }
}
