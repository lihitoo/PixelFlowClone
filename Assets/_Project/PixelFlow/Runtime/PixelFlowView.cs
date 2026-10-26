using System;
using Cysharp.Threading.Tasks;
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
        private readonly RectTransform safe;
        private readonly GameObject home, hud;
        private readonly TMP_Text toast;
        private readonly Dreamy.UI.SafeArea safeArea;
        private float toastUntil;
        private readonly Sprite buttonSprite;
        private readonly Texture2D panelTexture;
        private readonly Sprite panelSprite;
        public Button PlayButton { get; private set; }
        private readonly GameplayPanel gameplay;
        private int gameplayLevel;
        public GameplayPanel Gameplay => gameplay;
        public Button PauseButton => gameplay.SettingsButton;
        public Button BackButton => gameplay.SettingsButton;
        public Button NextButton => gameplay.NextButton;
        public Button RestartButton => settingsView != null ? settingsView.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.Restart) : gameplay.ResultCloseButton;
        public Button MenuButton => settingsView != null ? settingsView.ButtonFor(Dreamy.Feature.Settings.Integration.SettingsCommand.GiveUp) : gameplay.ResultCloseButton;
        public event Action PlayRequested, PauseRequested, MenuRequested, NextRequested;
        public event Action<int> LevelSelected;

        public PixelFlowView(PixelFlowPresentation art, int levelCount, Canvas authoredCanvas, Dreamy.Template.Home.HomePanel authoredHome, GameplayPanel authoredGameplay)
        {
            this.art = art; buttonSprite = art.BlueButton;
            panelSprite = CreatePanelSprite(out panelTexture);
            canvas = authoredCanvas != null ? authoredCanvas : throw new ArgumentNullException(nameof(authoredCanvas));
            var go = canvas.gameObject;
            safe = Rect(go.transform, "Safe Area", 0, 0, 0, 0); Stretch(safe);
            safeArea = safe.gameObject.AddComponent<Dreamy.UI.SafeArea>();

            home = authoredHome.Content;
            BindSourceHome(authoredHome);

            gameplay = authoredGameplay != null ? authoredGameplay : throw new ArgumentNullException(nameof(authoredGameplay));
            hud = gameplay.gameObject;
            gameplay.SettingsRequested += RequestGameplaySettings;
            gameplay.CoinsRequested += RequestGameplayShop;
            gameplay.BoosterRequested += RequestGameplayBooster;
            toast = Text(safe, "Feedback Toast", "", 25, Color.white, 650, 70, 0, 160, new Vector2(.5f, 0));
            toast.outlineWidth = .25f; toast.outlineColor = new Color(.10f, .15f, .25f); toast.gameObject.SetActive(false);
            BuildFullFlow(levelCount);
            home.SetActive(true); hud.SetActive(false); gameplay.HideResult();
            RefreshSafeArea();
        }

        public void ShowHome(PixelFlowProgress progress, int selected)
        {
            toast.gameObject.SetActive(false);
            home.SetActive(true); hud.SetActive(false); gameplay.HideResult();
            HidePages();
            RenderSourceHome(progress, selected, true);
        }

        public void ShowGame(int level)
        {
            ShowHomeChrome(false);
            home.SetActive(false); hud.SetActive(true); gameplay.HideResult();
            HidePages();
            gameplay.HideOffer(); gameplay.HideTarget(); gameplayLevel = level; gameplay.Show().Forget();
        }

        public void UpdateGame(PuzzleSession session, Camera worldCamera)
        {
            gameplay.RenderBelt(session.ConveyorCapacity - session.Active.Count, session.ConveyorCapacity);
            gameplay.RenderTimer(session.Level.TimeLimit > 0 ? session.TimeRemaining : 0);
            Vector3 world = new(-session.Track.HalfWidth + .35f, 0, session.Track.Bottom - .62f);
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, worldCamera.WorldToScreenPoint(world), uiCamera, out var point);
            gameplay.BeltLabel.rectTransform.position = canvas.transform.TransformPoint(point);
        }
        private void RequestGameplaySettings() => PauseRequested?.Invoke();
        private void RequestGameplayShop() => PageRequested?.Invoke(FlowPage.Shop);
        private void RequestGameplayBooster(int index) => BoosterRequested?.Invoke((BoosterKind)index);

        public void ShowResult(bool won, int level, int reward, bool canContinue, int cost, string reason)
        {
            toast.gameObject.SetActive(false); HidePages();
            home.SetActive(false); hud.SetActive(true);
            gameplay.ShowResult(won, level, reward, canContinue, cost, reason,
                () => NextRequested?.Invoke(), () => ContinueRequested?.Invoke(), () => MenuRequested?.Invoke());
        }

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
            PlayRequested = PauseRequested = MenuRequested = NextRequested = null;
            LevelSelected = null;
            if (safe != null) { safe.gameObject.SetActive(false); Object.Destroy(safe.gameObject); }
            gameplay.SettingsRequested -= RequestGameplaySettings;
            gameplay.CoinsRequested -= RequestGameplayShop;
            gameplay.BoosterRequested -= RequestGameplayBooster;
            if (hud != null) hud.SetActive(false);
            ClearFullFlow();
            ClearSourceHome();
            Object.Destroy(panelSprite); Object.Destroy(panelTexture);
        }
    }
}
