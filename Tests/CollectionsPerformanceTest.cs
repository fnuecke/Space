using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Collections;
using Engine.ComponentSystem.Systems;
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
        /// The radius of a range query.
        /// </summary>
        private const int QueryRadius = CellSystem.CellSize; // This is the furthest one should ever query, else it leaves the active area.

        // List of values for max entry count to test.
        private static readonly int[] QuadTreeMaxNodeEntries = new[] {30};

        private static void Main(string[] args)
        {
            // Generate data beforehand.
            var random = new MersenneTwister(Seed);
            var data = new List<Tuple<int, Vector2>>();
            for (var i = 0; i < NumberOfObjects; i++)
            {
                data.Add(Tuple.Create(i, random.NextVector(Area)));
            }

            // Wait for application to settle in.
            Console.WriteLine("Press any key to begin measurement.");
            Console.ReadKey(true);

            // Test QuadTree.
            foreach (var maxEntriesPerNode in QuadTreeMaxNodeEntries)
            {
                Console.WriteLine("Running QuadTree test with maximum entries per node = {0}.", maxEntriesPerNode);
                var tree = new QuadTree<int>(maxEntriesPerNode, IndexSystem.MinimumNodeSize);
                Test(tree, data);
            }

            // Test R-Tree.
            {
                Console.WriteLine("Running R-Tree test.");
                var tree = new RTree<int>();
                try
                {
                    Test(tree, data);
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

        private static void Test(IIndex<int> index, List<Tuple<int, Vector2>> data)
        {
            // Get new randomizer.
            var random = new MersenneTwister(Seed);

            // Get stop watch for profiling.
            var watch = new Stopwatch();

            // Also allocate the ids to look up in advance.
            var queries = new List<Vector2>(Operations);

            // And updates.
            var smallUpdates = new List<Tuple<int, Vector2>>(Operations);
            var largeUpdates = new List<Tuple<int, Vector2>>(Operations);

            var addTime = new DoubleSampling(Iterations);
            var queryTime = new DoubleSampling(Iterations);
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
                queries.Clear();
                for (var j = 0; j < Operations; j++)
                {
                    queries.Add(random.NextVector(Area));
                }

                // Generate position updates.
                smallUpdates.Clear();
                largeUpdates.Clear();
                for (var j = 0; j < Operations; j++)
                {
                    // High chance it remains in the same cell.
                    smallUpdates.Add(Tuple.Create(j, data[j].Item2 + random.NextVector(1)));
                }
                for (var j = 0; j < Operations; j++)
                {
                    // High chance it will be outside the original cell.
                    largeUpdates.Add(Tuple.Create(j, random.NextVector(Area)));
                }

                // Test time to add.
                watch.Reset();
                watch.Start();
                foreach (var item in data)
                {
                    index.Add(item.Item2, item.Item1);
                }
                watch.Stop();
                addTime.Put(watch.ElapsedMilliseconds / (double)data.Count);

                // Test look up time.
                watch.Reset();
                watch.Start();
                for (var j = 0; j < Operations; j++)
                {
                    index.RangeQuery(queries[j], QueryRadius, ref DummyCollection<int>.Instance);
                }
                watch.Stop();
                queryTime.Put(watch.ElapsedMilliseconds / (double)Operations);

                // Test update time.
                watch.Reset();
                watch.Start();
                foreach (var update in smallUpdates)
                {
                    index.Update(update.Item2, update.Item1);
                }
                watch.Stop();
                smallUpdateTime.Put(watch.ElapsedMilliseconds / (double)smallUpdates.Count);

                watch.Reset();
                watch.Start();
                foreach (var update in largeUpdates)
                {
                    index.Update(update.Item2, update.Item1);
                }
                watch.Stop();
                largeUpdateTime.Put(watch.ElapsedMilliseconds / (double)largeUpdates.Count);

                // Test removal time.
                watch.Reset();
                watch.Start();
                for (var j = 0; j < NumberOfObjects / 3; j++)
                {
                    index.Remove(data[j].Item1);
                }
                watch.Stop();
                highLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));

                watch.Reset();
                watch.Start();
                for (var j = NumberOfObjects / 3; j < NumberOfObjects * 2 / 3; j++)
                {
                    index.Remove(data[j].Item1);
                }
                watch.Stop();
                mediumLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));

                watch.Reset();
                watch.Start();
                for (var j = NumberOfObjects * 2 / 3; j < NumberOfObjects; j++)
                {
                    index.Remove(data[j].Item1);
                }
                watch.Stop();
                lowLoadRemoveTime.Put(watch.ElapsedMilliseconds / (double)(NumberOfObjects / 3));
            }

            Console.WriteLine("Done!");

            Console.WriteLine("Operation           | Mean      | Std.dev.\n" +
                              "Add:                | {0:0.00000}ms | {1:0.00000}ms\n" +
                              "Query:              | {2:0.00000}ms | {3:0.00000}ms\n" +
                              "Update (small):     | {4:0.00000}ms | {5:0.00000}ms\n" +
                              "Update (large):     | {6:0.00000}ms | {7:0.00000}ms\n" +
                              "Remove (high load): | {8:0.00000}ms | {9:0.00000}ms\n" +
                              "Remove (med. load): | {10:0.00000}ms | {11:0.00000}ms\n" +
                              "Remove (low load):  | {12:0.00000}ms | {13:0.00000}ms",
                              addTime.Mean(), addTime.StandardDeviation(),
                              queryTime.Mean(), queryTime.StandardDeviation(),
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
    }
    
    #endregion
}
