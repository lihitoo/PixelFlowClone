using System;
using System.Collections.Generic;
using System.IO;
using Dreamy.Datasave;
using Newtonsoft.Json;
using PixelFlow.Rules;

namespace PixelFlow
{
    [Serializable]
    public sealed class PixelFlowProgress : SaveData
    {
        public const string Key = "pixel-flow-progress";
        public override string SaveKey => Key;
        public override int Version => 2;
        public int ProfileSchema;
        public int UnlockedLevel;
        public bool SoundEnabled = true, MusicEnabled = true, HapticsEnabled = true;
        public float MasterVolume = 1;
        public List<int> CompletedLevels = new();
        public int Coins, WinsSinceFailure, DailyStreak;
        public int[] Boosters;
        public long LastClaimDay = -1;
        public List<string> Tutorials = new();
        public string LastRewardedRun, LastFailedRun;
        public SessionSnapshot ActiveRun;
        public int ActiveLevel;
        public HomeLives Lives;
        public float MusicVolume = 1, SoundVolume = 1;
        public bool SmoothMotion = true, NotificationsEnabled;
        public bool NoAds;
        public List<string> ShopTransactions = new();
        public List<string> ResourceTransactions = new();
        public override void Migrate(int fromVersion)
        {
            if (fromVersion < 1 || fromVersion > 2) throw new InvalidDataException("Unsupported progress version.");
            // Version1 had only unlocks/completions/sound. The store supplies new
            // economy defaults from verified static tuning without resetting those.
        }
        public override void OnAfterLoad()
        {
            if (ProfileSchema > 2) throw new InvalidDataException("Progress belongs to a newer game version.");
            UnlockedLevel = Math.Max(0, UnlockedLevel);
            CompletedLevels ??= new List<int>(); Tutorials ??= new List<string>();
            if (!float.IsFinite(MasterVolume)) MasterVolume = 1;
            MasterVolume = Math.Clamp(MasterVolume, 0, 1);
        }
    }

    // Copy-on-write: no wallet/inventory/reward mutation becomes visible until
    // the synchronous Datasave write succeeds. UI only sends intent to this owner.
    public sealed partial class ProgressStore
    {
        private readonly IDatasaveService service;
        private readonly int levelCount;
        private readonly EconomyTuning tuning;
        private readonly HomeTuning homeTuning;
        private bool committing;
        public PixelFlowProgress Data { get; private set; }
        public event Action Changed;
        public bool UnicornActive => Data.WinsSinceFailure >= 10;

        public ProgressStore(IDatasaveService service, int levelCount, EconomyTuning tuning = null, HomeTuning homeTuning = null)
        {
            if (service == null || levelCount < 1) throw new ArgumentException("A progress store requires a service and levels.");
            this.service = service; this.levelCount = levelCount; this.tuning = tuning ?? new EconomyTuning(); this.tuning.Validate();
            this.homeTuning = homeTuning ?? new HomeTuning(); this.homeTuning.Validate();
            Data = Clone(service.Load<PixelFlowProgress>(PixelFlowProgress.Key)); Data.OnAfterLoad();
            Data.UnlockedLevel = Math.Clamp(Data.UnlockedLevel, 0, levelCount - 1);
            if (Data.ProfileSchema < 2)
            {
                var upgraded = Clone(Data); upgraded.ProfileSchema = 2; upgraded.Coins = this.tuning.StartingCoins;
                upgraded.Boosters = new int[4]; Array.Fill(upgraded.Boosters, this.tuning.InitialBoosters);
                Commit(upgraded);
            }
            if (Data.Boosters?.Length != 4 || Data.Coins < 0 || Data.WinsSinceFailure < 0 || Data.DailyStreak < 0)
                throw new InvalidDataException("Progress has invalid wallet or inventory data.");
            foreach (int amount in Data.Boosters) if (amount < 0 || amount > 999) throw new InvalidDataException("Invalid booster inventory.");
            if (Data.Lives == null) { var next = Clone(Data); next.Lives = new HomeLives { Hearts = this.homeTuning.MaximumHearts }; Commit(next); }
            HomeLifeRules.Advance(Data.Lives, UtcSeconds, this.homeTuning.MaximumHearts, this.homeTuning.HeartIntervalSeconds);
            if (!float.IsFinite(Data.SoundVolume) || !float.IsFinite(Data.MusicVolume) || Data.SoundVolume < 0 || Data.SoundVolume > 1 || Data.MusicVolume < 0 || Data.MusicVolume > 1)
                throw new InvalidDataException("Invalid audio volume.");
        }

        private static PixelFlowProgress Clone(PixelFlowProgress data) => JsonConvert.DeserializeObject<PixelFlowProgress>(JsonConvert.SerializeObject(data), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 64 });
        private void Commit(PixelFlowProgress candidate)
        {
            if (committing) throw new InvalidOperationException("Progress mutation is already in progress.");
            committing = true;
            try
            {
                service.Save(candidate); Data = candidate;
                foreach (var subscriber in Changed?.GetInvocationList() ?? Array.Empty<Delegate>())
                    try { ((Action)subscriber)(); }
                    catch (Exception error) { UnityEngine.Debug.LogWarning("Progress notification failed: " + error.Message); }
            }
            finally { committing = false; }
        }
        private static int AddCoins(int current, int reward) => checked(current + reward);
        private void CheckLevel(int level) { if (level < 0 || level >= levelCount) throw new ArgumentOutOfRangeException(nameof(level)); }

        public void Complete(int level) => RecordWin(level, "legacy-level-" + level);
        public int RecordWin(int level, string runId)
        {
            CheckLevel(level); if (string.IsNullOrEmpty(runId)) throw new ArgumentException("Missing run identity.");
            if (Data.LastRewardedRun == runId) return 0;
            var next = Clone(Data); bool first = !next.CompletedLevels.Contains(level);
            if (first) next.CompletedLevels.Add(level);
            next.UnlockedLevel = Math.Max(next.UnlockedLevel, Math.Min(level + 1, levelCount - 1));
            int reward = first ? tuning.FirstClearCoins : tuning.ReplayCoins;
            next.Coins = AddCoins(next.Coins, reward); next.WinsSinceFailure = Math.Min(10, next.WinsSinceFailure + 1);
            next.LastRewardedRun = runId; next.ActiveRun = null; Commit(next); return reward;
        }

        public void RecordLoss(int level, SessionSnapshot snapshot)
        {
            CheckLevel(level); if (snapshot == null || snapshot.Status != SessionStatus.Lost) throw new ArgumentException("Missing failed run.");
            var next = Clone(Data);
            if (next.LastFailedRun != snapshot.RunId)
                next.Lives = HomeLifeRules.Spend(next.Lives, UtcSeconds, homeTuning.MaximumHearts, homeTuning.HeartIntervalSeconds) ?? next.Lives;
            next.LastFailedRun = snapshot.RunId; next.WinsSinceFailure = 0;
            next.ActiveLevel = level; next.ActiveRun = snapshot; Commit(next);
        }

        public void Checkpoint(int level, SessionSnapshot snapshot)
        {
            CheckLevel(level); if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var next = Clone(Data); next.ActiveLevel = level; next.ActiveRun = snapshot; Commit(next);
        }
        public void DiscardRun() { var next = Clone(Data); next.ActiveRun = null; Commit(next); }
        public void AbandonRun(long now)
        {
            if (Data.ActiveRun == null) return;
            var next = Clone(Data);
            if (next.LastFailedRun != next.ActiveRun.RunId)
                next.Lives = HomeLifeRules.Spend(next.Lives, now, homeTuning.MaximumHearts, homeTuning.HeartIntervalSeconds) ?? next.Lives;
            next.LastFailedRun = next.ActiveRun.RunId; next.WinsSinceFailure = 0; next.ActiveRun = null;
            Commit(next);
        }
        public int Amount(BoosterKind kind) => (int)kind < 4 && (int)kind >= 0 ? Data.Boosters[(int)kind] : 0;
        public int Price(BoosterKind kind) => (int)kind < 4 && (int)kind >= 0 ? tuning.BoosterPrices[(int)kind] : 0;
        public bool Buy(BoosterKind kind)
        {
            if ((int)kind < 0 || (int)kind >= 4) return false;
            int price = Price(kind), index = (int)kind;
            if (Data.Coins < price || Data.Boosters[index] >= 999) return false;
            var next = Clone(Data); next.Coins -= price; next.Boosters[index]++; Commit(next); return true;
        }
        public bool SpendBooster(BoosterKind kind, int level, SessionSnapshot snapshot)
        {
            CheckLevel(level); int index = (int)kind;
            if (index < 0 || index > 4 || snapshot == null || index < 4 && Data.Boosters[index] < 1 || index == 4 && !UnicornActive) return false;
            var next = Clone(Data); if (index < 4) next.Boosters[index]--;
            next.ActiveRun = snapshot; next.ActiveLevel = level; Commit(next); return true;
        }
        public bool SpendContinue(int level, SessionSnapshot snapshot)
        {
            CheckLevel(level);
            if (snapshot == null || snapshot.Status != SessionStatus.Playing || Data.Coins < tuning.ContinueCost) return false;
            var next = Clone(Data); next.Coins -= tuning.ContinueCost; next.ActiveLevel = level; next.ActiveRun = snapshot; Commit(next); return true;
        }

        public static long UtcDay(DateTime utc) => utc.ToUniversalTime().Date.Ticks / TimeSpan.TicksPerDay;
        public bool CanClaim(long day) => day >= 0 && day > Data.LastClaimDay;
        public int DailyReward(long day)
        {
            int streak = day == Data.LastClaimDay + 1 ? Data.DailyStreak + 1 : 1;
            return tuning.DailyRewards[(Math.Max(streak, 1) - 1) % tuning.DailyRewards.Length];
        }
        public bool ClaimDaily(long day)
        {
            if (!CanClaim(day)) return false;
            var next = Clone(Data); int reward = DailyReward(day);
            next.DailyStreak = day == next.LastClaimDay + 1 ? next.DailyStreak + 1 : 1;
            next.LastClaimDay = day; next.Coins = AddCoins(next.Coins, reward); Commit(next); return true;
        }

        public void ToggleSound() => SetSettings(!Data.SoundEnabled, Data.MusicEnabled, Data.HapticsEnabled, Data.MasterVolume);
        public void SetSettings(bool sound, bool music, bool haptics, float volume)
        {
            if (!float.IsFinite(volume)) throw new ArgumentOutOfRangeException(nameof(volume));
            var next = Clone(Data); next.SoundEnabled = sound; next.MusicEnabled = music; next.HapticsEnabled = haptics; next.MasterVolume = Math.Clamp(volume, 0, 1); Commit(next);
        }
        public void MarkTutorial(string id)
        {
            if (string.IsNullOrEmpty(id) || Data.Tutorials.Contains(id)) return;
            var next = Clone(Data); next.Tutorials.Add(id); Commit(next);
        }
        public void Save() => Commit(Clone(Data));
        public static long UtcSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public bool CanStartLevel(long now) => HomeLifeRules.CanPlay(HomeLifeRules.Advance(Data.Lives, now, homeTuning.MaximumHearts, homeTuning.HeartIntervalSeconds), now);
        public bool RefreshLives(long now)
        {
            var refreshed = HomeLifeRules.Advance(Data.Lives, now, homeTuning.MaximumHearts, homeTuning.HeartIntervalSeconds);
            if (refreshed.Hearts == Data.Lives.Hearts && refreshed.NextHeartUtc == Data.Lives.NextHeartUtc) return false;
            var next = Clone(Data); next.Lives = refreshed; Commit(next); return true;
        }
        public bool RefillLives(long now)
        {
            var lives = HomeLifeRules.Advance(Data.Lives, now, homeTuning.MaximumHearts, homeTuning.HeartIntervalSeconds);
            if (lives.Hearts == homeTuning.MaximumHearts || lives.UnlimitedUntilUtc > now || Data.Coins < homeTuning.HeartRefillCoins) return false;
            var next = Clone(Data); next.Lives = lives; next.Lives.Hearts = homeTuning.MaximumHearts; next.Lives.NextHeartUtc = 0;
            next.Coins -= homeTuning.HeartRefillCoins; Commit(next); return true;
        }
        public void SetAudioLevels(float music, float sound)
        {
            if (!float.IsFinite(music) || !float.IsFinite(sound)) throw new ArgumentException("Invalid volume.");
            if (Math.Abs(Data.MusicVolume - music) < .0001f && Math.Abs(Data.SoundVolume - sound) < .0001f && Data.MusicEnabled == (music > 0) && Data.SoundEnabled == (sound > 0)) return;
            var next = Clone(Data); next.MusicVolume = Math.Clamp(music, 0, 1); next.SoundVolume = Math.Clamp(sound, 0, 1);
            next.MusicEnabled = next.MusicVolume > 0; next.SoundEnabled = next.SoundVolume > 0; Commit(next);
        }
        public void SetHomePreferences(bool haptic, bool notifications, bool smooth)
        {
            var next = Clone(Data); next.HapticsEnabled = haptic; next.NotificationsEnabled = notifications; next.SmoothMotion = smooth; Commit(next);
        }
    }
}
