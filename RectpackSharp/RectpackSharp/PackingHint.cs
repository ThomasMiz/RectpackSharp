using System;

namespace RectpackSharp
{
    [Flags]
    public enum PackingHint
    {
        TryByArea = 1,
        TryByPerimeter = 2,
        TryByBiggerSide = 4,
        TryByWidth = 8,
        TryByHeight = 16,
        TryByPathologicalMultiplier = 32,

        FindBest = TryByArea | TryByPerimeter | TryByBiggerSide | TryByWidth | TryByHeight | TryByPathologicalMultiplier
    }

    internal class PackingHintExtensions
    {
        private delegate uint GetSortKeyDelegate(in PackingRectangle rectangle);

        public const int MaxHintCount = 6;

        public static uint GetArea(in PackingRectangle rectangle) => rectangle.Area;
        public static uint GetPerimeter(in PackingRectangle rectangle) => rectangle.Perimeter;
        public static uint GetBiggerSide(in PackingRectangle rectangle) => rectangle.BiggerSide;
        public static uint GetWidth(in PackingRectangle rectangle) => rectangle.Width;
        public static uint GetHeight(in PackingRectangle rectangle) => rectangle.Height;
        public static uint GetPathologicalMultiplier(in PackingRectangle rectangle) => rectangle.PathologicalMultiplier;

        public static void GetFlagsFrom(PackingHint packingHint, ref Span<PackingHint> span)
        {
            int index = 0;
            if (packingHint.HasFlag(PackingHint.TryByArea))
                span[index++] = PackingHint.TryByArea;
            if (packingHint.HasFlag(PackingHint.TryByPerimeter))
                span[index++] = PackingHint.TryByPerimeter;
            if (packingHint.HasFlag(PackingHint.TryByBiggerSide))
                span[index++] = PackingHint.TryByBiggerSide;
            if (packingHint.HasFlag(PackingHint.TryByWidth))
                span[index++] = PackingHint.TryByWidth;
            if (packingHint.HasFlag(PackingHint.TryByHeight))
                span[index++] = PackingHint.TryByHeight;
            if (packingHint.HasFlag(PackingHint.TryByPathologicalMultiplier))
                span[index++] = PackingHint.TryByPathologicalMultiplier;
            span = span.Slice(0, index);
        }

        public static void SortByPackingHint(PackingRectangle[] rectangles, PackingHint packingHint)
        {
            GetSortKeyDelegate getKeyDelegate = packingHint switch
            {
                PackingHint.TryByArea => GetArea,
                PackingHint.TryByPerimeter => GetPerimeter,
                PackingHint.TryByBiggerSide => GetBiggerSide,
                PackingHint.TryByWidth => GetWidth,
                PackingHint.TryByHeight => GetHeight,
                PackingHint.TryByPathologicalMultiplier => GetPathologicalMultiplier,
                _ => throw new ArgumentException(nameof(packingHint))
            };

            for (int i = 0; i < rectangles.Length; i++)
                rectangles[i].SortKey = getKeyDelegate(rectangles[i]);
            Array.Sort(rectangles);
        }
    }
}
