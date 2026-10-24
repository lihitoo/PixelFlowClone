using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PixelFlow.Rules
{
    [Serializable] public sealed class ShooterSnapshot
    {
        public int Id, Ammo, Ice, Counter, Column; public ShooterLocation Location;
        public float Progress, ShotTimer; public bool Revealed;
    }
    [Serializable] public sealed class ObjectSnapshot { public int Id, Layer, Health; public bool Active; public bool[] Seats; }
    [Serializable] public sealed class SessionSnapshot
    {
        public int Version = 1, SourceNumber, Width, Height, Launches, HitCount, ExtraConveyor, Revives, UnicornUsedAt;
        public string Identity, RunId; public SessionStatus Status; public string LossReason;
        public float LoopSeconds, ShotInterval, TimeRemaining; public double Accumulator;
        public int[] PixelHealth, Waiting, SlotHealth, DoorHealth; public bool[] RevealedPixels;
        public int[][] Queues; public int[] Active;
        public List<ShooterSnapshot> Shooters = new(); public List<ObjectSnapshot> Objects = new();
    }

    public sealed partial class PuzzleSession
    {
        private int unicornUsedAt;
        public int UnicornCharge => Math.Min(100, HitCount - unicornUsedAt);
        public bool CanAddTray => Status == SessionStatus.Playing && ConveyorCapacity < 8;
        public bool AddTray()
        {
            if (!CanAddTray) return false;
            extraConveyor++; FeatureChanged?.Invoke("Conveyor capacity increased"); return true;
        }

        public bool Shuffle(int seed)
        {
            if (Status != SessionStatus.Playing) return false;
            var linked = new HashSet<int>(); foreach (var link in Level.Links) foreach (int id in link.Members) linked.Add(id);
            var states = new List<ShooterState>(); var positions = new List<(int column, int row)>();
            for (int c = 0; c < queues.Length; c++)
            for (int r = 0; r < queues[c].Count; r++)
            {
                var state = queues[c][r];
                if (state.Kind != ShooterKind.Normal || state.Ice > 0 || state.PipeId >= 0 || linked.Contains(state.Id)) continue;
                states.Add(state); positions.Add((c, r));
            }
            if (states.Count < 2) return false;
            var random = new Random(seed);
            for (int i = states.Count - 1; i > 0; i--) { int j = random.Next(i + 1); (states[i], states[j]) = (states[j], states[i]); }
            bool changed = false;
            for (int i = 0; i < states.Count; i++) if (queues[positions[i].column][positions[i].row] != states[i]) changed = true;
            if (!changed) { var last = states[states.Count - 1]; states.RemoveAt(states.Count - 1); states.Insert(0, last); }
            for (int i = 0; i < states.Count; i++)
            { states[i].Column = positions[i].column; queues[positions[i].column][positions[i].row] = states[i]; }
            RevealFronts(); FeatureChanged?.Invoke("Queues shuffled"); return true;
        }

        public List<int> VisibleColors()
        {
            var colors = new HashSet<int>();
            foreach (var target in VisibleTargets())
            {
                int color = target.obj != null ? target.obj.Color : pixels[target.pixel];
                if (color >= 0) colors.Add(color);
            }
            var result = new List<int>(colors); result.Sort(); return result;
        }

        private List<(int pixel, BoardObjectState obj)> VisibleTargets()
        {
            var result = new List<(int, BoardObjectState)>(); var seen = new HashSet<string>();
            for (int side = 0; side < 4; side++)
            {
                var boardSide = (BoardSide)side;
                int count = boardSide is BoardSide.Left or BoardSide.Right ? Level.Height : Level.Width;
                for (int lane = 0; lane < count; lane++)
                    if (FindTarget(boardSide, lane, out int pixel, out var obj) && seen.Add(obj == null ? "p" + pixel : "o" + obj.Definition.Id)) result.Add((pixel, obj));
            }
            return result;
        }

        public bool ClearVisibleColor(int color)
        {
            if (Status != SessionStatus.Playing) return false;
            var targets = VisibleTargets(); bool changed = false;
            foreach (var target in targets)
            {
                if (Status != SessionStatus.Playing) break;
                if (target.obj == null)
                {
                    if (pixels[target.pixel] != color) continue;
                    int health = pixelHealth[target.pixel];
                    for (int hit = 0; hit < health && Status == SessionStatus.Playing; hit++) ApplyHit(-1, target.pixel, null, color);
                }
                else
                {
                    if (!target.obj.Active || target.obj.Color != color) continue;
                    int layer = target.obj.Layer, health = target.obj.Health;
                    for (int hit = 0; hit < health && target.obj.Active && target.obj.Layer == layer && Status == SessionStatus.Playing; hit++) ApplyHit(-1, target.pixel, target.obj, color);
                }
                changed = true;
            }
            return changed;
        }

        public bool UseUnicorn()
        {
            if (Status != SessionStatus.Playing || UnicornCharge < 100) return false;
            var participants = new List<ShooterState>(); foreach (var state in waiting) if (state != null && state.Ammo > 0) participants.Add(state);
            if (participants.Count == 0) return false;
            bool changed = false;
            foreach (var state in participants)
            {
                while (state.Ammo > 0 && Status == SessionStatus.Playing)
                {
                    int target = -1; BoardObjectState targetObject = null;
                    foreach (var visible in VisibleTargets())
                        if ((visible.obj?.Color ?? pixels[visible.pixel]) == state.Color) { target = visible.pixel; targetObject = visible.obj; break; }
                    if (target < 0)
                        for (int i = 0; i < pixels.Length; i++) if (pixels[i] == state.Color && pixelHealth[i] > 0) { target = i; break; }
                    if (target < 0)
                        foreach (var obj in objects) if (obj.Active && obj.Definition.Goal && obj.Color == state.Color) { targetObject = obj; target = obj.Definition.Cells[0].Y * Level.Width + obj.Definition.Cells[0].X; break; }
                    if (target < 0) break;
                    state.Ammo--; ApplyHit(-1, target, targetObject, state.Color); changed = true;
                }
                if (state.Ammo == 0)
                {
                    int slot = Array.IndexOf(waiting, state); if (slot >= 0) waiting[slot] = null;
                    state.Location = ShooterLocation.Finished; OnShooterFinished(state.Color);
                }
            }
            if (changed) { unicornUsedAt = HitCount; FeatureChanged?.Invoke("Super Unicorn"); }
            return changed;
        }

        public bool CanRevive
        {
            get
            {
                if (Status != SessionStatus.Lost || Revives >= 2 || waiting.Length >= 8) return false;
                foreach (var state in shooters.Values) if (state.Ammo > 0 && state.Location != ShooterLocation.Finished) return true;
                return false;
            }
        }
        public bool Revive()
        {
            if (!CanRevive) return false;
            Array.Resize(ref waiting, waiting.Length + 1);
            if (Level.TimeLimit > 0) TimeRemaining = Math.Max(TimeRemaining, 30);
            foreach (var queue in queues)
                if (queue.Count > 0)
                {
                    var front = queue[0]; front.Ice = 0;
                    if (front.Kind is ShooterKind.Lock or ShooterKind.Totem) RemoveSupport(front);
                }
            Status = SessionStatus.Playing; LossReason = null; Revives++; RevealFronts(); return true;
        }

        private string Identity()
        {
            var text = new StringBuilder("PixelFlowRules2|").Append(Level.SourceHash).Append('|').Append(Level.SourceNumber).Append('|').Append(Level.Width).Append('|').Append(Level.Height);
            foreach (var pixel in Level.Pixels) text.Append('|').Append(pixel.X).Append(',').Append(pixel.Y).Append(',').Append(pixel.Color).Append(',').Append(pixel.Health);
            foreach (var queue in Level.Queues) { text.Append(';'); foreach (var shooter in queue.Shooters) text.Append(shooter.Id).Append(',').Append(shooter.Ammo).Append(',').Append(shooter.Kind).Append(':'); }
            foreach (var obj in Level.Objects) { text.Append('/').Append(obj.Id).Append(obj.Kind); foreach (var layer in obj.Layers) text.Append(',').Append(layer.Color).Append(':').Append(layer.Health); }
            using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "");
        }

        public SessionSnapshot CaptureState()
        {
            var result = new SessionSnapshot
            {
                SourceNumber = Level.SourceNumber, Identity = Identity(), Width = Level.Width, Height = Level.Height, RunId = RunId,
                Status = Status, LossReason = LossReason, Launches = Launches, HitCount = HitCount, ExtraConveyor = extraConveyor,
                Revives = Revives, UnicornUsedAt = unicornUsedAt, LoopSeconds = loopSeconds, ShotInterval = shotInterval,
                TimeRemaining = TimeRemaining, Accumulator = accumulator, PixelHealth = (int[])pixelHealth.Clone(),
                RevealedPixels = (bool[])revealedPixels.Clone(), SlotHealth = (int[])slotHealth.Clone(), DoorHealth = (int[])doorHealth.Clone(),
                Waiting = new int[waiting.Length], Active = new int[active.Count], Queues = new int[queues.Length][]
            };
            for (int i = 0; i < waiting.Length; i++) result.Waiting[i] = waiting[i]?.Id ?? -1;
            for (int i = 0; i < active.Count; i++) result.Active[i] = active[i].Id;
            for (int c = 0; c < queues.Length; c++) { result.Queues[c] = new int[queues[c].Count]; for (int i = 0; i < queues[c].Count; i++) result.Queues[c][i] = queues[c][i].Id; }
            foreach (var state in shooters.Values) result.Shooters.Add(new ShooterSnapshot { Id = state.Id, Ammo = state.Ammo, Ice = state.Ice, Counter = state.Counter, Column = state.Column, Location = state.Location, Progress = state.Progress, ShotTimer = state.ShotTimer, Revealed = state.Revealed });
            foreach (var state in objects) result.Objects.Add(new ObjectSnapshot { Id = state.Definition.Id, Layer = state.Layer, Health = state.Health, Active = state.Active, Seats = (bool[])state.Seats.Clone() });
            return result;
        }

        public void RestoreState(SessionSnapshot snapshot)
        {
            ValidateSnapshot(snapshot);
            RunId = snapshot.RunId; Status = snapshot.Status; LossReason = snapshot.LossReason; Launches = snapshot.Launches;
            HitCount = snapshot.HitCount; extraConveyor = snapshot.ExtraConveyor; Revives = snapshot.Revives; unicornUsedAt = snapshot.UnicornUsedAt;
            TimeRemaining = snapshot.TimeRemaining; accumulator = snapshot.Accumulator;
            Array.Copy(snapshot.PixelHealth, pixelHealth, pixelHealth.Length); Array.Copy(snapshot.RevealedPixels, revealedPixels, pixels.Length);
            Array.Fill(pixels, -1); RemainingPixels = 0; RemainingHealth = 0;
            foreach (var definition in Level.Pixels)
            {
                int index = definition.Y * Level.Width + definition.X;
                if (pixelHealth[index] > 0) { pixels[index] = definition.Color; RemainingPixels++; RemainingHealth += pixelHealth[index]; }
            }
            foreach (var saved in snapshot.Shooters)
            {
                var state = shooters[saved.Id]; state.Ammo = saved.Ammo; state.Ice = saved.Ice; state.Counter = saved.Counter;
                state.Column = saved.Column; state.Location = saved.Location; state.Progress = saved.Progress; state.ShotTimer = saved.ShotTimer; state.Revealed = saved.Revealed;
            }
            for (int i = 0; i < objects.Count; i++)
            {
                var state = objects[i]; var saved = snapshot.Objects[i];
                state.Layer = saved.Layer; state.Health = saved.Health; state.Active = saved.Active; state.Seats = (bool[])saved.Seats.Clone();
                if (state.Definition.Goal) RemainingHealth += state.Remaining;
            }
            for (int c = 0; c < queues.Length; c++) { queues[c].Clear(); foreach (int id in snapshot.Queues[c]) { shooters[id].Column = c; queues[c].Add(shooters[id]); } }
            active.Clear(); foreach (int id in snapshot.Active) active.Add(shooters[id]);
            waiting = new ShooterState[snapshot.Waiting.Length]; for (int i = 0; i < waiting.Length; i++) if (snapshot.Waiting[i] >= 0) waiting[i] = shooters[snapshot.Waiting[i]];
            Array.Copy(snapshot.SlotHealth, slotHealth, slotHealth.Length); Array.Copy(snapshot.DoorHealth, doorHealth, doorHealth.Length);
        }

        private void ValidateSnapshot(SessionSnapshot s)
        {
            if (s == null || s.Version != 1 || s.Identity != Identity() || s.SourceNumber != Level.SourceNumber || string.IsNullOrEmpty(s.RunId) || s.RunId.Length > 64 || s.Width != Level.Width || s.Height != Level.Height)
                throw new ArgumentException("Saved run does not match this level/version.");
            if (s.PixelHealth?.Length != pixels.Length || s.RevealedPixels?.Length != pixels.Length || s.Shooters?.Count != shooters.Count || s.Objects?.Count != objects.Count || s.Queues?.Length != queues.Length || s.SlotHealth?.Length != slotHealth.Length || s.DoorHealth?.Length != doorHealth.Length || s.Waiting == null || s.Waiting.Length < Level.SlotCount || s.Waiting.Length > 8 || s.Active == null)
                throw new ArgumentException("Saved run has invalid dimensions.");
            if (!Enum.IsDefined(typeof(SessionStatus), s.Status) || s.ExtraConveyor < 0 || Level.ConveyorLimit + s.ExtraConveyor > 8 || s.Active.Length > Level.ConveyorLimit + s.ExtraConveyor || s.Revives < 0 || s.Revives > 2 || s.Launches < 0 || s.HitCount < 0 || s.UnicornUsedAt < 0 || s.UnicornUsedAt > s.HitCount || !float.IsFinite(s.TimeRemaining) || s.TimeRemaining < 0 || double.IsNaN(s.Accumulator) || s.Accumulator < 0 || s.Accumulator > StepSeconds + 1e-6 || Math.Abs(s.LoopSeconds - loopSeconds) > .0001 || Math.Abs(s.ShotInterval - shotInterval) > .0001)
                throw new ArgumentException("Saved run has invalid counters.");
            var seen = new HashSet<int>(); var states = new Dictionary<int, ShooterSnapshot>();
            foreach (var item in s.Shooters)
                if (!shooters.ContainsKey(item.Id) || !states.TryAdd(item.Id, item) || item.Column < 0 || item.Column >= queues.Length || item.Ammo < 0 || item.Ice < 0 || item.Counter < 0 || !float.IsFinite(item.Progress) || item.Progress < -4 || item.Progress > 1.001f || !float.IsFinite(item.ShotTimer) || !Enum.IsDefined(typeof(ShooterLocation), item.Location)) throw new ArgumentException("Invalid saved shooter.");
            void Occupant(int id, ShooterLocation expected)
            {
                if (!states.TryGetValue(id, out var state) || state.Location != expected || !seen.Add(id)) throw new ArgumentException("Duplicate or misplaced saved shooter.");
            }
            foreach (var queue in s.Queues) { if (queue == null) throw new ArgumentException("Missing saved queue."); foreach (int id in queue) Occupant(id, ShooterLocation.Queue); }
            foreach (int id in s.Active) Occupant(id, ShooterLocation.Conveyor);
            foreach (int id in s.Waiting) if (id >= 0) Occupant(id, ShooterLocation.Waiting);
            foreach (var item in states.Values) if (item.Location is not (ShooterLocation.Finished or ShooterLocation.Stored) && !seen.Contains(item.Id)) throw new ArgumentException("Unplaced saved shooter.");
            foreach (var pixel in Level.Pixels) { int hp = s.PixelHealth[pixel.Y * Level.Width + pixel.X]; if (hp < 0 || hp > pixel.Health) throw new ArgumentException("Invalid saved pixel health."); }
            for (int i = 0; i < pixels.Length; i++) if (pixelOwner[i] != i && s.PixelHealth[i] != 0) throw new ArgumentException("Health on an empty cell.");
            for (int i = 0; i < objects.Count; i++)
            {
                var item = s.Objects[i]; var definition = objects[i].Definition;
                if (item.Id != definition.Id || item.Seats?.Length != definition.Layers.Count || item.Layer < 0 || item.Layer > definition.Layers.Count || item.Health < 0 || item.Active && (item.Layer >= definition.Layers.Count || item.Health > definition.Layers[item.Layer].Health || item.Health == 0)) throw new ArgumentException("Invalid saved object.");
            }
            int remaining = 0;
            foreach (var pixel in Level.Pixels) remaining += s.PixelHealth[pixel.Y * Level.Width + pixel.X];
            for (int i = 0; i < objects.Count; i++)
            {
                var item = s.Objects[i]; var definition = objects[i].Definition;
                if (!definition.Goal || !item.Active) continue;
                remaining += item.Health;
                for (int layer = item.Layer + 1; layer < definition.Layers.Count; layer++) remaining += definition.Layers[layer].Health;
            }
            if ((s.Status == SessionStatus.Won) != (remaining == 0)) throw new ArgumentException("Saved result contradicts remaining goals.");
            for (int i = 0; i < slotHealth.Length; i++)
            {
                int maximum = 0; foreach (var cage in Level.SlotCages) if (cage.Slot == i) maximum = cage.Health;
                if (s.SlotHealth[i] < 0 || s.SlotHealth[i] > maximum) throw new ArgumentException("Invalid saved slot.");
            }
            for (int i = 0; i < doorHealth.Length; i++) if (s.DoorHealth[i] < 0 || s.DoorHealth[i] > Level.ConveyorDoors[i].Health) throw new ArgumentException("Invalid saved door.");
        }
    }
}
