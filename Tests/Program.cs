using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RectpackSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Tests
{
    class Program
    {
        static readonly Random r = new Random();

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            PackingRectangle[] rectangles = GetRectangles();
            Console.WriteLine("Packing " + rectangles.Length + " rectangles...");

            stopwatch.Restart();
            RectanglePacker.Pack(rectangles, out PackingRectangle bounds);
            stopwatch.Stop();

            Console.WriteLine("Took ~" + stopwatch.Elapsed.TotalMilliseconds.ToString() + "ms");

            if (RectanglePacker.AnyIntersects(rectangles))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Some rectangles intersect!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("No rectangles intersect.");
            }

            Console.ResetColor();

            string filename = GetImageName();
            Console.WriteLine("Saving as " + filename);
            SaveAsImage(rectangles, bounds, filename);
        }

        static PackingRectangle[] GetRectangles()
        {
            /*
            // Generate RectangleAmount randomized rectangles.
            const int RectangleAmount = 2048;
            PackingRectangle[] rectangles = new PackingRectangle[RectangleAmount];
            for (int i = 0; i < rectangles.Length; i++)
                rectangles[i] = new PackingRectangle(0, 0, (uint)r.Next(20, 200), (uint)r.Next(20, 200));*/



            // Generate a list of rectangles of varying sizes, as if simulating a texture atlas
            List<PackingRectangle> list = new List<PackingRectangle>();
            for (int i = r.Next(5); i < 12; i++)
                list.Add(new PackingRectangle(0, 0, 128 * (uint)r.Next(5, 9), 128 * (uint)r.Next(2, 5)));
            for (int i = 0; i < 1024; i++)
            {
                list.Add(new PackingRectangle(0, 0, 64, 64));
                list.Add(new PackingRectangle(0, 0, 32, 64));
                list.Add(new PackingRectangle(0, 0, 64, 32));
            }
            for (int i = 0; i < 196; i++)
                list.Add(new PackingRectangle(0, 0, 4 * (uint)r.Next(4, 11), 4 * (uint)r.Next(4, 11)));
            PackingRectangle[] rectangles = list.ToArray();


            return rectangles;
        }

        static string GetImageName()
        {
            string file = "rectangles.png";

            int num = 1;
            while (File.Exists(file))
            {
                file = string.Concat("rectangles", num.ToString(), ".png");
                num++;
            }

            return file;
        }

        static void SaveAsImage(PackingRectangle[] rectangles, in PackingRectangle bounds, string file)
        {
            using Image<Rgba32> image = new Image<Rgba32>((int)bounds.Width, (int)bounds.Height);
            image.Mutate(x => x.BackgroundColor(Color.Black));

            for (int i = 0; i < rectangles.Length; i++)
            {
                PackingRectangle r = rectangles[i];
                Rgba32 color = FromHue(i / 64f % 1);
                for (int x = 0; x < r.Width; x++)
                    for (int y = 0; y < r.Height; y++)
                        image[x + (int)r.X, y + (int)r.Y] = color;
            }

            image.SaveAsPng(file);
        }

        static Rgba32 FromHue(float hue)
        {
            hue *= 360.0f;

            float h = hue / 60.0f;
            float x = (1.0f - Math.Abs((h % 2.0f) - 1.0f));

            float r, g, b;
            if (h >= 0.0f && h < 1.0f)
            {
                r = 1;
                g = x;
                b = 0.0f;
            }
            else if (h >= 1.0f && h < 2.0f)
            {
                r = x;
                g = 1;
                b = 0.0f;
            }
            else if (h >= 2.0f && h < 3.0f)
            {
                r = 0.0f;
                g = 1;
                b = x;
            }
            else if (h >= 3.0f && h < 4.0f)
            {
                r = 0.0f;
                g = x;
                b = 1;
            }
            else if (h >= 4.0f && h < 5.0f)
            {
                r = x;
                g = 0.0f;
                b = 1;
            }
            else if (h >= 5.0f && h < 6.0f)
            {
                r = 1;
                g = 0.0f;
                b = x;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            return new Rgba32(r, g, b);
        }
    }
}
