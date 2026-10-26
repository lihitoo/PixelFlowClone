#if UNITY_EDITOR || DEVELOPMENT_BUILD || PIXEL_FLOW_VERIFICATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Dreamy.Datasave;
using PixelFlow.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace PixelFlow
{
    // Explicit verification-build scenario. It starts on Home and queues pointer input;
    // it never calls Launch, grants progress or bypasses the scene's normal bootstrap.
    public sealed class PixelFlowSmoke : MonoBehaviour
    {
        private PixelFlowApp app;
        private Mouse mouse;
        private readonly List<string> passed = new();
        private readonly List<string> errors = new();
        private string folder;
        private bool finished;
        private float started;
        public void Initialize(PixelFlowApp owner)
        {
            app = owner;
            started = Time.unscaledTime;
            folder = PixelFlowApp.CommandLineValue("-pixelFlowEvidence");
            if (string.IsNullOrEmpty(folder)) folder = Path.Combine(Application.persistentDataPath, "SmokeEvidence");
            Directory.CreateDirectory(folder);
            Application.logMessageReceived += OnLog;
            mouse = InputSystem.AddDevice<Mouse>("PixelFlowSmokePointer");
            StartCoroutine(Scenario());
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert) errors.Add(message + "\n" + stack);
        }
        private bool Check(bool value, string label)
        {
            if (!value) { errors.Add("FAILED: " + label); Complete(); return false; }
            passed.Add(label); return true;
        }
        private IEnumerator Click(Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return new WaitForSecondsRealtime(.08f);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return new WaitForSecondsRealtime(.12f);
        }
        private IEnumerator Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, button.transform.position));
        }

        private IEnumerator Scenario()
        {
            yield return new WaitForSecondsRealtime(.7f);
            if (!Check(app.ScreenState == AppScreen.Home, "Normal scene launch reaches Home")) yield break;
            yield return new WaitForEndOfFrame(); Capture("home.png");
            yield return Click(app.View.PlayButton);
            int tutorialSteps = 0;
            while (app.ScreenState == AppScreen.Tutorial && tutorialSteps++ < 45)
                yield return Click(app.View.ConfirmButton.gameObject.activeInHierarchy ? app.View.ConfirmButton : app.View.PageNextButton);
            if (!Check(app.ScreenState == AppScreen.Playing && app.SelectedLevel == 0, "Play pointer click starts source level 1")) yield break;
            yield return new WaitForSecondsRealtime(.3f);
            yield return new WaitForEndOfFrame(); Capture("level-1.png");
            yield return Click(app.View.PauseButton);
            if (!Check(app.ScreenState == AppScreen.Settings, "Gear pointer click opens Settings")) yield break;
            int before = app.Session.RemainingPixels;
            yield return new WaitForSecondsRealtime(.3f);
            if (!Check(before == app.Session.RemainingPixels, "Paused puzzle stops simulation")) yield break;
            float previousSound = app.Progress.Data.SoundVolume;
            yield return Click(RectTransformUtility.WorldToScreenPoint(null, app.View.Settings.SoundSlider.transform.position));
            if (!Check(app.Progress.Data.SoundVolume < previousSound, "Sound slider changes and persists preference")) yield break;
            yield return Click(app.View.Settings.CloseButton);
            if (!Check(app.ScreenState == AppScreen.Playing, "Resume pointer click closes modal")) yield break;

            float deadline = Time.unscaledTime + 150;
            bool sawWaiting = false;
            while (app.ScreenState == AppScreen.Playing && Time.unscaledTime < deadline && !finished)
            {
                var session = app.Session;
                foreach (var pig in session.Waiting) if (pig != null) sawWaiting = true;
                int candidate = Choose(session);
                if (candidate >= 0 && session.Active.Count < session.Level.ConveyorLimit)
                {
                    yield return new WaitForSecondsRealtime(.25f); // Let a newly returned/advanced pig settle before pointer down/up.
                    Vector3 position = app.Board.PigPosition(candidate) + Vector3.up * .55f;
                    int launches = session.Launches;
                    yield return Click(app.WorldCamera.WorldToScreenPoint(position));
                    if (session.Status == SessionStatus.Playing && session.Launches == launches)
                    {
                        errors.Add("Pointer did not launch eligible pig " + candidate); Complete(); yield break;
                    }
                }
                else yield return new WaitForSecondsRealtime(.10f);
            }
            if (!Check(app.ScreenState == AppScreen.Won, "Pointer-driven source level 1 clears through normal rule path")) yield break;
            if (sawWaiting) passed.Add("Observed pig return to waiting area during play");
            yield return new WaitForEndOfFrame(); Capture("win.png");
            var reload = new DatasaveService(new DatasaveOptions { DirectoryName = app.SaveDirectory });
            var saved = reload.Load<PixelFlowProgress>(PixelFlowProgress.Key);
            if (!Check(saved.UnlockedLevel >= 1 && saved.CompletedLevels.Contains(0), "Completed level and next unlock reload from Datasave")) yield break;
            yield return Click(app.View.NextButton);
            if (!Check(app.SelectedLevel == 1 && app.ScreenState == AppScreen.Playing, "Next pointer click starts original source level 2")) yield break;
            yield return Click(app.View.PauseButton);
            yield return Click(app.View.RestartButton);
            if (!Check(app.Session.Launches == 0 && app.Session.RemainingPixels == 400, "Restart reconstructs level 2 without mutating source data")) yield break;
            yield return Click(app.View.PauseButton); yield return Click(app.View.MenuButton);
            if (!Check(app.ScreenState == AppScreen.Home, "Home pointer click returns to normal menu")) yield break;
            yield return new WaitForEndOfFrame(); Capture("home-progress.png");
            Complete();
        }

        private void Capture(string name)
        {
            // CaptureScreenshot queues an asynchronous disk write, which can be lost
            // when this explicit scenario exits immediately after its final frame.
            // Every caller is already at end-of-frame; persist owned pixels now.
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                if (texture == null) throw new InvalidOperationException("No rendered screenshot texture.");
                File.WriteAllBytes(Path.Combine(folder, name), texture.EncodeToPNG());
            }
            finally { if (texture != null) Destroy(texture); }
        }

        public static int Choose(PuzzleSession session)
        {
            var visible = new Dictionary<int, int>();
            void Add(int color) { if (color >= 0) visible[color] = visible.GetValueOrDefault(color) + 1; }
            for (int y = 0; y < session.Level.Height; y++)
            {
                for (int x = 0; x < session.Level.Width; x++) if (session.PixelAt(x, y) >= 0) { Add(session.PixelAt(x, y)); break; }
                for (int x = session.Level.Width - 1; x >= 0; x--) if (session.PixelAt(x, y) >= 0) { Add(session.PixelAt(x, y)); break; }
            }
            for (int x = 0; x < session.Level.Width; x++)
            {
                for (int y = 0; y < session.Level.Height; y++) if (session.PixelAt(x, y) >= 0) { Add(session.PixelAt(x, y)); break; }
                for (int y = session.Level.Height - 1; y >= 0; y--) if (session.PixelAt(x, y) >= 0) { Add(session.PixelAt(x, y)); break; }
            }
            int chosen = -1, best = int.MinValue;
            foreach (var pig in session.Shooters.Values)
            {
                if (!session.CanSelect(pig.Id)) continue;
                int score = visible.GetValueOrDefault(pig.Color) * 100;
                if (pig.Location == ShooterLocation.Waiting && score > 0) score += 250;
                if (score == 0 && session.Active.Count > 0) continue;
                if (score > best) { best = score; chosen = pig.Id; }
            }
            return chosen;
        }

        private void Complete()
        {
            if (finished) return; finished = true;
            var result = new Result { status = errors.Count == 0 ? "pass" : "fail", checks = passed.ToArray(), errors = errors.ToArray(), unity = Application.unityVersion,
                scope = "Automated player scenario with queued Input System mouse events and isolated save scope; not human/device/fidelity acceptance." };
            File.WriteAllText(Path.Combine(folder, "smoke.json"), JsonUtility.ToJson(result, true));
            CleanupPointer();
            app.RequestExit(errors.Count == 0 ? 0 : 1);
        }
        private void Update()
        {
            if (!finished && app != null && Time.unscaledTime - started > 180)
            {
                errors.Add("Scenario exceeded its 180-second bound."); Complete();
            }
        }
        [Serializable] private sealed class Result { public string status, unity, scope; public string[] checks, errors; }
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            CleanupPointer();
        }
        private void CleanupPointer()
        {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            mouse = null;
        }
    }
}
#endif
