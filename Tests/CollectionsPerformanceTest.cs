using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Collections;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Systems;

namespace Tests
{
    class CollectionsPerformanceTest
    {
        /// <summary>
        /// The seed value to use for the randomizer.
        /// </summary>
        private const uint Seed = 1337;

        /// <summary>
        /// The number of points to generate.
        /// </summary>
        private const int NumberOfObjects = 75000;

        /// <summary>
        /// The area over which to distribute the points.
        /// </summary>
        private const int Area = CellSystem.CellSize * 3; // Normally active area.

        /// <summary>
        /// How many iterations to run for each configuration to average over.
        /// </summary>
        private const int Iterations = 5;

        // Number of lookups to perform per iteration.
        private const int Queries = 2000;

        /// <summary>
        /// The radius of a range query.
        /// </summary>
        private const int QueryRadius = CellSystem.CellSize;

        // Number of updates to perform per iteration.
        private const int Updates = 2000;

        // Number of objects to remove per iteration.
        private const int Removals = 2000;

        // List of values for max entry count to test.
        private static readonly int[] QuadTreeMaxNodeEntries = new[] { 29, 30, 31 };

        static void Main(string[] args)
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

            // Test R-Tree.
            {
                Console.WriteLine("Running R-Tree test.");
                var tree = new RTree<int>();
                Test(tree, data);
            }

            // Test QuadTree.
            foreach (var maxEntriesPerNode in QuadTreeMaxNodeEntries)
            {
                Console.WriteLine("Running QuadTree test with maximum entries per node = {0}.", maxEntriesPerNode);
                var tree = new QuadTree<int>(maxEntriesPerNode, IndexSystem.MinimumNodeSize);
                Test(tree, data);
            }

            // Wait for key press to close, to allow reading results.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

        static void Test(IIndex<int> index, List<Tuple<int, Vector2>> data)
        {
            // Get new randomizer.
            var random = new MersenneTwister(Seed);

            // Get stop watch for profiling.
            var watch = new Stopwatch();

            // Preallocate a list that's definitely large enough, to avoid having that
            // allocation impact the measurement.
            var results = new List<List<int>>(Queries);
            for (var i = 0; i < Queries; i++)
            {
                results.Add(new List<int>(NumberOfObjects));
            }

            // Also allocate the ids to look up in advance.
            var queries = new List<Vector2>();

            // And updates.
            var updates = new List<Tuple<int, Vector2>>();

            // As well as removals.
            var removals = new List<int>();
            
            double addTime = 0;
            double queryTime = 0;
            double updateTime = 0;
            double removeTime = 0;

            Console.Write("Doing {0} iterations... ", Iterations);
            for (int i = 0; i < Iterations; i++)
            {
                Console.Write("{0}. ", i + 1);

                // Clear the index.
                index.Clear();

                // Test time to add.
                watch.Reset();
                watch.Start();
                foreach (var item in data)
                {
                    index.Add(item.Item2, item.Item1);
                }
                watch.Stop();
                addTime += watch.ElapsedMilliseconds / (double)index.Count;

                // Generate look up ids in advance.
                queries.Clear();
                for (var j = 0; j < Queries; j++)
                {
                    queries.Add(random.NextVector(Area));
                }

                // Test look up time.
                watch.Reset();
                watch.Start();
                for (var j = 0; j < Queries; j++)
                {
                    var v = queries[j];
                    // Use different result list so we can clear afterwards.
                    index.RangeQuery(ref v, QueryRadius, results[j]);
                }
                watch.Stop();
                queryTime += watch.ElapsedMilliseconds / (double)queries.Count;

                // Clear results.
                for (int j = 0; j < Queries; j++)
                {
                    results[j].Clear();
                }

                // Generate position updates.
                updates.Clear();
                for (int j = 0; j < Updates; j++)
                {
                    updates.Add(Tuple.Create(random.NextInt32(NumberOfObjects), random.NextVector(Area)));
                }

                // Test update time.
                watch.Reset();
                watch.Start();
                foreach (var update in updates)
                {
                    index.Update(update.Item2, update.Item1);
                }
                watch.Stop();
                updateTime += watch.ElapsedMilliseconds / (double)Updates;

                // Generate removals in advance.
                removals.Clear();
                // No duplicates here.
                while (removals.Count < Removals)
                {
                    var n = random.NextInt32(NumberOfObjects);
                    if (!removals.Contains(n))
                    {
                        removals.Add(n);
                    }
                }

                // Test removal time.
                watch.Reset();
                watch.Start();
                foreach (var removal in removals)
                {
                    index.Remove(removal);
                }
                watch.Stop();
                removeTime += watch.ElapsedMilliseconds / (double)Removals;
            }
            
            Console.WriteLine("done!");

            addTime /= Iterations;
            queryTime /= Iterations;
            updateTime /= Iterations;
            removeTime /= Iterations;

            Console.WriteLine("Add: {0:0.00000}ms\nQuery: {1:0.00000}ms\nUpdate: {2:0.00000}ms\nRemove: {3:0.00000}ms", addTime, queryTime, updateTime, removeTime);
        }
    }

    static class Extensions
    {
        public static Vector2 NextVector(this IUniformRandom random, int area)
        {
            return new Vector2((float)(random.NextDouble() * area - area / 2.0),
                               (float)(random.NextDouble() * area - area / 2.0));
        }
    }
}
