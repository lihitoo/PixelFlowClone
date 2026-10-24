using System;
using System.Collections.Generic;
using Dreamy.DataConfig;
using PixelFlow.Rules;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;

namespace PixelFlow
{
    [Serializable]
    public sealed class PixelFlowCampaignConfig : ConfigBase
    {
        public float LoopSeconds = 8.8f;
        public float ShotInterval = .045f;
        public List<PaletteEntry> Colors = new();
        public List<LevelDefinition> Levels = new();
        public List<CampaignEntry> Entries = new();
        public EconomyTuning Economy = new();
        public HomeTuning Home = new();
        [NonSerialized] private string cachedChunk;
        [NonSerialized] private CampaignChunk cachedLevels;
        public int LevelCount => Entries != null && Entries.Count > 0 ? Entries.Count : Levels.Count;

        public override void Initialize(string documentName)
        {
            if (Levels == null || Levels.Count == 0 || Colors == null || Colors.Count == 0)
                throw new ArgumentException("Campaign needs levels and a palette.");
            if (LoopSeconds <= 0 || ShotInterval <= 0) throw new ArgumentException("Invalid shot/belt timing.");
            var ids = new HashSet<int>();
            foreach (var entry in Colors)
                if (entry == null || !ids.Add(entry.Id)) throw new ArgumentException("Invalid palette.");
            Entries ??= new List<CampaignEntry>(); Economy ??= new EconomyTuning();
            Economy.Validate();
            Home ??= new HomeTuning(); Home.Validate();
            var numbers = new HashSet<int>();
            foreach (var entry in Entries)
                if (entry == null || entry.SourceNumber < 1 || !numbers.Add(entry.SourceNumber) || string.IsNullOrEmpty(entry.Chunk) || entry.Chunk.Contains("..") || !entry.Chunk.StartsWith("Levels/campaign-", StringComparison.Ordinal)) throw new ArgumentException("Invalid campaign catalog.");
            foreach (var level in Levels) ValidateLevel(level, ids);
        }

        private static void ValidateLevel(LevelDefinition level, HashSet<int> ids)
        {
            level.Validate();
            foreach (var pixel in level.Pixels) if (!ids.Contains(pixel.Color)) throw new ArgumentException("Pixel references a missing color.");
            foreach (var queue in level.Queues)
                foreach (var shooter in queue.Shooters) if (!ids.Contains(shooter.Color)) throw new ArgumentException("Shooter references a missing color.");
            foreach (var obj in level.Objects)
            {
                foreach (var layer in obj.Layers) if (layer.Color >= 0 && !ids.Contains(layer.Color)) throw new ArgumentException("Object references a missing color.");
                if (obj.Release != null && !ids.Contains(obj.Release.Color)) throw new ArgumentException("Caged shooter references a missing color.");
            }
        }

        public CampaignEntry Entry(int index)
        {
            if (index < 0 || index >= LevelCount) throw new ArgumentOutOfRangeException(nameof(index));
            return Entries.Count > 0 ? Entries[index] : new CampaignEntry { SourceNumber = Levels[index].SourceNumber, Title = Levels[index].Title, Difficulty = Levels[index].Difficulty };
        }

        public LevelDefinition GetLevel(int index)
        {
            var entry = Entry(index);
            if (Entries.Count == 0 || index < Levels.Count && Levels[index].SourceNumber == entry.SourceNumber) return Levels[index];
            if (cachedChunk != entry.Chunk)
            {
                var asset = Resources.Load<TextAsset>("PixelFlow/" + entry.Chunk);
                if (asset == null) throw new InvalidOperationException("Missing campaign chunk: " + entry.Chunk);
                CampaignChunk parsed;
                try
                {
                    using var bytes = new MemoryStream(asset.bytes, false);
                    using var gzip = new GZipStream(bytes, CompressionMode.Decompress);
                    using var bounded = new BoundedCampaignStream(gzip, 32 * 1024 * 1024);
                    using var reader = new StreamReader(bounded);
                    using var json = new JsonTextReader(reader) { MaxDepth = 64 };
                    parsed = JsonSerializer.CreateDefault(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None }).Deserialize<CampaignChunk>(json);
                }
                finally { Resources.UnloadAsset(asset); }
                if (parsed?.Levels == null || parsed.Levels.Count == 0 || parsed.Levels.Count > 100) throw new InvalidOperationException("Invalid campaign chunk.");
                var ids = new HashSet<int>(); foreach (var color in Colors) ids.Add(color.Id);
                foreach (var level in parsed.Levels) ValidateLevel(level, ids);
                // One bounded decoded chunk, not 2600 resident level definitions.
                cachedLevels = parsed; cachedChunk = entry.Chunk;
            }
            foreach (var level in cachedLevels.Levels) if (level.SourceNumber == entry.SourceNumber && level.SourceHash == entry.SourceHash) return level;
            throw new InvalidOperationException("Campaign index/chunk identity mismatch.");
        }

        public Color ColorFor(int id)
        {
            foreach (var entry in Colors) if (entry.Id == id) return new Color(entry.R, entry.G, entry.B);
            throw new ArgumentOutOfRangeException(nameof(id));
        }
    }

    [Serializable] public sealed class PaletteEntry { public int Id; public string Name; public float R, G, B; }
    [Serializable] public sealed class CampaignEntry { public int SourceNumber; public string Title, Difficulty, Chunk, SourceHash; public List<string> Features = new(); }
    [Serializable] public sealed class HomeTuning
    {
        public int MaximumHearts = 5, HeartIntervalSeconds = 1800, HeartRefillCoins = 900;
        public int NoAdsLevel = 10, PassLevel = 35, ArenaLevel = 32, FireQuestLevel = 20, DailyLevel = 10, LeaderboardLevel = 100;
        public string Provenance = "Original art/hierarchy and screenshots; unlocks/heart interval/refill values are configurable clone estimates pending native0.36.3 config.";
        public void Validate()
        {
            if (MaximumHearts < 1 || MaximumHearts > 20 || HeartIntervalSeconds < 1 || HeartRefillCoins < 1 || NoAdsLevel < 1 || PassLevel < 1 || ArenaLevel < 1 || FireQuestLevel < 1 || DailyLevel < 1 || LeaderboardLevel < 1)
                throw new ArgumentException("Invalid Home tuning.");
        }
    }
    [Serializable] public sealed class CampaignChunk { public List<LevelDefinition> Levels = new(); }
    [Serializable] public sealed class EconomyTuning
    {
        public int StartingCoins = 300, InitialBoosters = 3, FirstClearCoins = 50, ReplayCoins = 5, ContinueCost = 100;
        public int[] BoosterPrices = { 100, 120, 100, 150 };
        public int[] DailyRewards = { 50, 75, 100, 125, 150, 200, 300 };
        public void Validate()
        {
            if (StartingCoins < 0 || InitialBoosters < 0 || FirstClearCoins < 0 || ReplayCoins < 0 || ContinueCost < 1 || BoosterPrices?.Length != 4 || DailyRewards?.Length != 7) throw new ArgumentException("Invalid economy tuning.");
            foreach (int price in BoosterPrices) if (price < 1) throw new ArgumentException("Invalid booster price.");
            foreach (int reward in DailyRewards) if (reward < 1) throw new ArgumentException("Invalid daily reward.");
        }
    }
    internal sealed class BoundedCampaignStream : Stream
    {
        private readonly Stream source; private readonly long limit; private long consumed;
        public BoundedCampaignStream(Stream source, long limit) { this.source = source; this.limit = limit; }
        public override int Read(byte[] buffer, int offset, int count)
        { int read = source.Read(buffer, offset, count); consumed += read; if (consumed > limit) throw new InvalidDataException("Campaign chunk exceeds its decoded size budget."); return read; }
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException(); public override long Position { get => consumed; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
