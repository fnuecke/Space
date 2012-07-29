using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Collections;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Systems;

namespace Tests
{
    internal static class CollectionsPerformanceTest
    {
        /// <summary>
        /// The seed value to use for the randomizer.
        /// </summary>
        private const uint Seed = 1337;

        /// <summary>
        /// The number of points to generate.
        /// </summary>
        private const int NumberOfObjects = 90000; // 10k per active cell.

        /// <summary>
        /// How many iterations to run for each configuration to average over.
        /// </summary>
        private const int Iterations = 20;

        /// <summary>
        /// Number of runs per operation to perform to average over. This is
        /// used to minimize impact from outside influences. Note that the
        /// parameters for the operations need to be precomputed, though, so
        /// a high value here will result in higher memory consumption.
        /// </summary>
        private const int Operations = 8000;

        /// <summary>
        /// The area over which to distribute the points.
        /// </summary>
        private const int Area = CellSystem.CellSize * 3; // Normally active area.

        /// <summary>
        /// Minimum bounds size to allow for nodes before stopping to split.
        /// </summary>
        private const int MinimumNodeSize = 64;

        /// <summary>
        /// The maximum radius of a range query, and half the maximum
        /// extent of an area query.
        /// </summary>
        private const int MaxQueryRange = CellSystem.CellSize; // This is the furthest one should ever query, else it leaves the active area.

        /// <summary>
        /// The minimum query range, and half the minimum extent of an
        /// area query.
        /// </summary>
        private const int MinQueryRange = (CellSystem.CellSize >> 8) + 1;

        // List of values for max entry count to test.
        private static readonly int[] QuadTreeMaxNodeEntries = new[] {30};

        private static void Main()
        {
            // Generate data beforehand.
            var random = new MersenneTwister(Seed);

            Console.WriteLine("Used area is {0}x{0}.", Area);
            Console.WriteLine("Number of objects is {0}.", NumberOfObjects);
            Console.WriteLine("Number of operations is {0}.", Operations);

            var points = new List<Tuple<int, Vector2>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                points.Add(Tuple.Create(i, random.NextVector(Area)));
            }

            Console.WriteLine("Minimum node size for index structures is {0}.", MinimumNodeSize);
            Console.WriteLine("Rectangle sizes: {0}-{1}, {2}-{3}, {4}-{5}.", MinimumNodeSize >> 2,
                MinimumNodeSize >> 1, MinimumNodeSize, MinimumNodeSize << 1,
                MinimumNodeSize << 2, MinimumNodeSize << 3);

            var smallRectangles = new List<Tuple<int, Rectangle>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                smallRectangles.Add(Tuple.Create(i, random.NextRectangle(Area, MinimumNodeSize >> 2, MinimumNodeSize >> 1)));
            }
            var mediumRectangles = new List<Tuple<int, Rectangle>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                mediumRectangles.Add(Tuple.Create(i, random.NextRectangle(Area, MinimumNodeSize, MinimumNodeSize << 1)));
            }
            var largeRectangles = new List<Tuple<int, Rectangle>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                largeRectangles.Add(Tuple.Create(i, random.NextRectangle(Area, MinimumNodeSize << 2, MinimumNodeSize << 3)));
            }

            // Wait for application to settle in.
            Console.WriteLine("Press any key to begin measurement.");
            Console.ReadKey(true);

            // Test Michael Coyle's QuadTree.
            {
                //Console.WriteLine("Running Michael Coyle's QuadTree test.");
                //var tree = new MCQuadTree<int>(new Rectangle(-Area / 2, -Area / 2, Area, Area));
                //Test(tree, points, smallRectangles, mediumRectangles, largeRectangles);
            }

            // Test John McDonald's QuadTree.
            {
                //Console.WriteLine("Running John McDonald's QuadTree test.");
                //var tree = new JMDQuadTree<int>(-Area / 2, -Area / 2, Area, Area);
                //Test(tree, points, smallRectangles, mediumRectangles, largeRectangles);
            }

            // Test MicroSoft's QuadTree.
            {
                //Console.WriteLine("Running MicroSoft's QuadTree test.");
                //var tree = new MSQuadTree<int> {Bounds = new Rectangle(-Area / 2, -Area / 2, Area, Area)};
                //Test(tree, points, smallRectangles, mediumRectangles, largeRectangles);
            }

            // Test QuadTree.
            foreach (var maxEntriesPerNode in QuadTreeMaxNodeEntries)
            {
                Console.WriteLine("Running QuadTree test with maximum entries per node = {0}.", maxEntriesPerNode);
                var tree = new QuadTree<int>(maxEntriesPerNode, MinimumNodeSize);
                Test(tree, points, smallRectangles, mediumRectangles, largeRectangles);
            }

            // Test SpatialHash.
            // -- Disabled because it's so bloody slow.
            {
                //Console.WriteLine("Running SpatialHash test.");
                //var tree = new SpatialHash<int>(MinimumNodeSize * 64);
                //try
                //{
                //    Test(tree, points, smallRectangles, mediumRectangles, largeRectangles);
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine("Error!");
                //    Console.WriteLine(ex.Message);
                //    Console.WriteLine(ex.StackTrace);
                //}
            }

            // Test R-Tree.
            {
                Console.WriteLine("Running R-Tree test.");
                var tree = new RTree<int>();
                try
                {
                    Test(tree, points, smallRectangles, mediumRectangles, largeRectangles);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error!");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

            // Wait for key press to close, to allow reading results.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

        private delegate void BuildSmallUpdates<T>(IList<Tuple<int, T>> list, IList<Tuple<int, T>> data, IUniformRandom random, int i);

        private delegate void BuildLargeUpdates<T>(IList<Tuple<int, T>> list, IList<Tuple<int, T>> data, IUniformRandom random, int i);

        private delegate void AddEntries<T>(IIndex<int> index, IList<Tuple<int, T>> data);

        private delegate void DoUpdate<T>(IIndex<int> index, Tuple<int, T> update);

        private static void Test(IIndex<int> index, IList<Tuple<int, Vector2>> points,
            IList<Tuple<int, Rectangle>> smallRectangles,
            IList<Tuple<int, Rectangle>> mediumRectangles,
            IList<Tuple<int, Rectangle>> largeRectangles)
        {
            Console.WriteLine("Testing with point data...");
            {
                try
                {
                    RunPoints(index, points);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Points not supported.");
                }
            }

            Console.WriteLine("Testing with small rectangle data...");
            {
                RunRectangles(index, smallRectangles);
            }

            Console.WriteLine("Testing with medium rectangle data...");
            {
                RunRectangles(index, mediumRectangles);
            }

            Console.WriteLine("Testing with large rectangle data...");
            {
                RunRectangles(index, largeRectangles);
            }
        }

        private static void RunPoints(IIndex<int> index, IList<Tuple<int, Vector2>> points)
        {
            Run(index,
                points,
                (list, data, random, i) => list.Add(Tuple.Create(i, data[i].Item2 + random.NextVector(2))),
                (list, data, random, i) => list.Add(Tuple.Create(i, random.NextVector(Area))),
                (ints, data) =>
                {
                    foreach (var item in data)
                    {
                        index.Add(item.Item2, item.Item1);
                    }
                },
                (ints, update) => index.Update(update.Item2, update.Item1)
                );
        }

        private static void RunRectangles(IIndex<int> index, IList<Tuple<int, Rectangle>> rectangles)
        {
            Run(index,
                rectangles,
                (list, data, random, i) =>
                {
                    var rect = data[i].Item2;
                    rect.X += random.NextInt32(-2, 2);
                    rect.Y += random.NextInt32(-2, 2);
                    list.Add(Tuple.Create(i, rect));
                },
                (list, data, random, i) => list.Add(Tuple.Create(i, random.NextRectangle(Area, 16, 1024))),
                (ints, data) =>
                {
                    foreach (var item in data)
                    {
                        var rect = item.Item2;
                        index.Add(rect, item.Item1);
                    }
                },
                (ints, update) =>
                {
                    var rect = update.Item2;
                    index.Update(rect, update.Item1);
                }
                );
        }

        private static void Run<T>(IIndex<int> index, IList<Tuple<int, T>> data,
            BuildSmallUpdates<T> makeSmallUpdate,
            BuildLargeUpdates<T> makeLargeUpdate,
            AddEntries<T> addEntries,
            DoUpdate<T> doUpdate)
        {
            // Get new randomizer.
            var random = new MersenneTwister(Seed);

            // Get stop watch for profiling.
            var watch = new Stopwatch();

            // Also allocate the ids to look up in advance.
            var rangeQueries = new List<Tuple<Vector2, float>>(Operations);
            var areaQueries = new List<Rectangle>(Operations);

            // And updates.
            var smallUpdates = new List<Tuple<int, T>>(Operations);
            var largeUpdates = new List<Tuple<int, T>>(Operations);

            var addTime = new DoubleSampling(Iterations);
            var rangeQueryTime = new DoubleSampling(Iterations);
            var areaQueryTime = new DoubleSampling(Iterations);
            var smallUpdateTime = new DoubleSampling(Iterations);
            var largeUpdateTime = new DoubleSampling(Iterations);
            var highLoadRemoveTime = new DoubleSampling(Iterations);
            var mediumLoadRemoveTime = new DoubleSampling(Iterations);
            var lowLoadRemoveTime = new DoubleSampling(Iterations);

            Console.Write("Doing {0} iterations... ", Iterations);

            for (var i = 0; i < Iterations; i++)
            {
                Console.Write("{0}. ", i + 1);

                // Clear the index.
                index.Clear();

                // Generate look up ids in advance.
                rangeQueries.Clear();
                for (var j = 0; j < Operations; j++)
                {
                    rangeQueries.Add(Tuple.Create(random.NextVector(Area), MinQueryRange + (MaxQueryRange - MinQueryRange) * (float)random.NextDouble()));
                }
                areaQueries.Clear();
                for (var j = 0; j < Operations; j++)
                {
                    areaQueries.Add(random.NextRectangle(Area, MinQueryRange * 2, MaxQueryRange * 2));
                }

                // Generate position updates.
                smallUpdates.Clear();
                largeUpdates.Clear();
                for (var j = 0; j < Operations; j++)
                {
                    // High chance it remains in the same cell.
                    makeSmallUpdate(smallUpdates, data, random, j);
                }
                for (var j = 0; j < Operations; j++)
                {
                    // High chance it will be outside the original cell.
                    makeLargeUpdate(largeUpdates, data, random, j);
                }

                // Test time to add.
                watch.Reset();
                try
                {
                    watch.Start();
                    addEntries(index, data);
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                addTime.Put(watch.ElapsedMilliseconds / (double)NumberOfObjects);

                // Test update time.
                watch.Reset();
                try
                {
                    watch.Start();
                    foreach (var update in smallUpdates)
                    {
                        doUpdate(index, update);
                    }
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                smallUpdateTime.Put(watch.ElapsedMilliseconds / (double)smallUpdates.Count);

                watch.Reset();
                try
                {
                    watch.Start();
                    foreach (var update in largeUpdates)
                    {
                        doUpdate(index, update);
                    }
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                largeUpdateTime.Put(watch.ElapsedMilliseconds / (double)largeUpdates.Count);

                // Test look up time.
                watch.Reset();
                try
                {
                    watch.Start();
                    for (var j = 0; j < Operations; j++)
                    {
                        index.Find(rangeQueries[j].Item1, rangeQueries[j].Item2, ref DummyCollection<int>.Instance);
                    }
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                rangeQueryTime.Put(watch.ElapsedMilliseconds / (double)Operations);

                watch.Reset();
                try
                {
                    watch.Start();
                    for (var j = 0; j < Operations; j++)
                    {
                        var rect = areaQueries[j];
                        index.Find(ref rect, ref DummyCollection<int>.Instance);
                    }
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                areaQueryTime.Put(watch.ElapsedMilliseconds / (double)Operations);

                // Test removal time.
                watch.Reset();
                try
                {
                    watch.Start();
                    for (var j = 0; j < NumberOfObjects / 3; j++)
                    {
                        index.Remove(data[j].Item1);
                    }
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                highLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));

                watch.Reset();
                try
                {
                    watch.Start();
                    for (var j = NumberOfObjects / 3; j < NumberOfObjects * 2 / 3; j++)
                    {
                        index.Remove(data[j].Item1);
                    }
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                mediumLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));

                watch.Reset();
                try
                {
                    watch.Start();
                    for (var j = NumberOfObjects * 2 / 3; j < NumberOfObjects; j++)
                    {
                        index.Remove(data[j].Item1);
                    }
                }
                catch (NotSupportedException)
                {
                }
                watch.Stop();
                lowLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));
            }

            Console.WriteLine("Done!");

            Console.WriteLine("Operation           | Mean      | Std.dev.\n" +
                              "Add:                | {0:0.00000}ms | {1:0.00000}ms\n" +
                              "Range query:        | {2:0.00000}ms | {3:0.00000}ms\n" +
                              "Area query:         | {4:0.00000}ms | {5:0.00000}ms\n" +
                              "Update (small):     | {6:0.00000}ms | {7:0.00000}ms\n" +
                              "Update (large):     | {8:0.00000}ms | {9:0.00000}ms\n" +
                              "Remove (high load): | {10:0.00000}ms | {11:0.00000}ms\n" +
                              "Remove (med. load): | {12:0.00000}ms | {13:0.00000}ms\n" +
                              "Remove (low load):  | {14:0.00000}ms | {15:0.00000}ms",
                              addTime.Mean(), addTime.StandardDeviation(),
                              rangeQueryTime.Mean(), rangeQueryTime.StandardDeviation(),
                              areaQueryTime.Mean(), areaQueryTime.StandardDeviation(),
                              smallUpdateTime.Mean(), smallUpdateTime.StandardDeviation(),
                              largeUpdateTime.Mean(), largeUpdateTime.StandardDeviation(),
                              highLoadRemoveTime.Mean(), highLoadRemoveTime.StandardDeviation(),
                              mediumLoadRemoveTime.Mean(), mediumLoadRemoveTime.StandardDeviation(),
                              lowLoadRemoveTime.Mean(), lowLoadRemoveTime.StandardDeviation());
        }
    }

    #region Utilities

    /// <summary>
    /// Dummy collection used for result gathering, to ignore overhead from used collections.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class DummyCollection<T> : ICollection<T>
    {
        public static ICollection<T> Instance = new DummyCollection<T>();

        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
        }

        public void Clear()
        {
        }

        public bool Contains(T item)
        {
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
        }

        public bool Remove(T item)
        {
            return false;
        }

        public int Count
        {
            get { return  0; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Random number generator extension for vector generation.
    /// </summary>
    internal static class Extensions
    {
        public static Vector2 NextVector(this IUniformRandom random, int area)
        {
            return new Vector2((float)(random.NextDouble() * area - area / 2.0),
                               (float)(random.NextDouble() * area - area / 2.0));
        }

        public static Rectangle NextRectangle(this IUniformRandom random, int area, int minSize, int maxSize)
        {
            var rect = new Rectangle
                       {
                           Width = random.NextInt32(minSize, maxSize),
                           Height = random.NextInt32(minSize, maxSize)
                       };
            rect.X = random.NextInt32(-area / 2, area / 2 - rect.Width);
            rect.Y = random.NextInt32(-area / 2, area / 2 - rect.Height);
            return rect;
        }
    }
    
    #endregion
}
