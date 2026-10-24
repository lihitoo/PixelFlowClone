using System;

namespace PixelFlow.Rules
{
    public enum BoardSide { Left, Top, Right, Bottom }

    public readonly struct TrackPoint
    {
        public readonly float X, Z;
        public readonly BoardSide Side;
        public readonly int Lane;
        public TrackPoint(float x, float z, BoardSide side, int lane) { X = x; Z = z; Side = side; Lane = lane; }
    }

    // Shared by targeting and presentation, so a shot's lane follows the visible belt.
    public sealed class TrackGeometry
    {
        public const float CenterZ = 2f;
        public readonly float CellSize, BoardWidth, BoardHeight, HalfWidth = 3.82f, Bottom, Top, Length;
        private readonly int width, height;

        public TrackGeometry(int width, int height)
        {
            this.width = width; this.height = height;
            CellSize = 6.12f / Math.Max(width, height);
            BoardWidth = width * CellSize; BoardHeight = height * CellSize;
            Bottom = CenterZ - BoardHeight / 2 - .75f;
            Top = CenterZ + BoardHeight / 2 + .75f;
            Length = 4 * HalfWidth + 2 * (Top - Bottom);
        }

        public TrackPoint At(float progress)
        {
            float distance = (progress - (float)Math.Floor(progress)) * Length;
            float vertical = Top - Bottom;
            if (distance < vertical)
            {
                float z = Bottom + distance;
                return new TrackPoint(-HalfWidth, z, BoardSide.Left, Lane(z - CenterZ + BoardHeight / 2, height));
            }
            distance -= vertical;
            if (distance < 2 * HalfWidth)
            {
                float x = -HalfWidth + distance;
                return new TrackPoint(x, Top, BoardSide.Top, Lane(x + BoardWidth / 2, width));
            }
            distance -= 2 * HalfWidth;
            if (distance < vertical)
            {
                float z = Top - distance;
                return new TrackPoint(HalfWidth, z, BoardSide.Right, Lane(z - CenterZ + BoardHeight / 2, height));
            }
            distance -= vertical;
            float bottomX = HalfWidth - distance;
            return new TrackPoint(bottomX, Bottom, BoardSide.Bottom, Lane(bottomX + BoardWidth / 2, width));
        }

        private int Lane(float value, int count)
        {
            int lane = (int)Math.Floor(value / CellSize);
            return lane >= 0 && lane < count ? lane : -1;
        }
    }
}
