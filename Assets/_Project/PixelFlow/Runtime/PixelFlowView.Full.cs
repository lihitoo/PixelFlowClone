using System;
using System.Collections.Generic;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow
{
    public enum FlowPage { Levels, Shop, Daily, Settings, Help, Targets, Confirm, Error, Lives, Rank, NoAds, Pass, FireQuest, Arena }
    public enum SettingAction { Sound, Music, Haptics, Quieter, Louder }

    public sealed partial class PixelFlowView
    {
        private int totalLevels, levelPage, targetPage, helpStep;
        private TMP_Text homeWallet, timerText, pageTitle, pageDetail, pageWallet;
        private GameObject page;
        private Button continueButton, pageBack, pageNext, pagePrevious, confirmYes, confirmNo;
        private Button[] pageItems;
        private readonly Button[] boosterButtons = new Button[5];
        private readonly TMP_Text[] boosterLabels = new TMP_Text[5];
        private Action backAction, confirmAction;
        private FlowPage currentPage;
        private ProgressStore pageProgress;
        private PixelFlowCampaignConfig pageCampaign;
        private PuzzleSession targetSession;
        private bool handTarget;
        public Button LevelsButton { get; private set; }
        public Button ShopButton { get; private set; }
        public Button DailyButton { get; private set; }
        private Button homeSettingsButton;
        public Button SettingsButton { get => foundationShop != null ? foundationShop.SettingsButton : homeSettingsButton; private set => homeSettingsButton = value; }
        public Button HelpButton { get; private set; }
        public Button ContinueButton => continueButton;
        public Button CloseFlowButton => pageBack;
        public Button PageNextButton => pageNext;
        public Button ConfirmButton => confirmYes;
        public IReadOnlyList<Button> PageItems => pageItems;
        public Button BoosterButton(BoosterKind kind) => boosterButtons[(int)kind];
        public void SetCampaignCount(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count)); totalLevels = count;
        }
        public event Action<FlowPage> PageRequested;
        public event Action<SettingAction> SettingRequested;
        public event Action<BoosterKind> BoosterRequested, PurchaseRequested;
        public event Action<int> TargetRequested;
        public event Action DailyRequested, ContinueRequested;
        private static readonly string[] BoosterNames = { "TRAY", "HAND", "SHUFFLE", "SUPER", "UNICORN" };
        private static readonly string[] BoosterDescriptions = {
            "Add one conveyor space (maximum 8).", "Launch an available pig from any queue row.",
            "Reorder eligible pigs still in the queues.", "Clear the exposed layer of one visible color." };
        private static readonly string[] HelpTitles = { "MAKE THE COLORS FLOW", "KEEP SOME ROOM", "BOOST YOUR NEXT MOVE", "SAVE YOUR JOURNEY" };
        private static readonly string[] HelpDetails = {
            "Tap a front pig. It circles the board and shoots matching colors along its line of sight. Other colors and obstacles can block a shot.",
            "A pig with ammo returns to a waiting space. Tap it to launch again. A returning pig needs a free space. Linked pigs launch together; ice, locks and other blockers have their own conditions.",
            "Tray adds conveyor capacity. Hand selects a deeper pig. Shuffle changes the queue. Super clears a visible color. Unicorn unlocks after 10 wins and charges through hits, using waiting pigs' ammo.",
            "Pause or go Home to keep this run. Continue resumes it. Clear levels to earn coins and unlock the campaign. Shop and daily rewards work offline; no ad or payment is required." };

        private void BuildFullFlow(int count)
        {
            totalLevels = count;
            timerText = Text(hud.transform, "Time Left", "", 20, Ink, 200, 35, 240, -125, new Vector2(.5f, 1));
            for (int i = 0; i < boosterButtons.Length; i++)
            {
                int index = i;
                boosterButtons[i] = Button(hud.transform, BoosterNames[i], BoosterNames[i], 115, 86, (i - 2) * 124, 75,
                    buttonSprite, () => BoosterRequested?.Invoke((BoosterKind)index), 19, new Vector2(.5f, 0));
                boosterLabels[i] = boosterButtons[i].GetComponentInChildren<TMP_Text>();
                var icon = Resources.Load<Sprite>("PixelFlow/UI/" + (i == 3 ? "Super" : BoosterNames[i].Substring(0, 1) + BoosterNames[i].Substring(1).ToLowerInvariant()));
                if (icon != null)
                {
                    Image(boosterButtons[i].transform, "Booster Icon", icon, Color.white, 44, 44, 0, 20, preserve: true);
                    boosterLabels[i].rectTransform.anchoredPosition = new Vector2(0, -21); boosterLabels[i].fontSizeMax = 16;
                }
            }
            continueButton = Button(overlay.transform, "Continue", "CONTINUE", 390, 88, 0, 20, art.PlayButton, () => ContinueRequested?.Invoke());
            Button(overlay.transform, "Pause Settings", "SETTINGS", 240, 50, -130, -335, buttonSprite, () => PageRequested?.Invoke(FlowPage.Settings), 19);
            Button(overlay.transform, "Pause Help", "HELP", 240, 50, 130, -335, buttonSprite, () => PageRequested?.Invoke(FlowPage.Help), 19);
            page = Rect(safe, "Flow Page", 0, 0, 0, 0).gameObject; Stretch((RectTransform)page.transform);
            Image(page.transform, "Page Background", null, new Color(.12f, .17f, .30f, 1), 0, 0, 0, 0, stretch: true, blocks: true);
            pageBack = Button(page.transform, "Close Flow", "< BACK", 145, 66, -255, -55, buttonSprite, () => backAction?.Invoke(), 22, new Vector2(.5f, 1));
            pageTitle = Text(page.transform, "Page Title", "", 36, Ink, 480, 90, 40, -140, new Vector2(.5f, 1));
            pageTitle.enableAutoSizing = true; pageTitle.fontSizeMin = 20; pageTitle.fontSizeMax = 36;
            pageWallet = Text(page.transform, "Page Wallet", "", 24, Ink, 550, 45, 0, -205, new Vector2(.5f, 1));
            pageDetail = Text(page.transform, "Page Description", "", 26, Ink, 590, 230, 0, 235);
            pageDetail.enableAutoSizing = true; pageDetail.fontSizeMin = 19; pageDetail.fontSizeMax = 26;
            pageItems = new Button[20];
            for (int i = 0; i < pageItems.Length; i++)
            {
                int index = i;
                pageItems[i] = Button(page.transform, "Page Item " + i, "", 130, 95, 0, 0, buttonSprite, () => ClickPageItem(index), 25);
            }
            pagePrevious = Button(page.transform, "Previous Page", "< PREV", 230, 74, -145, 100, buttonSprite, () => ChangePage(-1), 24, new Vector2(.5f, 0));
            pageNext = Button(page.transform, "Next Page", "NEXT >", 230, 74, 145, 100, art.PlayButton, () => ChangePage(1), 24, new Vector2(.5f, 0));
            confirmYes = Button(page.transform, "Confirm", "YES", 420, 82, 0, -140, art.PlayButton, () => confirmAction?.Invoke());
            confirmNo = Button(page.transform, "Cancel", "CANCEL", 420, 72, 0, -240, buttonSprite, () => backAction?.Invoke());
            page.SetActive(false);
            BuildHomeModal();
        }
        private Button HomeEntry(string name, string title, float x, float y, FlowPage flow) =>
            Button(home.transform, name, title, 280, 72, x, y, buttonSprite, () => PageRequested?.Invoke(flow), 25);
        private void HidePages(bool keepShop = false) { if (page != null) page.SetActive(false); if (homeModal != null) homeModal.SetActive(false); }
        private void BeginPage(FlowPage flow, string title, string detail, Action close)
        {
            ShowHomeChrome(false); if (homeModal != null) homeModal.SetActive(false);
            currentPage = flow; backAction = close; page.SetActive(true); page.transform.SetAsLastSibling();
            pageTitle.text = title; pageDetail.text = detail; pageWallet.text = "";
            pageDetail.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            foreach (var item in pageItems) item.gameObject.SetActive(false);
            pagePrevious.gameObject.SetActive(false); pageNext.gameObject.SetActive(false);
            confirmYes.gameObject.SetActive(false); confirmNo.gameObject.SetActive(false);
        }
        private void Item(int i, string label, float width, float height, float x, float y, bool enabled = true)
        {
            var item = pageItems[i]; item.gameObject.SetActive(true); item.interactable = enabled;
            var rect = (RectTransform)item.transform; rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, y);
            var text = item.GetComponentInChildren<TMP_Text>(); text.rectTransform.sizeDelta = new Vector2(width - 24, height - 12); text.text = label;
            item.GetComponent<Image>().color = Color.white;
        }
        public void ShowLevels(ProgressStore progress, PixelFlowCampaignConfig campaign, int selected, Action close)
        {
            pageProgress = progress; pageCampaign = campaign; levelPage = selected / 20;
            BeginPage(FlowPage.Levels, "CAMPAIGN", null, close); DrawLevels();
        }
        private void DrawLevels()
        {
            pageWallet.text = "LEVELS " + (levelPage * 20 + 1) + " - " + Math.Min(totalLevels, (levelPage + 1) * 20) + " / " + totalLevels;
            for (int i = 0; i < 20; i++)
            {
                int level = levelPage * 20 + i;
                if (level >= totalLevels) { pageItems[i].gameObject.SetActive(false); continue; }
                bool unlocked = level <= pageProgress.Data.UnlockedLevel;
                string tag = !unlocked ? "LOCKED" : pageProgress.Data.CompletedLevels.Contains(level) ? "CLEAR" : "PLAY";
                var entry = pageCampaign.Entry(level);
                if (entry != null && entry.Difficulty != "Easy" && unlocked) tag += "\n" + entry.Difficulty.ToUpperInvariant();
                Item(i, (level + 1) + "\n" + tag, 138, 106, (i % 4 - 1.5f) * 154, 270 - i / 4 * 125, unlocked);
                if (pageProgress.Data.CompletedLevels.Contains(level)) pageItems[i].GetComponent<Image>().color = new Color(.65f, 1, .75f);
            }
            pagePrevious.gameObject.SetActive(true); pageNext.gameObject.SetActive(true);
            pagePrevious.interactable = levelPage > 0; pageNext.interactable = (levelPage + 1) * 20 < totalLevels;
            pageNext.GetComponentInChildren<TMP_Text>().text = "NEXT >";
        }
        public void ShowDaily(ProgressStore progress, long day, Action close)
        {
            pageProgress = progress; BeginPage(FlowPage.Daily, "DAILY GIFT", "A new gift each UTC day.\nConsecutive days increase the reward.\n\nCurrent streak: " + progress.Data.DailyStreak + "\nCOINS " + progress.Data.Coins, close);
            Item(0, progress.CanClaim(day) ? "CLAIM " + progress.DailyReward(day) + " COINS" : "CLAIMED - COME BACK TOMORROW", 550, 100, 0, -110, progress.CanClaim(day));
        }
        public void ShowSettings(ProgressStore progress, Action close)
        {
            pageProgress = progress; var p = progress.Data; BeginPage(FlowPage.Settings, "SETTINGS", null, close);
            string[] labels = { "SOUND  " + (p.SoundEnabled ? "ON" : "OFF"), "MUSIC  " + (p.MusicEnabled ? "ON" : "OFF"), "HAPTICS  " + (p.HapticsEnabled ? "ON" : "OFF"), "VOLUME -", "VOLUME +" };
            for (int i = 0; i < 3; i++) Item(i, labels[i], 510, 90, 0, 270 - i * 115);
            Item(3, labels[3], 245, 85, -135, -150); Item(4, labels[4], 245, 85, 135, -150);
            pageWallet.text = "MASTER VOLUME " + Mathf.RoundToInt(p.MasterVolume * 100) + "%";
        }
        public void ShowHelp(Action close)
        {
            helpStep = 0; BeginPage(FlowPage.Help, HelpTitles[0], HelpDetails[0], close); DrawHelp();
        }
        private void DrawHelp()
        {
            pageTitle.text = HelpTitles[helpStep]; pageDetail.text = HelpDetails[helpStep]; pageWallet.text = (helpStep + 1) + " / " + HelpTitles.Length;
            pagePrevious.gameObject.SetActive(helpStep > 0); pageNext.gameObject.SetActive(true); pageNext.interactable = true;
            pageNext.GetComponentInChildren<TMP_Text>().text = helpStep + 1 == HelpTitles.Length ? "LET'S PLAY" : "NEXT >";
        }
        private List<int> targets = new();
        public void ShowTargets(PuzzleSession session, bool hand, PixelFlowCampaignConfig campaign, Action close)
        {
            targetSession = session; handTarget = hand; pageCampaign = campaign; targetPage = 0; targets.Clear();
            if (hand) foreach (var pig in session.Shooters.Values) { if (pig.Location == ShooterLocation.Queue && session.CanSelect(pig.Id, true)) targets.Add(pig.Id); }
            else targets.AddRange(session.VisibleColors());
            BeginPage(FlowPage.Targets, hand ? "HAND: CHOOSE A PIG" : "SUPER: CHOOSE COLOR", null, close); DrawTargets();
        }
        private void DrawTargets()
        {
            pageWallet.text = "Nothing is spent until an effect succeeds.";
            for (int i = 0; i < pageItems.Length; i++)
            {
                int index = targetPage * 20 + i;
                if (index >= targets.Count) { pageItems[i].gameObject.SetActive(false); continue; }
                int id = targets[index];
                string label = handTarget ? (targetSession.Shooters[id].Revealed ? "PIG " + id + "\n" + targetSession.Shooters[id].Ammo : "?") : "COLOR " + id;
                Item(i, label, 138, 100, (i % 4 - 1.5f) * 154, 270 - i / 4 * 118);
                int color = handTarget ? targetSession.Shooters[id].Color : id;
                pageItems[i].GetComponent<Image>().color = handTarget && !targetSession.Shooters[id].Revealed ? Color.gray : pageCampaign.ColorFor(color);
            }
            pagePrevious.gameObject.SetActive(targetPage > 0); pageNext.gameObject.SetActive((targetPage + 1) * 20 < targets.Count);
            pageNext.GetComponentInChildren<TMP_Text>().text = "NEXT >";
        }
        public void ShowConfirm(string title, string detail, string yes, Action accept, Action cancel, bool error = false)
        {
            BeginPage(error ? FlowPage.Error : FlowPage.Confirm, title, detail, cancel); confirmAction = accept;
            confirmYes.gameObject.SetActive(true); confirmNo.gameObject.SetActive(true); confirmYes.GetComponentInChildren<TMP_Text>().text = yes;
        }
        private void ChangePage(int direction)
        {
            if (currentPage == FlowPage.Levels) { levelPage += direction; DrawLevels(); }
            else if (currentPage == FlowPage.Targets) { targetPage += direction; DrawTargets(); }
            else if (currentPage == FlowPage.Help)
            {
                if (helpStep + direction == HelpTitles.Length) backAction?.Invoke();
                else { helpStep = Math.Clamp(helpStep + direction, 0, HelpTitles.Length - 1); DrawHelp(); }
            }
        }
        private void ClickPageItem(int index)
        {
            switch (currentPage)
            {
                case FlowPage.Levels: LevelSelected?.Invoke(levelPage * 20 + index); break;
                case FlowPage.Shop: PurchaseRequested?.Invoke((BoosterKind)index); break;
                case FlowPage.Daily: DailyRequested?.Invoke(); break;
                case FlowPage.Settings: SettingRequested?.Invoke((SettingAction)index); break;
                case FlowPage.Targets: if (targetPage * 20 + index < targets.Count) TargetRequested?.Invoke(targets[targetPage * 20 + index]); break;
            }
        }
        public void UpdateBoosters(ProgressStore progress, PuzzleSession session)
        {
            for (int i = 0; i < 5; i++)
            {
                boosterLabels[i].text = BoosterNames[i] + "\n" + (i < 4 ? "x" + progress.Amount((BoosterKind)i) : progress.UnicornActive ? session.UnicornCharge + "%" : progress.Data.WinsSinceFailure + "/10 WINS");
                boosterButtons[i].interactable = session.Status == SessionStatus.Playing && (i < 4 || progress.UnicornActive && session.UnicornCharge >= 100);
            }
        }
        public void ShowContinue(bool available, int cost)
        {
            continueButton.gameObject.SetActive(available); continueButton.GetComponentInChildren<TMP_Text>().text = "CONTINUE  " + cost + " COINS";
        }
        public void Back() => backAction?.Invoke();
        private void ClearFullFlow()
        {
            backAction = confirmAction = null; PageRequested = null; SettingRequested = null;
            BoosterRequested = PurchaseRequested = null; TargetRequested = null; DailyRequested = ContinueRequested = null;
        }
    }
}
