using System;
using System.Collections.Generic;

namespace RectpackSharp
{
    /// <summary>
    /// A static class providing functionality for packing rectangles into a bin as small as possible.
    /// </summary>
    public static class RectanglePacker
    {
        /// <summary>A weak reference to the last list used, so it can be reused in subsequent packs.</summary>
        private static WeakReference<List<PackingRectangle>> oldListReference;

        /// <summary>
        /// Finds a way to pack all the given rectangles into a single bin. Performance can be traded for
        /// space efficiency by using the optional parameters.
        /// </summary>
        /// <param name="rectangles">The rectangles to pack. The result is saved onto this array.</param>
        /// <param name="bounds">The bounds of the resulting bin. This will always be at X=Y=0.</param>
        /// <param name="packingHint">Specifies hints for optimizing performance.</param>
        /// <param name="acceptableDensity">Searching stops once a bin is found with this density (usedArea/totalArea) or better.</param>
        /// <param name="stepSize">The amount by which to increment/decrement size when trying to pack another bin.</param>
        public static void Pack(PackingRectangle[] rectangles, out PackingRectangle bounds,
            PackingHint packingHint = PackingHint.FindBest, float acceptableDensity = 1, uint stepSize = 1)
        {
            if (rectangles == null)
                throw new ArgumentNullException(nameof(rectangles));

            if (stepSize == 0)
                throw new ArgumentOutOfRangeException(nameof(stepSize), stepSize, nameof(stepSize) + " must be greater than 0.");

            bounds = default;
            if (rectangles.Length == 0)
                return;

            // We separate the value in packingHint into the different options it specifies.
            Span<PackingHint> hints = stackalloc PackingHint[PackingHintExtensions.MaxHintCount];
            PackingHintExtensions.GetFlagsFrom(packingHint, ref hints);

            if (hints.Length == 0)
                throw new ArgumentException("No valid packing hints specified.", nameof(packingHint));

            // We calculate the initial bin size we'll try, alongisde the sum of the areas of the rectangles.
            uint binSize = CalculateInitialBinSize(rectangles, out uint totalArea);

            // We turn the acceptableDensity parameter into an acceptableArea value, so we can
            // compare the area directly rather than having to calculate the density.
            acceptableDensity = Math.Clamp(acceptableDensity, 0.1f, 1);
            uint acceptableArea = (uint)Math.Ceiling(totalArea / acceptableDensity);

            // We get a list that will be used by the packing algorithm.
            List<PackingRectangle> emptySpaces = GetList(rectangles.Length * 2);

            uint currentBestArea = uint.MaxValue;
            PackingRectangle[] currentBest = rectangles;
            PackingRectangle[] tmpBest = new PackingRectangle[rectangles.Length];
            PackingRectangle[] tmpArray = new PackingRectangle[rectangles.Length];

            for (int i = 0; i < hints.Length && currentBestArea > acceptableArea; i++)
            {
                currentBest.CopyTo(tmpBest, 0);
                PackingHintExtensions.SortByPackingHint(tmpBest, hints[i]);
                if (FindBestBin(emptySpaces, tmpBest, tmpArray, binSize, stepSize, currentBestArea == uint.MaxValue))
                {
                    PackingRectangle boundsTmp = FindBounds(tmpBest);
                    uint areaTmp = boundsTmp.Area;
                    if (areaTmp < currentBestArea)
                    {
                        bounds = boundsTmp;
                        currentBestArea = areaTmp;
                        PackingRectangle[] swaptmp = tmpBest;
                        tmpBest = currentBest;
                        currentBest = swaptmp;
                        binSize = bounds.BiggerSide;
                    }
                }
            }

            if (currentBest != rectangles)
                currentBest.CopyTo(rectangles, 0);

            // We return the list so it can be used in subsequent pack operations.
            ReturnList(emptySpaces);
        }

        private static bool FindBestBin(List<PackingRectangle> emptySpaces, Span<PackingRectangle> rectangles,
            Span<PackingRectangle> tmpArray, uint binSize, uint stepSize, bool allowGrow)
        {
            if (TryPackAsOrdered(emptySpaces, rectangles, rectangles, binSize, binSize))
            {
                binSize -= stepSize;
                Span<PackingRectangle> from = rectangles, to = tmpArray;
                while (TryPackAsOrdered(emptySpaces, from, to, binSize, binSize))
                {
                    binSize -= stepSize;
                    Span<PackingRectangle> swaptmp = from;
                    from = to;
                    to = swaptmp;
                }

                if (from != rectangles)
                    from.CopyTo(rectangles);
                return true;
            }

            if (allowGrow)
            {
                while (!TryPackAsOrdered(emptySpaces, rectangles, rectangles, binSize, binSize))
                    binSize += stepSize;
                return true;
            }

            return false;
        }

        private static uint CalculateInitialBinSize(ReadOnlySpan<PackingRectangle> rectangles, out uint totalArea)
        {
            totalArea = CalculateTotalArea(rectangles);
            return (uint)Math.Ceiling(Math.Sqrt(totalArea) * 1.03);
        }

        private static bool TryPackAsOrdered(List<PackingRectangle> emptySpaces, Span<PackingRectangle> unpacked,
            Span<PackingRectangle> packed, uint binWidth, uint binHeight)
        {
            emptySpaces.Clear();
            emptySpaces.Add(new PackingRectangle(0, 0, binWidth, binHeight));

            for (int r = 0; r < unpacked.Length; r++)
            {
                if (!TryFindBestSpace(unpacked[r], emptySpaces, out int spaceIndex))
                    return false;

                PackingRectangle oldSpace = emptySpaces[spaceIndex];
                packed[r] = unpacked[r];
                packed[r].X = oldSpace.X;
                packed[r].Y = oldSpace.Y;

                uint freeWidth = oldSpace.Width - packed[r].Width;
                uint freeHeight = oldSpace.Height - packed[r].Height;

                if (freeWidth != 0 && freeHeight != 0)
                {
                    emptySpaces.RemoveAt(spaceIndex);
                    // Both freeWidth and freeHeight are different from 0. We need to split the
                    // empty space into two (plus the image).
                    if (freeWidth > freeHeight)
                    {
                        emptySpaces.AddSorted(new PackingRectangle(packed[r].Right, oldSpace.Y, freeWidth, oldSpace.Height));
                        emptySpaces.AddSorted(new PackingRectangle(oldSpace.X, packed[r].Bottom, packed[r].Width, freeHeight));
                    }
                    else
                    {
                        emptySpaces.AddSorted(new PackingRectangle(oldSpace.X, packed[r].Bottom, oldSpace.Width, freeHeight));
                        emptySpaces.AddSorted(new PackingRectangle(packed[r].Right, oldSpace.Y, freeWidth, packed[r].Height));
                    }
                }
                else if (freeWidth == 0)
                {
                    oldSpace.Y += packed[r].Height;
                    oldSpace.Height = freeHeight;
                    emptySpaces[spaceIndex] = oldSpace;
                    EnsureSorted(emptySpaces, spaceIndex);
                    //emptySpaces.RemoveAt(spaceIndex);
                    //emptySpaces.Add(new PackingRectangle(oldSpace.X, oldSpace.Y + packed[r].Height, oldSpace.Width, freeHeight));
                }
                else if (freeHeight == 0)
                {
                    oldSpace.X += packed[r].Width;
                    oldSpace.Width = freeWidth;
                    emptySpaces[spaceIndex] = oldSpace;
                    EnsureSorted(emptySpaces, spaceIndex);
                    //emptySpaces.RemoveAt(spaceIndex);
                    //emptySpaces.Add(new PackingRectangle(oldSpace.X + packed[r].Width, oldSpace.Y, freeWidth, oldSpace.Height));
                }
                else
                    emptySpaces.RemoveAt(spaceIndex);
            }

            return true;
        }

        private static bool TryFindBestSpace(in PackingRectangle rectangle, List<PackingRectangle> emptySpaces, out int index)
        {
            for (int i = 0; i < emptySpaces.Count; i++)
                if (rectangle.Width <= emptySpaces[i].Width && rectangle.Height <= emptySpaces[i].Height)
                {
                    index = i;
                    return true;
                }

            index = -1;
            return false;
        }

        /// <summary>
        /// Gets a list of rectangles that can be used for empty spaces.
        /// </summary>
        /// <param name="preferredCapacity">If a list has to be created, this is used as initial capacity.</param>
        private static List<PackingRectangle> GetList(int preferredCapacity)
        {
            if (oldListReference == null)
                return new List<PackingRectangle>(preferredCapacity);

            lock (oldListReference)
            {
                if (oldListReference.TryGetTarget(out List<PackingRectangle> list))
                {
                    oldListReference.SetTarget(null);
                    return list;
                }
                else
                    return new List<PackingRectangle>(preferredCapacity);
            }
        }

        /// <summary>
        /// Returns a list so it can be used in future pack operations. The list should
        /// no longer be used after returned.
        /// </summary>
        private static void ReturnList(List<PackingRectangle> list)
        {
            if (oldListReference == null)
                oldListReference = new WeakReference<List<PackingRectangle>>(list);
            else
            {
                lock (oldListReference)
                {
                    if (!oldListReference.TryGetTarget(out List<PackingRectangle> oldList) || oldList.Capacity < list.Capacity)
                        oldListReference.SetTarget(list);
                }
            }
        }

        private static void AddSorted(this List<PackingRectangle> list, PackingRectangle rectangle)
        {
            rectangle.SortKey = Math.Max(rectangle.X, rectangle.Y);
            int max = list.Count - 1, min = 0;
            int middle, compared;

            while (min <= max)
            {
                middle = (max + min) / 2;
                compared = rectangle.SortKey.CompareTo(list[middle].SortKey);

                if (compared == 0)
                {
                    min = middle + 1;
                    break;
                }

                // If comparison is less than 0, rectangle should be inserted before list[middle].
                // If comparison is greater than 0, rectangle should be after list[middle].
                if (compared < 0)
                    max = middle - 1;
                else
                    min = middle + 1;
            }

            list.Insert(min, rectangle);
        }

        private static void EnsureSorted(List<PackingRectangle> list, int index)
        {
            uint newSortKey = Math.Max(list[index].X, list[index].Y);
            if (newSortKey == list[index].SortKey)
                return;

            int min = index;
            int max = list.Count - 1;
            int middle, compared;
            PackingRectangle rectangle = list[index];
            rectangle.SortKey = newSortKey;

            while (min <= max)
            {
                middle = (max + min) / 2;
                compared = newSortKey.CompareTo(list[middle].SortKey);

                if (compared == 0)
                {
                    min = middle - 1;
                    break;
                }

                // If comparison is less than 0, rectangle should be inserted before list[middle].
                // If comparison is greater than 0, rectangle should be after list[middle].
                if (compared < 0)
                    max = middle - 1;
                else
                    min = middle + 1;
            }
            min = Math.Min(min, list.Count - 1);

            // We have to place the rectangle in the index 'min'.
            for (int i = index; i < min; i++)
                list[i] = list[i + 1];

            list[min] = rectangle;
        }

        /// <summary>
        /// Calculates the sum of the areas of all the given <see cref="PackingRectangle"/>-s.
        /// </summary>
        public static uint CalculateTotalArea(ReadOnlySpan<PackingRectangle> rectangles)
        {
            uint totalArea = 0;
            for (int i = 0; i < rectangles.Length; i++)
                totalArea += rectangles[i].Area;
            return totalArea;
        }

        /// <summary>
        /// Calculates the smallest possible rectangle that contains all the given rectangles.
        /// </summary>
        public static PackingRectangle FindBounds(ReadOnlySpan<PackingRectangle> rectangles)
        {
            PackingRectangle bounds = rectangles[0];
            for (int i = 1; i < rectangles.Length; i++)
            {
                bounds.X = Math.Min(bounds.X, rectangles[i].X);
                bounds.Y = Math.Min(bounds.Y, rectangles[i].Y);
                bounds.Right = Math.Max(bounds.Right, rectangles[i].Right);
                bounds.Bottom = Math.Max(bounds.Bottom, rectangles[i].Bottom);
            }

            return bounds;
        }

        /// <summary>
        /// Returns true if any two different rectangles in the given span intersect.
        /// </summary>
        public static bool AnyIntersects(ReadOnlySpan<PackingRectangle> rectangles)
        {
            for (int i = 0; i < rectangles.Length; i++)
                for (int c = i + 1; c < rectangles.Length; c++)
                    if (rectangles[c].Intersects(rectangles[i]))
                        return true;
            return false;
        }
    }
}
