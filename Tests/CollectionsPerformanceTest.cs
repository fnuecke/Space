﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Collections;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Systems;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
#if FARMATH
using Engine.FarCollections;
using Engine.FarMath;
using TPoint = Engine.FarMath.FarPosition;
using TSingle = Engine.FarMath.FarValue;
using TRectangle = Engine.FarMath.FarRectangle;
#else
using TPoint = Microsoft.Xna.Framework.Vector2;
using TSingle = System.Single;
using TRectangle = Engine.Math.RectangleF;
#endif

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
        private const int MinQueryRange = (MaxQueryRange >> 8) + 1;

        // List of values for max entry count to test.
        private static readonly int[] QuadTreeMaxNodeEntries = new[] {30};

        private static void Main()
        {
            // Generate data beforehand.
            var random = new MersenneTwister(Seed);

            Console.WriteLine("Used area is {0}x{0}.", Area);
            Console.WriteLine("Number of objects is {0}.", NumberOfObjects);
            Console.WriteLine("Number of operations is {0}.", Operations);

            var points = new List<Tuple<int, TPoint>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                points.Add(Tuple.Create(i, random.NextVector(Area)));
            }

            Console.WriteLine("Minimum node size for index structures is {0}.", MinimumNodeSize);
            Console.WriteLine("Rectangle sizes: {0}-{1}, {2}-{3}, {4}-{5}.", MinimumNodeSize >> 2,
                MinimumNodeSize >> 1, MinimumNodeSize, MinimumNodeSize << 1,
                MinimumNodeSize << 2, MinimumNodeSize << 3);

            var smallRectangles = new List<Tuple<int, TRectangle>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                smallRectangles.Add(Tuple.Create(i, random.NextRectangle(Area, MinimumNodeSize >> 2, MinimumNodeSize >> 1)));
            }
            var mediumRectangles = new List<Tuple<int, TRectangle>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                mediumRectangles.Add(Tuple.Create(i, random.NextRectangle(Area, MinimumNodeSize, MinimumNodeSize << 1)));
            }
            var largeRectangles = new List<Tuple<int, TRectangle>>();
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
                //var tree = new SpatialHashedQuadTree<int>(maxEntriesPerNode, MinimumNodeSize);
#if FARMATH
                //var tree = new Engine.FarCollections.QuadTree<int>(maxEntriesPerNode, MinimumNodeSize);
                var tree = new Engine.FarCollections.SpatialHashedQuadTree<int>(maxEntriesPerNode, MinimumNodeSize);
#else
                //var tree = new Engine.Collections.QuadTree<int>(maxEntriesPerNode, MinimumNodeSize);
                var tree = new Engine.Collections.SpatialHashedQuadTree<int>(maxEntriesPerNode, MinimumNodeSize);
#endif
                Test(tree, points, smallRectangles, mediumRectangles, largeRectangles);
            }

            // Test SpatialHash.
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
                //Console.WriteLine("Running R-Tree test.");
                //var tree = new RTree<int>();
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

            // Wait for key press to close, to allow reading results.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

        private delegate void BuildSmallUpdates<T>(IList<Tuple<int, T>> list, IList<Tuple<int, T>> data, IUniformRandom random, int i);

        private delegate void BuildLargeUpdates<T>(IList<Tuple<int, T>> list, IList<Tuple<int, T>> data, IUniformRandom random, int i);

        private delegate void AddEntries<T>(IIndex<int, TRectangle, TPoint> index, IList<Tuple<int, T>> data);

        private delegate void DoUpdate<T>(IIndex<int, TRectangle, TPoint> index, Tuple<int, T> update);

        private static void Test(IIndex<int, TRectangle, TPoint> index, IList<Tuple<int, TPoint>> points,
            IList<Tuple<int, TRectangle>> smallRectangles,
            IList<Tuple<int, TRectangle>> mediumRectangles,
            IList<Tuple<int, TRectangle>> largeRectangles)
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

        private static void RunPoints(IIndex<int, TRectangle, TPoint> index, IList<Tuple<int, TPoint>> points)
        {
            Run(index,
                points,
                (list, data, random, i) => list.Add(Tuple.Create(i, data[i].Item2 + random.NextVector(0.05f))),
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

        private static void RunRectangles(IIndex<int, TRectangle, TPoint> index, IList<Tuple<int, TRectangle>> rectangles)
        {
            Run(index,
                rectangles,
                (list, data, random, i) =>
                {
                    var rect = data[i].Item2;
                    rect.X += (float)random.NextDouble(-0.05, 0.05);
                    rect.Y += (float)random.NextDouble(-0.05, 0.05);
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
                    index.Update(rect, Vector2.Zero, update.Item1);
                }
                );
        }

        private static void Run<T>(IIndex<int, TRectangle, TPoint> index, IList<Tuple<int, T>> data,
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
            var rangeQueries = new List<Tuple<TPoint, float>>(Operations);
            var areaQueries = new List<TRectangle>(Operations);

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
                try
                {
                    watch.Reset();
                    watch.Start();
                    addEntries(index, data);
                    watch.Stop();
                    addTime.Put(watch.ElapsedMilliseconds / (double)NumberOfObjects);
                }
                catch (NotSupportedException)
                {
                }

                // Test update time.
                try
                {
                    watch.Reset();
                    watch.Start();
                    foreach (var update in smallUpdates)
                    {
                        doUpdate(index, update);
                    }
                    watch.Stop();
                    smallUpdateTime.Put(watch.ElapsedMilliseconds / (double)smallUpdates.Count);
                }
                catch (NotSupportedException)
                {
                }

                try
                {
                    watch.Reset();
                    watch.Start();
                    foreach (var update in largeUpdates)
                    {
                        doUpdate(index, update);
                    }
                    watch.Stop();
                    largeUpdateTime.Put(watch.ElapsedMilliseconds / (double)largeUpdates.Count);
                }
                catch (NotSupportedException)
                {
                }

                // Test look up time.
                try
                {
                    watch.Reset();
                    watch.Start();
                    for (var j = 0; j < Operations; j++)
                    {
#if USE_CALLBACK
                        index.Find(rangeQueries[j].Item1, rangeQueries[j].Item2, value => true);
#else
                        index.Find(rangeQueries[j].Item1, rangeQueries[j].Item2, ref DummyCollection<int>.Instance);
#endif
                    }
                    watch.Stop();
                    rangeQueryTime.Put(watch.ElapsedMilliseconds / (double)Operations);
                }
                catch (NotSupportedException)
                {
                }

                try
                {
                    watch.Reset();
                    watch.Start();
                    for (var j = 0; j < Operations; j++)
                    {
                        var rect = areaQueries[j];
#if USE_CALLBACK
                        index.Find(rect, value => true);
#else
                        index.Find(rect, ref DummyCollection<int>.Instance);
#endif
                    }
                    watch.Stop();
                    areaQueryTime.Put(watch.ElapsedMilliseconds / (double)Operations);
                }
                catch (NotSupportedException)
                {
                }

                // Test removal time.
                try
                {
                    watch.Reset();
                    watch.Start();
                    for (var j = 0; j < NumberOfObjects / 3; j++)
                    {
                        index.Remove(data[j].Item1);
                    }
                    watch.Stop();
                    highLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));
                }
                catch (NotSupportedException)
                {
                }

                try
                {
                    watch.Reset();
                    watch.Start();
                    for (var j = NumberOfObjects / 3; j < NumberOfObjects * 2 / 3; j++)
                    {
                        index.Remove(data[j].Item1);
                    }
                    watch.Stop();
                    mediumLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));
                }
                catch (NotSupportedException)
                {
                }

                try
                {
                    watch.Reset();
                    watch.Start();
                    for (var j = NumberOfObjects * 2 / 3; j < NumberOfObjects; j++)
                    {
                        index.Remove(data[j].Item1);
                    }
                    watch.Stop();
                    lowLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));
                }
                catch (NotSupportedException)
                {
                }
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
    internal sealed class DummyCollection<T> : ISet<T>
    {
        public static ISet<T> Instance = new DummyCollection<T>();

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

        /// <summary>
        /// Modifies the current set so that it contains all elements that are present in both the current set and in the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public void UnionWith(IEnumerable<T> other)
        {
        }

        /// <summary>
        /// Modifies the current set so that it contains only elements that are also in a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public void IntersectWith(IEnumerable<T> other)
        {
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current set.
        /// </summary>
        /// <param name="other">The collection of items to remove from the set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public void ExceptWith(IEnumerable<T> other)
        {
        }

        /// <summary>
        /// Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both. 
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
        }

        /// <summary>
        /// Determines whether a set is a subset of a specified collection.
        /// </summary>
        /// <returns>
        /// true if the current set is a subset of <paramref name="other"/>; otherwise, false.
        /// </returns>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection.
        /// </summary>
        /// <returns>
        /// true if the current set is a superset of <paramref name="other"/>; otherwise, false.
        /// </returns>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the current set is a correct superset of a specified collection.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.ISet`1"/> object is a correct superset of <paramref name="other"/>; otherwise, false.
        /// </returns>
        /// <param name="other">The collection to compare to the current set. </param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the current set is a property (strict) subset of a specified collection.
        /// </summary>
        /// <returns>
        /// true if the current set is a correct subset of <paramref name="other"/>; otherwise, false.
        /// </returns>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the current set overlaps with the specified collection.
        /// </summary>
        /// <returns>
        /// true if the current set and <paramref name="other"/> share at least one common element; otherwise, false.
        /// </returns>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public bool Overlaps(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <returns>
        /// true if the current set is equal to <paramref name="other"/>; otherwise, false.
        /// </returns>
        /// <param name="other">The collection to compare to the current set.</param><exception cref="T:System.ArgumentNullException"><paramref name="other"/> is null.</exception>
        public bool SetEquals(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Adds an element to the current set and returns a value to indicate if the element was successfully added. 
        /// </summary>
        /// <returns>
        /// true if the element is added to the set; false if the element is already in the set.
        /// </returns>
        /// <param name="item">The element to add to the set.</param>
        bool ISet<T>.Add(T item)
        {
            return false;
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
#if FARMATH
        public static FarPosition NextVector(this IUniformRandom random, float area)
        {
            return new FarPosition((float)(random.NextDouble() * area - area / 2.0),
                                   (float)(random.NextDouble() * area - area / 2.0));
        }

        public static FarRectangle NextRectangle(this IUniformRandom random, float area, float minSize, float maxSize)
        {
            var rect = new FarRectangle
            {
                Width = (float)random.NextDouble(minSize, maxSize),
                Height = (float)random.NextDouble(minSize, maxSize)
            };
            rect.X = (float)random.NextDouble(-area / 2, area / 2 - (int)rect.Width);
            rect.Y = (float)random.NextDouble(-area / 2, area / 2 - (int)rect.Height);
            return rect;
        }
#else
        public static Vector2 NextVector(this IUniformRandom random, float area)
        {
            return new Vector2((float)(random.NextDouble() * area - area / 2.0),
                               (float)(random.NextDouble() * area - area / 2.0));
        }

        public static RectangleF NextRectangle(this IUniformRandom random, float area, float minSize, float maxSize)
        {
            var rect = new RectangleF
            {
                Width = (float)random.NextDouble(minSize, maxSize),
                Height = (float)random.NextDouble(minSize, maxSize)
            };
            rect.X = (float)random.NextDouble(-area / 2, area / 2 - rect.Width);
            rect.Y = (float)random.NextDouble(-area / 2, area / 2 - rect.Height);
            return rect;
        }

        //public static Rectangle NextRectangle(this IUniformRandom random, int area, int minSize, int maxSize)
        //{
        //    var rect = new Rectangle
        //               {
        //                   Width = random.NextInt32(minSize, maxSize),
        //                   Height = random.NextInt32(minSize, maxSize)
        //               };
        //    rect.X = random.NextInt32(-area / 2, area / 2 - rect.Width);
        //    rect.Y = random.NextInt32(-area / 2, area / 2 - rect.Height);
        //    return rect;
        //}
#endif
    }
    
    #endregion
}
