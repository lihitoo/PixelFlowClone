using System;
using System.Collections.Generic;

namespace PixelFlow.Rules
{
    public enum SessionStatus { Playing, Won, Lost }
    public enum ShooterLocation { Queue, Conveyor, Waiting, Finished, Stored }
    public enum LaunchResult { Accepted, ConveyorFull, Unavailable, SessionFinished, FeatureBlocked }

    public sealed class ShooterState
    {
        public int Id { get; internal set; }
        public int Color { get; internal set; }
        public int Ammo { get; internal set; }
        public int Column { get; internal set; }
        public ShooterLocation Location { get; internal set; }
        public float Progress { get; internal set; }
        public ShooterKind Kind { get; internal set; }
        public bool Revealed { get; internal set; }
        public bool Astronaut { get; internal set; }
        public int Ice { get; internal set; }
        public int Counter { get; internal set; }
        public int PipeId { get; internal set; }
        public int FeatureColor { get; internal set; }
        internal float ShotTimer;
    }

    public readonly struct ShotRecord
    {
        public readonly int ShooterId, X, Y, Color, ObjectId;
        public readonly bool Destroyed;
        public ShotRecord(int shooterId, int x, int y, int color, bool destroyed = true, int objectId = -1)
        { ShooterId = shooterId; X = x; Y = y; Color = color; Destroyed = destroyed; ObjectId = objectId; }
    }

    public sealed partial class PuzzleSession
    {
        private const float StepSeconds = .01f;
        private readonly List<ShooterState>[] queues;
        private readonly List<ShooterState> active = new();
        private ShooterState[] waiting;
        private readonly Dictionary<int, ShooterState> shooters = new();
        private readonly int[] pixels, pixelHealth, pixelOwner;
        private readonly bool[] revealedPixels;
        private readonly float loopSeconds, shotInterval;
        private double accumulator;
        private int extraConveyor;

        public LevelDefinition Level { get; }
        public TrackGeometry Track { get; }
        public string RunId { get; private set; }
        public SessionStatus Status { get; private set; } = SessionStatus.Playing;
        public string LossReason { get; private set; }
        public int RemainingPixels { get; private set; }
        public int InitialPixels { get; }
        public int RemainingHealth { get; private set; }
        public int InitialHealth { get; private set; }
        public int HitCount { get; private set; }
        public int Launches { get; private set; }
        public int Revives { get; private set; }
        public int ConveyorCapacity => Level.ConveyorLimit + extraConveyor;
        public float TimeRemaining { get; private set; }
        public float Progress01 => InitialHealth > 0 ? 1 - (float)RemainingHealth / InitialHealth : 1;
        public IReadOnlyList<ShooterState> Active => active;
        public IReadOnlyList<ShooterState> Waiting => waiting;
        public IReadOnlyDictionary<int, ShooterState> Shooters => shooters;
        public int ColumnCount => queues.Length;
        public event Action<ShotRecord> Shot;
        public event Action<SessionStatus> Finished;

        public PuzzleSession(LevelDefinition level, float loopSeconds = 8.8f, float shotInterval = .045f)
        {
            level.Validate();
            if (!float.IsFinite(loopSeconds) || !float.IsFinite(shotInterval) || loopSeconds <= 0 || shotInterval <= 0)
                throw new ArgumentOutOfRangeException(nameof(loopSeconds));
            Level = level; Track = new TrackGeometry(level.Width, level.Height);
            this.loopSeconds = loopSeconds; this.shotInterval = shotInterval;
            RunId = Guid.NewGuid().ToString("N"); TimeRemaining = level.TimeLimit;
            pixels = new int[level.Width * level.Height]; Array.Fill(pixels, -1);
            pixelOwner = new int[pixels.Length]; Array.Fill(pixelOwner, -1);
            pixelHealth = new int[pixels.Length]; revealedPixels = new bool[pixels.Length];
            foreach (var pixel in level.Pixels)
            {
                int index = pixel.Y * level.Width + pixel.X;
                pixels[index] = pixel.Color; pixelHealth[index] = pixel.Health;
                revealedPixels[index] = !pixel.Hidden; InitialHealth += pixel.Health;
                for (int y = pixel.Y; y < pixel.Y + pixel.SpanY; y++)
                for (int x = pixel.X; x < pixel.X + pixel.SpanX; x++) pixelOwner[y * level.Width + x] = index;
            }
            InitialPixels = RemainingPixels = level.Pixels.Count;
            waiting = new ShooterState[level.SlotCount];
            queues = new List<ShooterState>[level.Queues.Count];
            for (int column = 0; column < queues.Length; column++)
            {
                queues[column] = new List<ShooterState>();
                foreach (var source in level.Queues[column].Shooters)
                {
                    var state = CreateShooter(source, column, ShooterLocation.Queue);
                    shooters.Add(state.Id, state); queues[column].Add(state);
                }
            }
            InitializeObjects();
            RemainingHealth = InitialHealth;
            RevealFronts(); UpdateExposedObjects();
        }

        private static ShooterState CreateShooter(ShooterDefinition source, int column, ShooterLocation location) => new()
        {
            Id = source.Id, Color = source.Color, Ammo = source.Ammo, Column = column, Location = location,
            Kind = source.Kind, Revealed = !source.Surprise, Astronaut = source.Astronaut,
            Ice = source.Ice, Counter = source.Counter, FeatureColor = source.FeatureColor, PipeId = source.PipeId
        };

        public int PixelAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Level.Width || y >= Level.Height) return -1;
            int owner = pixelOwner[y * Level.Width + x]; return owner < 0 ? -1 : pixels[owner];
        }
        public int PixelHealthAt(int x, int y) { int owner = pixelOwner[y * Level.Width + x]; return owner < 0 ? 0 : pixelHealth[owner]; }
        public bool PixelRevealed(int x, int y) { int owner = pixelOwner[y * Level.Width + x]; return owner < 0 || revealedPixels[owner]; }
        public int QueueCount(int column) => queues[column].Count;
        public ShooterState QueueAt(int column, int offset = 0) => offset >= 0 && offset < queues[column].Count ? queues[column][offset] : null;
        public int QueueOffset(int id) => shooters.TryGetValue(id, out var state) && state.Column >= 0 ? queues[state.Column].IndexOf(state) : -1;

        private bool IndividuallyEligible(ShooterState state, bool rear)
        {
            if (state.Ice > 0 || state.Kind == ShooterKind.Lock || state.Kind == ShooterKind.Totem) return false;
            return state.Location == ShooterLocation.Waiting ||
                state.Location == ShooterLocation.Queue && (rear || QueueAt(state.Column) == state);
        }

        private List<ShooterState> LaunchGroup(int id)
        {
            var result = new List<ShooterState>(); var visited = new HashSet<int>(); var pending = new Queue<int>(); pending.Enqueue(id);
            while (pending.Count > 0)
            {
                int item = pending.Dequeue(); if (!visited.Add(item)) continue;
                var state = shooters[item]; if (state.Location == ShooterLocation.Finished) continue;
                result.Add(state);
                foreach (var link in Level.Links)
                    if (link.Members.Contains(item)) foreach (int other in link.Members) if (!visited.Contains(other)) pending.Enqueue(other);
            }
            return result;
        }

        public bool CanSelect(int id) => CanSelect(id, false);
        public bool CanSelect(int id, bool rear)
        {
            if (Status != SessionStatus.Playing || !shooters.TryGetValue(id, out var state) || !IndividuallyEligible(state, rear)) return false;
            if (state.Kind == ShooterKind.Mallet)
            {
                bool any = false, matching = false;
                foreach (var obj in objects) if (obj.Active && obj.Definition.Kind == BoardObjectKind.MusicToy) { any = true; if (obj.Color == state.FeatureColor) matching = true; }
                if (any && !matching) return false;
            }
            var group = LaunchGroup(id);
            foreach (var member in group)
            {
                if (member.Ice > 0 || member.Kind is ShooterKind.Lock or ShooterKind.Totem || member.Location is ShooterLocation.Conveyor or ShooterLocation.Stored) return false;
                if (member.Id != id && member.Kind is ShooterKind.Hammer or ShooterKind.Mallet) return false;
                if (member.Location == ShooterLocation.Queue && !rear)
                {
                    int position = QueueOffset(member.Id);
                    for (int i = 0; i < position; i++) if (!group.Contains(QueueAt(member.Column, i))) return false;
                }
            }
            return group.Count > 0;
        }

        public LaunchResult Launch(int id) => Launch(id, false);
        public LaunchResult Launch(int id, bool rear)
        {
            if (Status != SessionStatus.Playing) return LaunchResult.SessionFinished;
            if (!CanSelect(id, rear)) return LaunchResult.Unavailable;
            var state = shooters[id];
            if (state.Kind is ShooterKind.Hammer or ShooterKind.Mallet)
            {
                if (rear || !ActivateSupport(state)) return LaunchResult.FeatureBlocked;
                Launches++; RevealFronts(); CheckWin(); return LaunchResult.Accepted;
            }
            var group = LaunchGroup(id);
            if (active.Count + group.Count > ConveyorCapacity) return LaunchResult.ConveyorFull;
            foreach (var member in group)
            {
                if (member.Location == ShooterLocation.Queue) queues[member.Column].Remove(member);
                else waiting[Array.IndexOf(waiting, member)] = null;
                member.Location = ShooterLocation.Conveyor; member.Revealed = true;
                float entry = 0;
                foreach (var other in active) if (other.Progress < .07f) entry = Math.Min(entry, other.Progress - .07f);
                member.Progress = entry; member.ShotTimer = 0; active.Add(member);
                if (member.Astronaut) OpenSpaceship(member.Color);
            }
            Launches++; RevealFronts(); return LaunchResult.Accepted;
        }

        private void RevealFronts()
        {
            foreach (var queue in queues) if (queue.Count > 0) queue[0].Revealed = true;
        }

        public void Tick(float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0 || deltaSeconds > 60) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (Status != SessionStatus.Playing) return;
            accumulator += deltaSeconds;
            while (accumulator + 1e-8 >= StepSeconds && Status == SessionStatus.Playing)
            { accumulator -= StepSeconds; Step(); }
            if (Status != SessionStatus.Playing) accumulator = 0;
        }

        private void Step()
        {
            if (Level.TimeLimit > 0)
            {
                TimeRemaining = Math.Max(0, TimeRemaining - StepSeconds);
                if (TimeRemaining <= 0) { Finish(SessionStatus.Lost, "Time is up."); return; }
            }
            for (int index = 0; index < active.Count && Status == SessionStatus.Playing;)
            {
                var shooter = active[index]; float before = shooter.Progress;
                shooter.Progress += StepSeconds / loopSeconds;
                bool earlyReturn = CrossConveyorDoor(shooter, before);
                if (!earlyReturn && shooter.Progress >= 0)
                {
                    shooter.ShotTimer -= StepSeconds;
                    if (shooter.ShotTimer <= 0)
                    {
                        shooter.ShotTimer += shotInterval;
                        var point = Track.At(Math.Min(shooter.Progress, .999999f));
                        if (point.Lane >= 0) TryHit(shooter, point.Side, point.Lane);
                    }
                }
                if (Status != SessionStatus.Playing) return;
                if (shooter.Ammo == 0)
                {
                    shooter.Location = ShooterLocation.Finished; active.RemoveAt(index); OnShooterFinished(shooter.Color);
                }
                else if (shooter.Progress >= 1)
                {
                    int slot = FreeSlot();
                    if (slot < 0) { shooter.Progress = .99999f; Finish(SessionStatus.Lost, "The waiting area is full."); return; }
                    shooter.Location = ShooterLocation.Waiting; waiting[slot] = shooter; active.RemoveAt(index);
                }
                else index++;
            }
            if (active.Count == 0 && RemainingHealth > 0)
            {
                bool ammo = false;
                foreach (var shooter in shooters.Values)
                    if (shooter.Location != ShooterLocation.Finished && shooter.Ammo > 0) { ammo = true; break; }
                if (!ammo) Finish(SessionStatus.Lost, "No ammunition remains.");
            }
        }

        private int FreeSlot()
        {
            for (int i = 0; i < waiting.Length; i++) if (waiting[i] == null && (i >= slotHealth.Length || slotHealth[i] == 0)) return i;
            return -1;
        }
        public bool SlotBlocked(int slot) => slot < slotHealth.Length && slotHealth[slot] > 0;
        public int SlotHealth(int slot) => slot < slotHealth.Length ? slotHealth[slot] : 0;
        private void CheckWin() { if (RemainingHealth == 0) Finish(SessionStatus.Won); }
        private void Finish(SessionStatus status, string reason = null)
        {
            if (Status != SessionStatus.Playing) return;
            Status = status; LossReason = reason; Finished?.Invoke(status);
        }
    }
}
