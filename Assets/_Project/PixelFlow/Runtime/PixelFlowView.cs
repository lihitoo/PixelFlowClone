using System;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PixelFlow
{
    public sealed partial class PixelFlowView : IDisposable
    {
        private static readonly Color Ink = new(.95f, .97f, 1f);
        private readonly PixelFlowPresentation art;
        private readonly Canvas canvas;
        private readonly bool ownsCanvas;
        private readonly RectTransform safe;
        private readonly GameObject home, hud, overlay;
        private readonly TMP_Text levelTitle, counter, beltCount, waitingCount, tip, overlayTitle, overlayDetail, toast, soundLabel;
        private readonly RectTransform fill;
        private readonly Button[] levelButtons;
        private readonly Button resume, next, restart, menu, overlaySound;
        private readonly TMP_Text nextLabel;
        private readonly Dreamy.UI.SafeArea safeArea;
        private float toastUntil;
        private readonly Sprite buttonSprite;
        private readonly Texture2D panelTexture;
        private readonly Sprite panelSprite;
        public Button PlayButton { get; private set; }
        public Button PauseButton { get; }
        public Button BackButton { get; }
        public Button NextButton => next;
        public Button ResumeButton => resume;
        public Button RestartButton => restart;
        public Button MenuButton => menu;
        public Button SoundButton => overlaySound;
        public event Action PlayRequested, PauseRequested, ResumeRequested, RestartRequested, MenuRequested, NextRequested, SoundRequested;
        public event Action<int> LevelSelected;

        public PixelFlowView(Transform owner, PixelFlowPresentation art, int levelCount, Canvas authoredCanvas, Dreamy.Template.Home.HomePanel authoredHome)
        {
            this.art = art; buttonSprite = art.BlueButton;
            panelSprite = CreatePanelSprite(out panelTexture);
            ownsCanvas = authoredCanvas == null;
            var go = ownsCanvas ? new GameObject("Pixel Flow UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) : authoredCanvas.gameObject;
            if (ownsCanvas)
            {
                go.transform.SetParent(owner, false);
                canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
                var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            }
            else canvas = authoredCanvas;
            safe = Rect(go.transform, "Safe Area", 0, 0, 0, 0); Stretch(safe);
            safeArea = safe.gameObject.AddComponent<Dreamy.UI.SafeArea>();

            home = authoredHome.Content;
            BindSourceHome(authoredHome);
            levelButtons = Array.Empty<Button>();
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int index = i;
                levelButtons[i] = Button(home.transform, "Level " + (i + 1), (i + 1).ToString(), 76, 74, (i - (levelCount - 1) / 2f) * 88, -355,
                    buttonSprite, () => LevelSelected?.Invoke(index), 28);
            }

            hud = Rect(safe, "Game HUD", 0, 0, 0, 0).gameObject; Stretch((RectTransform)hud.transform);
            levelTitle = Text(hud.transform, "Level Title", "LEVEL 1", 38, Ink, 400, 65, 0, -50, new Vector2(.5f, 1));
            BackButton = Button(hud.transform, "Back", "<", 82, 78, -286, -65, buttonSprite, () => PauseRequested?.Invoke(), 36, new Vector2(.5f, 1));
            PauseButton = Button(hud.transform, "Pause", "II", 82, 78, 286, -65, buttonSprite, () => PauseRequested?.Invoke(), 32, new Vector2(.5f, 1));
            counter = Text(hud.transform, "Blocks Left", "180 BLOCKS", 22, Ink, 400, 44, 0, -104, new Vector2(.5f, 1));
            var bar = Image(hud.transform, "Progress Track", panelSprite, new Color(.12f, .18f, .32f), 382, 22, 0, -142, anchor: new Vector2(.5f, 1));
            var inset = Rect(bar.transform, "Progress Inset", 0, 0, 0, 0); Stretch(inset);
            inset.offsetMin = new Vector2(3, 3); inset.offsetMax = new Vector2(-3, -3);
            var progress = Image(inset, "Progress", panelSprite, new Color(.99f, .83f, .24f), 0, 0, 0, 0, stretch: true);
            fill = progress.rectTransform; fill.anchorMax = new Vector2(0, 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
            beltCount = Text(hud.transform, "Belt Capacity", "CONVEYOR 0 / 5", 21, Ink, 600, 40, 0, -187, new Vector2(.5f, 1));
            waitingCount = Text(hud.transform, "Waiting Capacity", "WAITING 0 / 5", 21, Ink, 600, 40, 0, -150);
            tip = Text(hud.transform, "Queue Hint", "TAP A FRONT PIG", 18, Ink, 660, 36, 0, -225, new Vector2(.5f, 1));

            overlay = Rect(safe, "Pause and Result", 0, 0, 0, 0).gameObject; Stretch((RectTransform)overlay.transform);
            Image(overlay.transform, "Modal Dim", null, new Color(.04f, .12f, .14f, .78f), 0, 0, 0, 0, stretch: true, blocks: true);
            Image(overlay.transform, "Modal Rim", panelSprite, new Color(.48f, .60f, .88f), 620, 745, 0, 0);
            Image(overlay.transform, "Modal Card", panelSprite, new Color(.19f, .27f, .47f), 610, 735, 0, 0);
            overlayTitle = Text(overlay.transform, "Result Title", "PAUSED", 53, Color.white, 550, 100, 0, 250);
            overlayDetail = Text(overlay.transform, "Result Detail", "Find your next color.", 25, Color.white, 530, 125, 0, 145);
            resume = Button(overlay.transform, "Resume", "KEEP PLAYING", 390, 88, 0, 20, art.PlayButton, () => ResumeRequested?.Invoke());
            next = Button(overlay.transform, "Next", "NEXT LEVEL", 390, 88, 0, 20, art.PlayButton, () => NextRequested?.Invoke());
            nextLabel = next.GetComponentInChildren<TMP_Text>();
            restart = Button(overlay.transform, "Restart", "RETRY", 390, 80, 0, -82, buttonSprite, () => RestartRequested?.Invoke());
            menu = Button(overlay.transform, "Menu", "HOME", 390, 80, 0, -180, buttonSprite, () => MenuRequested?.Invoke());
            overlaySound = Button(overlay.transform, "Sound", "SOUND ON", 260, 54, 0, -278, buttonSprite, () => SoundRequested?.Invoke(), 21);
            soundLabel = overlaySound.GetComponentInChildren<TMP_Text>();
            toast = Text(safe, "Feedback Toast", "", 25, Color.white, 650, 70, 0, 160, new Vector2(.5f, 0));
            toast.outlineWidth = .25f; toast.outlineColor = new Color(.10f, .15f, .25f); toast.gameObject.SetActive(false);
            BuildFullFlow(levelCount);
            home.SetActive(true); hud.SetActive(false); overlay.SetActive(false);
            RefreshSafeArea();
        }

        public void ShowHome(PixelFlowProgress progress, int selected)
        {
            toast.gameObject.SetActive(false);
            home.SetActive(true); hud.SetActive(false); overlay.SetActive(false);
            HidePages();
            RenderSourceHome(progress, selected, true);
            for (int i = 0; i < levelButtons.Length; i++)
            {
                levelButtons[i].interactable = i <= progress.UnlockedLevel;
                levelButtons[i].transform.localScale = Vector3.one * (i == selected ? 1.12f : 1);
                levelButtons[i].GetComponent<Image>().color = progress.CompletedLevels.Contains(i) ? new Color(.64f, 1f, .72f) : Color.white;
            }
        }

        public void ShowGame(int level)
        {
            ShowHomeChrome(false);
            home.SetActive(false); hud.SetActive(true); overlay.SetActive(false);
            HidePages();
            levelTitle.text = "LEVEL " + (level + 1);
            tip.text = level == 0 ? "TAP A FRONT PIG TO START" : "MATCH COLORS. KEEP SPACE.";
        }

        public void UpdateGame(PuzzleSession session, Camera worldCamera)
        {
            counter.SetText("{0} PIXELS  /  {1} HITS LEFT", session.RemainingPixels, session.RemainingHealth);
            beltCount.SetText("CONVEYOR  {0} / {1}", session.Active.Count, session.ConveyorCapacity);
            int waiting = 0; foreach (var pig in session.Waiting) if (pig != null) waiting++;
            waitingCount.SetText("WAITING  {0} / {1}", waiting, session.Waiting.Count);
            fill.anchorMax = new Vector2(session.Progress01, 1);
            Vector3 world = new(0, 0, session.Track.Bottom - .82f);
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, worldCamera.WorldToScreenPoint(world), uiCamera, out var point);
            waitingCount.rectTransform.position = canvas.transform.TransformPoint(point);
            if (session.Launches > 0) tip.text = waiting > 0 ? "TAP A WAITING PIG TO SEND IT AGAIN" : "ONLY THE FRONT PIGS CAN LAUNCH";
            timerText.text = session.Level.TimeLimit > 0 ? "TIME " + Mathf.CeilToInt(session.TimeRemaining) : "";
        }

        public void ShowPause(bool sound)
        {
            toast.gameObject.SetActive(false);
            HidePages(); overlay.SetActive(true); overlayTitle.text = "PAUSED"; overlayDetail.text = "Take a breath. Find your next color.";
            resume.gameObject.SetActive(true); next.gameObject.SetActive(false); restart.gameObject.SetActive(true); UpdateSound(sound);
            continueButton.gameObject.SetActive(false);
        }

        public void ShowResult(bool won, bool lastLevel, int launches, string reason, bool sound)
        {
            toast.gameObject.SetActive(false);
            HidePages(); overlay.SetActive(true); resume.gameObject.SetActive(false); next.gameObject.SetActive(won); restart.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(false);
            overlayTitle.text = won ? "LEVEL CLEAR!" : "TRY AGAIN";
            overlayDetail.text = won ? "Perfect flow!\n" + launches + " launches to clear the board." : reason + "\nContinue this run or retry for free.";
            nextLabel.text = lastLevel ? "BACK TO HOME" : "NEXT LEVEL";
            UpdateSound(sound);
        }

        public void UpdateSound(bool enabled) => soundLabel.text = enabled ? "SOUND ON" : "SOUND OFF";
        public void Toast(string message)
        {
            toast.text = message; toast.gameObject.SetActive(true); toast.transform.SetAsLastSibling(); toastUntil = Time.unscaledTime + 2.5f;
        }
        public void Tick()
        {
            RefreshSafeArea();
            TickSourceHome();
            if (toast.gameObject.activeSelf && Time.unscaledTime >= toastUntil) toast.gameObject.SetActive(false);
        }

        private void RefreshSafeArea()
        {
            if (!Application.isPlaying) return; // Authoring preview keeps full rect; SafeArea.Awake has not run in Edit Mode.
            safeArea.Apply();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static Sprite CreatePanelSprite(out Texture2D texture)
        {
            // One small owned nine-slice for panels/bars. Button artwork stays on
            // buttons; stretching its octagonal silhouette distorts other UI roles.
            const int size = 64, radius = 16;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Pixel Flow Rounded UI", wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var colors = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - .5f, x + .5f - (size - radius), 0);
                float dy = Mathf.Max(radius - y - .5f, y + .5f - (size - radius), 0);
                byte alpha = (byte)(255 * Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f));
                colors[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(colors); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, Vector4.one * radius);
        }
        private static RectTransform Rect(Transform parent, string name, float width, float height, float x, float y, Vector2? anchor = null)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }
        private Image Image(Transform parent, string name, Sprite sprite, Color color, float width, float height, float x, float y, bool stretch = false, bool blocks = false, bool preserve = false, Vector2? anchor = null)
        {
            var rect = Rect(parent, name, width, height, x, y, anchor); if (stretch) Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = blocks;
            image.type = sprite == panelSprite && !preserve ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = preserve; return image;
        }
        private TMP_Text Text(Transform parent, string name, string value, float size, Color color, float width, float height, float x, float y, Vector2? anchor = null)
        {
            var rect = Rect(parent, name, width, height, x, y, anchor);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = art.Font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow; return text;
        }
        private Button Button(Transform parent, string name, string title, float width, float height, float x, float y, Sprite sprite, Action action, float fontSize = 28, Vector2? anchor = null)
        {
            var image = Image(parent, name, sprite, Color.white, width, height, x, y, blocks: true, anchor: anchor);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.pressedColor = new Color(.78f, .82f, .78f); colors.disabledColor = new Color(.55f, .63f, .60f, .65f); button.colors = colors;
            button.onClick.AddListener(() => action());
            var label = Text(image.transform, "Label", title, fontSize, Color.white, width - 20, height - 8, 0, 3);
            label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = fontSize;
            label.outlineColor = new Color(.11f, .21f, .21f); label.outlineWidth = .17f;
            return button;
        }
        public void Dispose()
        {
            PlayRequested = PauseRequested = ResumeRequested = RestartRequested = MenuRequested = NextRequested = SoundRequested = null;
            LevelSelected = null;
            if (ownsCanvas && canvas != null) { canvas.gameObject.SetActive(false); Object.Destroy(canvas.gameObject); }
            else
            {
                if (safe != null) { safe.gameObject.SetActive(false); Object.Destroy(safe.gameObject); }
                if (homeBackground != null) { homeBackground.SetActive(false); Object.Destroy(homeBackground); }
            }
            ClearFullFlow();
            ClearSourceHome();
            Object.Destroy(panelSprite); Object.Destroy(panelTexture);
        }
#if UNITY_EDITOR
        public void DisposeAuthoringPreview()
        {
            ClearFullFlow(); ClearSourceHome();
            Object.DestroyImmediate(canvas.gameObject); Object.DestroyImmediate(panelSprite); Object.DestroyImmediate(panelTexture);
        }
#endif
    }
}
