using System;
using System.Drawing;

namespace RectpackSharp
{
    public struct PackingRectangle : IEquatable<PackingRectangle>, IComparable<PackingRectangle>
    {
        public int Id;

        public uint X;
        public uint Y;
        public uint Width;
        public uint Height;

        internal uint SortKey;

        public uint Right
        {
            get => X + Width;
            set => Width = value - X;
        }

        public uint Bottom
        {
            get => Y + Height;
            set => Height = value - Y;
        }

        public uint Area => Width * Height;

        public uint Perimeter => Width + Width + Height + Height;

        public uint BiggerSide => Math.Max(Width, Height);

        public uint PathologicalMultiplier // => Math.Max(Width, Height) / Math.Min(Width, Height) * Width * Height;
            => (Width > Height ? (Width / Height) : (Height / Width)) * Width * Height;

        public PackingRectangle(uint x, uint y, uint width, uint height, int id = 0)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Id = id;
            SortKey = 0;
        }

        public PackingRectangle(Rectangle rectangle, int id = 0)
            : this((uint)rectangle.X, (uint)rectangle.Y, (uint)rectangle.Width, (uint)rectangle.Height, id)
        {

        }

        public static implicit operator Rectangle(PackingRectangle rectangle)
            => new Rectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height);

        public static implicit operator PackingRectangle(Rectangle rectangle)
            => new PackingRectangle((uint)rectangle.X, (uint)rectangle.Y, (uint)rectangle.Width, (uint)rectangle.Height);

        public static bool operator ==(PackingRectangle left, PackingRectangle right) => left.Equals(right);
        public static bool operator !=(PackingRectangle left, PackingRectangle right) => !left.Equals(right);

        public bool Contains(PackingRectangle other)
        {
            return X <= other.X && Y <= other.Y && Right >= other.Right && Bottom >= other.Bottom;
        }

        public bool Intersects(PackingRectangle other)
        {
            return other.X < X + Width && X < (other.X + other.Width)
                && other.Y < Y + Height && Y < other.Y + other.Height;
        }

        public PackingRectangle Intersection(PackingRectangle other)
        {
            uint x1 = Math.Max(X, other.X);
            uint x2 = Math.Min(Right, other.Right);
            uint y1 = Math.Max(Y, other.Y);
            uint y2 = Math.Min(Bottom, other.Bottom);

            if (x2 >= x1 && y2 >= y1)
                return new PackingRectangle(x1, y1, x2 - x1, y2 - y1);
            return default;
        }

        public override string ToString()
        {
            return string.Concat("{ X=", X.ToString(), ", Y=", Y.ToString(), ", Width=", Width.ToString() + ", Height=", Height.ToString(), ", Id=", Id.ToString(), " }");
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height, Id);
        }

        public bool Equals(PackingRectangle other)
        {
            return X == other.X && Y == other.Y && Width == other.Width
                && Height == other.Height && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is PackingRectangle viewport)
                return Equals(viewport);
            return false;
        }

        public int CompareTo(PackingRectangle other)
        {
            return -SortKey.CompareTo(other.SortKey);
        }
    }
}
