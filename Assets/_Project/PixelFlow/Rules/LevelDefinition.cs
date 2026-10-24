using System;
using System.Collections.Generic;

namespace PixelFlow.Rules
{
    [Serializable]
    public sealed class LevelDefinition
    {
        public int SourceNumber;
        public string Title;
        public int Width;
        public int Height;
        public int SlotCount = 5;
        public int ConveyorLimit = 5;
        public string SourceHash;
        public float TimeLimit;
        public string Difficulty = "Easy";
        public List<PixelDefinition> Pixels = new();
        public List<QueueDefinition> Queues = new();
        public List<LinkDefinition> Links = new();
        public List<BoardObjectDefinition> Objects = new();
        public List<SlotCageDefinition> SlotCages = new();
        public List<ConveyorDoorDefinition> ConveyorDoors = new();

        public void Validate()
        {
            if (Width < 1 || Height < 1 || Width > 256 || Height > 256)
                throw new ArgumentException("Board dimensions must be 1..256.");
            if (SlotCount < 1 || SlotCount > 8 || ConveyorLimit < 1 || ConveyorLimit > 8)
                throw new ArgumentException("Slot and conveyor capacities must be 1..8.");
            if (Pixels == null || Pixels.Count == 0 && (Objects == null || !Objects.Exists(obj => obj != null && obj.Goal)) || Queues == null || Queues.Count < 1 || Queues.Count > 5 || !float.IsFinite(TimeLimit) || TimeLimit < 0)
                throw new ArgumentException("A level needs pixel or object goals and 1..5 shooter queues.");
            var positions = new HashSet<int>();
            var colors = new Dictionary<int, int>();
            foreach (var pixel in Pixels)
            {
                if (pixel == null || pixel.X < 0 || pixel.X + pixel.SpanX > Width || pixel.Y < 0 || pixel.Y + pixel.SpanY > Height || pixel.Color < 0 || pixel.Health < 1 || pixel.SpanX < 1 || pixel.SpanY < 1)
                    throw new ArgumentException("Pixel outside the board or invalid color.");
                for (int y = pixel.Y; y < pixel.Y + pixel.SpanY; y++)
                for (int x = pixel.X; x < pixel.X + pixel.SpanX; x++)
                    if (!positions.Add(y * Width + x)) throw new ArgumentException("Overlapping pixel footprint.");
                colors[pixel.Color] = colors.GetValueOrDefault(pixel.Color) + pixel.Health;
            }
            var ids = new HashSet<int>();
            var ammo = new Dictionary<int, int>();
            foreach (var queue in Queues)
            {
                if (queue?.Shooters == null) throw new ArgumentException("Missing shooter queue.");
                foreach (var shooter in queue.Shooters)
                {
                    if (shooter == null || !ids.Add(shooter.Id) || shooter.Ammo < (shooter.Kind == ShooterKind.Normal ? 1 : 0) || shooter.Color < 0 || shooter.Ice < 0 || shooter.Counter < 0)
                        throw new ArgumentException("Invalid or duplicate shooter.");
                    ammo[shooter.Color] = ammo.GetValueOrDefault(shooter.Color) + shooter.Ammo;
                }
            }
            Links ??= new List<LinkDefinition>(); Objects ??= new List<BoardObjectDefinition>();
            SlotCages ??= new List<SlotCageDefinition>(); ConveyorDoors ??= new List<ConveyorDoorDefinition>();
            var objectIds = new HashSet<int>();
            foreach (var obj in Objects)
            {
                if (obj == null || !objectIds.Add(obj.Id) || obj.Cells == null || obj.Cells.Count == 0 || !Enum.IsDefined(typeof(BoardObjectKind), obj.Kind))
                    throw new ArgumentException("Invalid board object.");
                foreach (var cell in obj.Cells)
                    if (cell.X < 0 || cell.Y < 0 || cell.X >= Width || cell.Y >= Height) throw new ArgumentException("Object footprint outside the board.");
                if (obj.Layers == null || obj.Layers.Count == 0) throw new ArgumentException("Object needs a layer/counter.");
                foreach (var layer in obj.Layers)
                    if (layer.Health < 1 || layer.Color < -1) throw new ArgumentException("Invalid object layer.");
                if (obj.Release != null && (!ids.Add(obj.Release.Id) || obj.Release.Ammo < 1 || obj.Release.Color < 0)) throw new ArgumentException("Invalid caged shooter.");
            }
            foreach (var link in Links)
            {
                if (link.Members == null || link.Members.Count < 2) throw new ArgumentException("A link needs two members.");
                var members = new HashSet<int>();
                foreach (int id in link.Members) if (!ids.Contains(id) || !members.Add(id)) throw new ArgumentException("Missing or repeated link member.");
            }
            var slots = new HashSet<int>();
            foreach (var cage in SlotCages)
                if (cage.Slot < 0 || cage.Slot >= SlotCount || cage.Health < 1 || cage.Color < 0 || !slots.Add(cage.Slot)) throw new ArgumentException("Invalid slot cage.");
            foreach (var door in ConveyorDoors)
                if (!float.IsFinite(door.Progress) || door.Progress < 0 || door.Progress >= 1 || door.Health < 1 || door.Color < 0) throw new ArgumentException("Invalid conveyor door.");
            // Complex levels contain support placeholders, captive ammo and layered
            // targets. Do not validate those with the plain-pixel ammo shortcut.
            if (Objects.Count == 0)
                foreach (var pair in colors)
                    if (ammo.GetValueOrDefault(pair.Key) < pair.Value) throw new ArgumentException("Insufficient ammunition for a board color.");
        }
    }

    public enum ShooterKind { Normal, Lock, Hammer, Totem, Mallet }
    public enum BoardObjectKind { Key, Gate, PixelPipe, Snake, Wall, EggBox, Ice, Wood, ColorDoor, Biscuit, Split, Ufo, Pumpkin, Curtain, ShooterCage, MultiSnake, BeanBox, MusicToy, Crossbow, Accordion, ColoredCage, Matryoshka, Spaceship, Mixer }
    public enum BoosterKind { Tray, Hand, Shuffle, Color, Unicorn }
    [Serializable] public sealed class Cell { public int X, Y; public Cell() { } public Cell(int x, int y) { X = x; Y = y; } }
    [Serializable] public sealed class PixelDefinition { public int X; public int Y; public int Color; public int Health = 1; public int SpanX = 1, SpanY = 1; public bool Hidden; }
    [Serializable] public sealed class ShooterDefinition { public int Id; public int Ammo; public int Color; public ShooterKind Kind; public bool Surprise, Astronaut; public int Ice, Counter, FeatureColor = -1, PipeId = -1; }
    [Serializable] public sealed class QueueDefinition { public List<ShooterDefinition> Shooters = new(); }
    [Serializable] public sealed class LinkDefinition { public int Id; public bool Chain; public List<int> Members = new(); }
    [Serializable] public sealed class ColorHealth { public int Color = -1, Health = 1; public ColorHealth() { } public ColorHealth(int color, int health) { Color = color; Health = health; } }
    [Serializable] public sealed class BoardObjectDefinition
    {
        public int Id; public BoardObjectKind Kind; public bool Goal;
        public List<Cell> Cells = new(); public List<ColorHealth> Layers = new();
        public ShooterDefinition Release; public int Direction, Count, FeatureColor = -1;
    }
    [Serializable] public sealed class SlotCageDefinition { public int Slot, Color, Health; }
    [Serializable] public sealed class ConveyorDoorDefinition { public float Progress; public int Color, Health; }
}
