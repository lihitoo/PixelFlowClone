using System;
using System.Collections.Generic;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow
{
    public sealed partial class PixelFlowView
    {
        private readonly Dictionary<string, Sprite> homeSprites = new();
        private PixelFlowCampaignConfig homeCampaign;
        private PixelFlowProgress sourceProgress;
        private HomeTuning homeTuning = new();
        private GameObject homeChrome, homeBackground;
        private RectTransform mapContent;
        private readonly Button[] mapNodes = new Button[10];
        private readonly TMP_Text[] mapNumbers = new TMP_Text[10], mapDifficulty = new TMP_Text[10];
        private readonly Image[] mapGlows = new Image[10], mapLabels = new Image[10];
        private readonly Button[] tabs = new Button[3];
        private readonly Image[] tabSelections = new Image[3];
        private readonly Dictionary<FlowPage, Button> homeDock = new();
        private TMP_Text heartCount, heartTimer, rankTabLabel;
        private Image heartIcon, rankIcon;
        private int sourceSelected, mapStart;
        private long homeClock = -1;
        public ScrollRect HomeMap { get; private set; }
        public IReadOnlyList<Button> HomeNodes => mapNodes;
        public Button HomeStartButton => foundationShop != null ? foundationShop.StartButton : tabs[0];
        public Button HomeRankButton => foundationShop != null ? foundationShop.RankButton : tabs[2];
        public Button HomeHeartsButton { get; private set; }
        public Button HomeCoinsButton { get; private set; }
        public event Action HomeRequested, RefillRequested;
        public bool IsHomeModalVisible => homeModal != null && homeModal.activeSelf;

        private Sprite HomeSprite(string name)
        {
            if (!homeSprites.TryGetValue(name, out var sprite))
            {
                sprite = Resources.Load<Sprite>("PixelFlow/UI/Home/" + name);
                if (sprite == null) throw new InvalidOperationException("Missing original Home sprite: " + name);
                homeSprites.Add(name, sprite);
            }
            return sprite;
        }
        public void ConfigureHome(PixelFlowCampaignConfig campaign) { homeCampaign = campaign; homeTuning = campaign.Home; }
        private Image HomeImage(Transform parent, string name, string sprite, float width, float height, float x, float y, Vector2? anchor = null, bool preserve = true) =>
            Image(parent, name, HomeSprite(sprite), Color.white, width, height, x, y, preserve: preserve, anchor: anchor);
        private Button HomeButton(Transform parent, string name, string sprite, float width, float height, float x, float y, Action action, Vector2? anchor = null)
        {
            var image = Image(parent, name, HomeSprite(sprite), Color.white, width, height, x, y, blocks: true, preserve: true, anchor: anchor);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            var colors = button.colors; colors.pressedColor = new Color(.82f, .88f, 1); colors.disabledColor = new Color(.7f, .7f, .7f); button.colors = colors;
            return button;
        }
        private TMP_Text HomeText(Transform parent, string name, string text, float size, float width, float height, float x, float y, Vector2? anchor = null)
        {
            var label = Text(parent, name, text, size, Color.white, width, height, x, y, anchor);
            label.outlineColor = new Color(.07f, .12f, .22f); label.outlineWidth = .22f;
            label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = size;
            return label;
        }
        private Dreamy.Template.Home.HomePanel homePanel;
        private void BindSourceHome(Dreamy.Template.Home.HomePanel panel)
        {
            homePanel = panel ?? throw new ArgumentNullException(nameof(panel));
            homeBackground = panel.Backdrop; homeChrome = panel.Chrome;
            HomeMap = panel.Map; mapContent = HomeMap.content;
            PlayButton = panel.PlayButton; SettingsButton = panel.SettingsButton;
            HomeHeartsButton = panel.LivesButton; HomeCoinsButton = panel.CoinsButton;
            heartCount = panel.HeartCount; heartTimer = panel.HeartTimer;
            heartIcon = panel.HeartIcon; homeWallet = panel.Wallet;
            rankTabLabel = panel.RankLabel; rankIcon = panel.RankButton.image;
            tabs[0] = panel.StartButton; tabs[1] = panel.ShopButton; tabs[2] = panel.RankButton;
            for (int i = 0; i < tabs.Length; i++) tabSelections[i] = panel.TabSelections[i];
            for (int i = 0; i < mapNodes.Length; i++)
            {
                var slot = panel.Levels[i];
                mapNodes[i] = slot.Button; mapNumbers[i] = slot.Number;
                mapDifficulty[i] = slot.Difficulty; mapGlows[i] = slot.Glow; mapLabels[i] = slot.Label;
            }
            foreach (var slot in panel.Docks) homeDock.Add((FlowPage)slot.Destination, slot.Button);
            ShopButton = panel.ShopButton; DailyButton = homeDock[FlowPage.Daily];
            LevelsButton = mapNodes[0]; HelpButton = SettingsButton;
            panel.PlayRequested += OnHomePlay; panel.StartRequested += OnHomeStart;
            panel.RankRequested += OnHomeRank; panel.LivesRequested += OnHomeLives;
            panel.CoinsRequested += OnHomeCoins; panel.LevelRequested += OnHomeLevel;
            panel.DockRequested += OnHomeDock; panel.DestinationRequested += OnHomeDestination;
        }
        private void OnHomePlay() => PlayRequested?.Invoke();
        private void OnHomeStart() => HomeRequested?.Invoke();
        private void OnHomeRank() => PageRequested?.Invoke(FlowPage.Rank);
        private void OnHomeLives() => PageRequested?.Invoke(FlowPage.Lives);
        private void OnHomeCoins() => ShopGoldRequested?.Invoke();
        private void OnHomeDock(int destination) => PageRequested?.Invoke((FlowPage)destination);
        private void OnHomeDestination(Dreamy.Template.Home.HomeDestination destination) =>
            PageRequested?.Invoke(destination == Dreamy.Template.Home.HomeDestination.Shop ? FlowPage.Shop : FlowPage.Settings);
        private void OnHomeLevel(int slot)
        {
            int level = mapStart + slot;
            if (sourceProgress == null || level >= totalLevels) return;
            if (level > sourceProgress.UnlockedLevel) { Toast("Complete Level " + (sourceProgress.UnlockedLevel + 1) + " to keep going."); return; }
            if (level == sourceSelected) PlayRequested?.Invoke(); else LevelSelected?.Invoke(level);
        }
        private string Difficulty(int level) => homeCampaign != null && level >= 0 && level < homeCampaign.LevelCount ? homeCampaign.Entry(level).Difficulty : "Easy";
        private void RenderSourceHome(PixelFlowProgress progress, int selected, bool resetMap)
        {
            sourceProgress = progress; sourceSelected = selected; homeWallet.SetText("{0}", progress.Coins); homeClock = -1;
            homeBackground.SetActive(true); ShowHomeChrome(true); SelectHomeTab(0);
            int desiredStart = Math.Max(0, selected - 2);
            if (resetMap || mapStart != desiredStart)
            {
                mapStart = desiredStart;
                for (int i = 0; i < mapNodes.Length; i++)
                {
                    int level = mapStart + i; bool exists = level < totalLevels;
                    mapNodes[i].gameObject.SetActive(exists); mapGlows[i].gameObject.SetActive(exists && level == selected); if (!exists) continue;
                    string difficulty = Difficulty(level); bool hard = difficulty == "Hard", veryHard = difficulty == "Very Hard", ultra = difficulty == "Ultra Hard";
                    mapNodes[i].image.sprite = HomeSprite(veryHard ? "NodeRed" : hard ? "NodePurple" : ultra ? "NodeOrange" : "NodeBlue");
                    mapNodes[i].image.color = Color.white;
                    mapGlows[i].sprite = HomeSprite(veryHard ? "CurrentRed" : hard ? "CurrentPurple" : "CurrentBlue");
                    mapNumbers[i].text = (level + 1).ToString(); mapLabels[i].gameObject.SetActive(hard || veryHard || ultra);
                    mapLabels[i].sprite = HomeSprite(veryHard ? "LabelRed" : "LabelPurple"); mapDifficulty[i].text = veryHard ? "Very Hard" : difficulty;
                }
                HomeMap.StopMovement(); mapContent.anchoredPosition = new Vector2(0, -(selected - mapStart) * homePanel.NodeSpacing);
            }
            int number = progress.UnlockedLevel + 1;
            homeDock[FlowPage.NoAds].gameObject.SetActive(!progress.NoAds && number >= homeTuning.NoAdsLevel);
            homeDock[FlowPage.Pass].gameObject.SetActive(number >= homeTuning.PassLevel);
            homeDock[FlowPage.FireQuest].gameObject.SetActive(number >= homeTuning.FireQuestLevel);
            homeDock[FlowPage.Arena].gameObject.SetActive(number >= homeTuning.ArenaLevel);
            homeDock[FlowPage.Daily].gameObject.SetActive(number >= homeTuning.DailyLevel);
            bool ranked = number >= homeTuning.LeaderboardLevel;
            rankIcon.sprite = HomeSprite(ranked ? "Leaderboard" : "Lock"); rankTabLabel.text = ranked ? "Rank" : "";
            UpdateHomeClock();
        }
        public void UpdateHomeProfile(PixelFlowProgress data)
        {
            sourceProgress = data; homeWallet.SetText("{0}", data.Coins); homeClock = -1; UpdateHomeClock();
        }
        private void UpdateHomeClock()
        {
            if (sourceProgress?.Lives == null) return;
            long now = ProgressStore.UtcSeconds; if (homeClock == now) return; homeClock = now;
            var state = HomeLifeRules.Advance(sourceProgress.Lives, now, homeTuning.MaximumHearts, homeTuning.HeartIntervalSeconds);
            bool unlimited = state.UnlimitedUntilUtc > now; heartIcon.sprite = HomeSprite(unlimited ? "HeartInfinite" : "Heart");
            heartCount.text = unlimited ? "" : state.Hearts.ToString();
            long seconds = unlimited ? state.UnlimitedUntilUtc - now : HomeLifeRules.SecondsToNext(state, now, homeTuning.HeartIntervalSeconds);
            heartTimer.text = !unlimited && state.Hearts == homeTuning.MaximumHearts ? "MAX" : string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        }
        private void ShowHomeChrome(bool show)
        {
            if (homeChrome == null) return; homeChrome.SetActive(show);
            if (show) homeChrome.transform.SetAsLastSibling();
        }
        private void SelectHomeTab(int tab)
        {
            for (int i = 0; i < tabSelections.Length; i++) tabSelections[i].gameObject.SetActive(i == tab);
        }
        private void TickSourceHome()
        {
            if (homeBackground != null && homeBackground.activeSelf != home.activeSelf) homeBackground.SetActive(home.activeSelf);
            if (!home.activeSelf) return;
            UpdateHomeClock();
            int slot = sourceSelected - mapStart;
            if (slot >= 0 && slot < mapGlows.Length && sourceProgress != null)
                mapGlows[slot].transform.localScale = Vector3.one * (sourceProgress.SmoothMotion ? 1 + .035f * Mathf.Sin(Time.unscaledTime * 3) : 1);
        }
        private void ClearSourceHome()
        {
            if (homePanel != null)
            {
                homePanel.PlayRequested -= OnHomePlay; homePanel.StartRequested -= OnHomeStart;
                homePanel.RankRequested -= OnHomeRank; homePanel.LivesRequested -= OnHomeLives;
                homePanel.CoinsRequested -= OnHomeCoins; homePanel.LevelRequested -= OnHomeLevel;
                homePanel.DockRequested -= OnHomeDock; homePanel.DestinationRequested -= OnHomeDestination;
            }
            HomeRequested = RefillRequested = null;
            MiniShopRequested = ShopGoldRequested = null;
        }
    }
}
