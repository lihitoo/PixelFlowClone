using System;
using System.Collections.Generic;

namespace PixelFlow.Rules
{
    public sealed class BoardObjectState
    {
        public BoardObjectDefinition Definition { get; internal set; }
        public bool Active { get; internal set; } = true;
        public int Layer { get; internal set; }
        public int Health { get; internal set; }
        public bool[] Seats { get; internal set; }
        internal readonly Dictionary<int, int> CellOrder = new();
        public int Color => Definition.Layers[Math.Min(Layer, Definition.Layers.Count - 1)].Color;
        public int Remaining
        {
            get
            {
                if (!Active) return 0;
                if (Definition.Kind == BoardObjectKind.Ufo) { int count = 0; foreach (bool seat in Seats) if (!seat) count++; return count; }
                int result = Health; for (int i = Layer + 1; i < Definition.Layers.Count; i++) result += Definition.Layers[i].Health;
                return result;
            }
        }
        public int Total { get { int count = 0; foreach (var layer in Definition.Layers) count += layer.Health; return count; } }
        public float Fraction => Total > 0 ? (float)Remaining / Total : 0;
        public bool Covers(int index)
        {
            if (!Active || !CellOrder.TryGetValue(index, out int order)) return false;
            if (Definition.Kind is BoardObjectKind.Snake or BoardObjectKind.MultiSnake or BoardObjectKind.Split or BoardObjectKind.Accordion or BoardObjectKind.Mixer)
                return order < Math.Max(1, (int)Math.Ceiling(Definition.Cells.Count * Fraction));
            return true;
        }
    }

    public sealed partial class PuzzleSession
    {
        private readonly List<BoardObjectState> objects = new();
        private List<BoardObjectState>[] objectsAt;
        private int[] slotHealth, doorHealth;
        public IReadOnlyList<BoardObjectState> BoardObjects => objects;
        public event Action<string> FeatureChanged;

        private void InitializeObjects()
        {
            objectsAt = new List<BoardObjectState>[pixels.Length];
            foreach (var definition in Level.Objects)
            {
                var state = new BoardObjectState { Definition = definition, Health = definition.Layers[0].Health, Seats = new bool[definition.Layers.Count] };
                int order = 0;
                foreach (var cell in definition.Cells)
                {
                    int index = cell.Y * Level.Width + cell.X;
                    if (!state.CellOrder.TryAdd(index, order)) continue;
                    order++; objectsAt[index] ??= new List<BoardObjectState>(); objectsAt[index].Add(state);
                }
                if (definition.Goal) InitialHealth += state.Total;
                objects.Add(state);
                if (definition.Release != null)
                {
                    int column = Math.Clamp(definition.Cells[0].X * ColumnCount / Level.Width, 0, ColumnCount - 1);
                    var release = CreateShooter(definition.Release, column, ShooterLocation.Stored);
                    shooters.Add(release.Id, release);
                }
            }
            slotHealth = new int[Level.SlotCount];
            foreach (var cage in Level.SlotCages) slotHealth[cage.Slot] = cage.Health;
            doorHealth = new int[Level.ConveyorDoors.Count];
            for (int i = 0; i < doorHealth.Length; i++) doorHealth[i] = Level.ConveyorDoors[i].Health;
        }

        private BoardObjectState BlockingObject(int index, BoardObjectState except = null)
        {
            if (objectsAt[index] == null) return null;
            for (int i = objectsAt[index].Count - 1; i >= 0; i--)
            {
                var state = objectsAt[index][i];
                if (state == except || !state.Covers(index) || state.Definition.Kind is BoardObjectKind.Key or BoardObjectKind.ShooterCage) continue;
                return state;
            }
            return null;
        }

        private bool TryHit(ShooterState shooter, BoardSide side, int lane)
        {
            if (!FindTarget(side, lane, out int pixel, out var obj)) return false;
            int color = obj != null ? obj.Color : pixels[pixel];
            if (color >= 0 && color != shooter.Color) return false;
            shooter.Ammo--;
            ApplyHit(shooter.Id, pixel, obj, shooter.Color);
            return true;
        }

        private bool FindTarget(BoardSide side, int lane, out int pixel, out BoardObjectState obj)
        {
            pixel = -1; obj = null;
            int count = side is BoardSide.Left or BoardSide.Right ? Level.Width : Level.Height;
            for (int depth = 0; depth < count; depth++)
            {
                int x = side switch { BoardSide.Left => depth, BoardSide.Right => Level.Width - depth - 1, _ => lane };
                int y = side switch { BoardSide.Bottom => depth, BoardSide.Top => Level.Height - depth - 1, _ => lane };
                int index = y * Level.Width + x;
                var blocker = BlockingObject(index);
                if (blocker != null)
                {
                    if (!blocker.Definition.Goal) return false;
                    obj = blocker; pixel = index; return true;
                }
                int owner = pixelOwner[index];
                if (owner >= 0 && pixels[owner] >= 0) { pixel = owner; return true; }
            }
            return false;
        }

        private void ApplyHit(int shooterId, int index, BoardObjectState obj, int color)
        {
            bool destroyed;
            if (obj == null)
            {
                revealedPixels[index] = true; pixelHealth[index]--; RemainingHealth--;
                destroyed = pixelHealth[index] == 0;
                if (destroyed) { pixels[index] = -1; RemainingPixels--; }
            }
            else
            {
                RemainingHealth--; AdvanceLayer(obj, 1); destroyed = !obj.Active;
            }
            HitCount++; OnSuccessfulHit(color); UpdateExposedObjects();
            Shot?.Invoke(new ShotRecord(shooterId, index % Level.Width, index / Level.Width, color, destroyed, obj?.Definition.Id ?? -1));
            CheckWin();
        }

        private void AdvanceLayer(BoardObjectState state, int damage)
        {
            while (damage > 0 && state.Active)
            {
                int amount = Math.Min(damage, state.Health); state.Health -= amount; damage -= amount;
                if (state.Health > 0) break;
                state.Layer++;
                if (state.Layer < state.Definition.Layers.Count) state.Health = state.Definition.Layers[state.Layer].Health;
                else ClearObject(state);
            }
        }

        private void ClearObject(BoardObjectState state)
        {
            if (!state.Active) return;
            state.Active = false; state.Health = 0;
            if (state.Definition.Release != null)
            {
                var shooter = shooters[state.Definition.Release.Id];
                if (shooter.Location == ShooterLocation.Stored)
                { shooter.Location = ShooterLocation.Queue; shooter.Revealed = true; queues[shooter.Column].Add(shooter); }
            }
            FeatureChanged?.Invoke(state.Definition.Kind + " cleared");
        }

        private void OnSuccessfulHit(int color)
        {
            foreach (var shooter in shooters.Values) if (shooter.Ice > 0) shooter.Ice--;
            foreach (var cage in Level.SlotCages)
                if (cage.Color == color && slotHealth[cage.Slot] > 0) slotHealth[cage.Slot]--;
            foreach (var state in objects)
            {
                if (!state.Active || state.Definition.Goal) continue;
                bool damage = state.Definition.Kind switch
                {
                    BoardObjectKind.Ice or BoardObjectKind.Biscuit or BoardObjectKind.Split => true,
                    BoardObjectKind.ColorDoor or BoardObjectKind.Curtain or BoardObjectKind.Accordion or BoardObjectKind.Mixer => state.Color < 0 || state.Color == color,
                    _ => false
                };
                if (damage) AdvanceLayer(state, 1);
            }
        }

        private bool ObjectExposed(BoardObjectState state)
        {
            foreach (var cell in state.Definition.Cells)
            {
                for (int side = 0; side < 4; side++)
                {
                    bool clear = true;
                    int x = side == 0 ? 0 : side == 1 ? Level.Width - 1 : cell.X;
                    int y = side == 2 ? 0 : side == 3 ? Level.Height - 1 : cell.Y;
                    int dx = side == 0 ? 1 : side == 1 ? -1 : 0;
                    int dy = side == 2 ? 1 : side == 3 ? -1 : 0;
                    while (x != cell.X || y != cell.Y)
                    {
                        int index = y * Level.Width + x;
                        if (!state.CellOrder.ContainsKey(index) && (PixelAt(x, y) >= 0 || BlockingObject(index, state) != null)) { clear = false; break; }
                        x += dx; y += dy;
                    }
                    if (clear && BlockingObject(cell.Y * Level.Width + cell.X, state) == null) return true;
                }
            }
            return false;
        }

        private void UpdateExposedObjects()
        {
            foreach (var state in objects)
            {
                if (!state.Active || state.Definition.Kind is not (BoardObjectKind.Key or BoardObjectKind.ShooterCage) || !ObjectExposed(state)) continue;
                if (state.Definition.Kind == BoardObjectKind.Key)
                {
                    ShooterState locked = null;
                    foreach (var queue in queues)
                        foreach (var shooter in queue)
                            if (shooter.Kind == ShooterKind.Lock && (locked == null || QueueOffset(shooter.Id) < QueueOffset(locked.Id))) locked = shooter;
                    if (locked != null) RemoveSupport(locked);
                }
                ClearObject(state);
            }
            RevealFronts();
        }

        private void RemoveSupport(ShooterState shooter)
        {
            if (shooter.Location == ShooterLocation.Queue) queues[shooter.Column].Remove(shooter);
            shooter.Location = ShooterLocation.Finished; shooter.Ammo = 0;
        }

        private bool ActivateSupport(ShooterState shooter)
        {
            if (shooter.Kind == ShooterKind.Hammer)
            {
                foreach (var state in objects) if (state.Active && state.Definition.Kind == BoardObjectKind.Wood) AdvanceLayer(state, 1);
                FeatureChanged?.Invoke("Hammer strike");
            }
            else
            {
                BoardObjectState target = null;
                foreach (var state in objects)
                    if (state.Active && state.Definition.Kind == BoardObjectKind.MusicToy && state.Color == shooter.FeatureColor) { target = state; break; }
                if (target == null)
                {
                    bool any = false; foreach (var state in objects) if (state.Active && state.Definition.Kind == BoardObjectKind.MusicToy) any = true;
                    if (any) return false;
                }
                else AdvanceLayer(target, 1);
                FeatureChanged?.Invoke("Mallet strike");
            }
            RemoveSupport(shooter); UpdateExposedObjects(); return true;
        }

        private void OnShooterFinished(int color)
        {
            foreach (var state in shooters.Values)
                if (state.Kind == ShooterKind.Totem && state.Location == ShooterLocation.Queue && state.Counter > 0)
                {
                    state.Counter--;
                    if (state.Counter == 0) { RemoveSupport(state); FeatureChanged?.Invoke("Totem cleared"); }
                }
            foreach (var state in objects)
            {
                if (!state.Active || state.Definition.Kind != BoardObjectKind.Ufo) continue;
                for (int i = 0; i < state.Seats.Length; i++)
                    if (!state.Seats[i] && state.Definition.Layers[i].Color == color) { state.Seats[i] = true; break; }
                bool full = true; foreach (bool seat in state.Seats) if (!seat) full = false;
                if (full) ClearObject(state);
            }
            RevealFronts(); UpdateExposedObjects();
        }

        private void OpenSpaceship(int color)
        {
            foreach (var state in objects)
                if (state.Active && state.Definition.Kind == BoardObjectKind.Spaceship && state.Color == color) { ClearObject(state); return; }
        }

        private bool CrossConveyorDoor(ShooterState shooter, float before)
        {
            for (int i = 0; i < doorHealth.Length; i++)
            {
                var door = Level.ConveyorDoors[i];
                if (doorHealth[i] <= 0 || before >= door.Progress || shooter.Progress < door.Progress) continue;
                if (shooter.Color == door.Color) doorHealth[i]--;
                else { shooter.Progress = 1; FeatureChanged?.Invoke("Conveyor door returned a different color"); return true; }
            }
            return false;
        }
        public int DoorHealth(int index) => doorHealth[index];
    }
}
