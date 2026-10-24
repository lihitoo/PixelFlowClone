using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Template.Home
{
    public sealed partial class HomePanel
    {
        [Serializable]
        public sealed class LevelSlot
        {
            public Button Button;
            public TMP_Text Number, Difficulty;
            public Image Glow, Label;
        }

        [Serializable]
        public sealed class DockSlot
        {
            public int Destination;
            public Button Button;
        }

        [Header("Authored map and mobile chrome")]
        [SerializeField] private GameObject content, chrome, backdrop;
        [SerializeField] private ScrollRect map;
        [SerializeField] private LevelSlot[] levels = Array.Empty<LevelSlot>();
        [SerializeField] private DockSlot[] docks = Array.Empty<DockSlot>();
        [SerializeField] private Button playButton, startButton, rankButton, livesButton, coinsButton;
        [SerializeField] private Image[] tabSelections;
        [SerializeField] private TMP_Text wallet, heartCount, heartTimer, rankLabel;
        [SerializeField] private Image heartIcon;
        [SerializeField] private float nodeSpacing = 327;

        public GameObject Content => content;
        public GameObject Chrome => chrome;
        public GameObject Backdrop => backdrop;
        public ScrollRect Map => map;
        public LevelSlot[] Levels => levels;
        public DockSlot[] Docks => docks;
        public Button PlayButton => playButton;
        public Button StartButton => startButton;
        public Button ShopButton => shopButton;
        public Button SettingsButton => settingsButton;
        public Button RankButton => rankButton;
        public Button LivesButton => livesButton;
        public Button CoinsButton => coinsButton;
        public Image[] TabSelections => tabSelections;
        public TMP_Text Wallet => wallet;
        public TMP_Text HeartCount => heartCount;
        public TMP_Text HeartTimer => heartTimer;
        public TMP_Text RankLabel => rankLabel;
        public Image HeartIcon => heartIcon;
        public float NodeSpacing => nodeSpacing;

        public event Action PlayRequested, StartRequested, RankRequested, LivesRequested, CoinsRequested;
        public event Action<int> LevelRequested, DockRequested;

        private readonly System.Collections.Generic.List<Action> removeMapListeners = new();

        private void BindMap()
        {
            Bind(playButton, () => PlayRequested?.Invoke());
            Bind(startButton, () => StartRequested?.Invoke());
            Bind(rankButton, () => RankRequested?.Invoke());
            Bind(livesButton, () => LivesRequested?.Invoke());
            Bind(coinsButton, () => CoinsRequested?.Invoke());
            for (int i = 0; i < levels.Length; i++)
            {
                int slot = i;
                Bind(levels[i].Button, () => LevelRequested?.Invoke(slot));
            }
            foreach (var slot in docks) Bind(slot.Button, () => DockRequested?.Invoke(slot.Destination));
        }

        private void Bind(Button button, Action action)
        {
            if (button == null) return;
            UnityEngine.Events.UnityAction handler = () => action();
            button.onClick.AddListener(handler);
            removeMapListeners.Add(() => button.onClick.RemoveListener(handler));
        }

        private void UnbindMap()
        {
            foreach (var remove in removeMapListeners) remove();
            removeMapListeners.Clear();
        }
    }
}
