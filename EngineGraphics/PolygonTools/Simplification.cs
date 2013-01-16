using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Engine.Graphics.PolygonTools
{
    /// <summary>Methods for simplifying polygon shapes.</summary>
    public static class Simplification
    {
        /// <summary>Removes all collinear points on the polygon.</summary>
        /// <param name="vertices">The polygon that needs simplification.</param>
        /// <param name="collinearityTolerance">The collinearity tolerance.</param>
        /// <returns>A simplified polygon.</returns>
        public static List<Vector2> CollinearSimplify(List<Vector2> vertices, float collinearityTolerance = 0.01f)
        {
            // We can't simplify polygons under with three or less vertices.
            if (vertices.Count < 3)
            {
                return vertices;
            }

            var simplified = new List<Vector2>();

            var previous = vertices[vertices.Count - 2];
            var current = vertices[vertices.Count - 1];
            foreach (var next in vertices)
            {
                var area = previous.X * (current.Y - next.Y) +
                           current.X * (next.Y - previous.Y) +
                           next.X * (previous.Y - current.Y);
                if (area < -collinearityTolerance || area > collinearityTolerance)
                {
                    simplified.Add(current);
                }

                previous = current;
                current = next;
            }

            return simplified;
        }

        /// <summary>Merges all parallel edges in the list of vertices</summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="tolerance">The tolerance.</param>
        public static void MergeParallelEdges(List<Vector2> vertices, float tolerance)
        {
            if (vertices.Count <= 3)
            {
                return; // Can't do anything useful here to a triangle.
            }

            var mergeMe = new bool[vertices.Count];
            var newNVertices = vertices.Count;

            // Gather points to process.
            for (var i = 0; i < vertices.Count; ++i)
            {
                var lower = (i == 0) ? (vertices.Count - 1) : (i - 1);
                var middle = i;
                var upper = (i == vertices.Count - 1) ? (0) : (i + 1);

                var dx0 = vertices[middle].X - vertices[lower].X;
                var dy0 = vertices[middle].Y - vertices[lower].Y;
                var dx1 = vertices[upper].Y - vertices[middle].X;
                var dy1 = vertices[upper].Y - vertices[middle].Y;

                var norm0 = (float) System.Math.Sqrt(dx0 * dx0 + dy0 * dy0);
                var norm1 = (float) System.Math.Sqrt(dx1 * dx1 + dy1 * dy1);

                if (!(norm0 > 0.0f && norm1 > 0.0f) && newNVertices > 3)
                {
                    // Merge identical points.
                    mergeMe[i] = true;
                    --newNVertices;
                }

                dx0 /= norm0;
                dy0 /= norm0;
                dx1 /= norm1;
                dy1 /= norm1;

                var cross = dx0 * dy1 - dx1 * dy0;
                var dot = dx0 * dx1 + dy0 * dy1;

                if (System.Math.Abs(cross) < tolerance && dot > 0 && newNVertices > 3)
                {
                    mergeMe[i] = true;
                    --newNVertices;
                }
                else
                {
                    mergeMe[i] = false;
                }
            }

            if (newNVertices == vertices.Count || newNVertices == 0)
            {
                return;
            }


            // Copy the vertices to a new list and clear the old.
            var oldVertices = new List<Vector2>(vertices);
            vertices.Clear();

            var index = 0;
            for (var i = 0; i < oldVertices.Count; ++i)
            {
                if (mergeMe[i] || newNVertices == 0 || index == newNVertices)
                {
                    continue;
                }

                System.Diagnostics.Debug.Assert(index < newNVertices);

                vertices.Add(oldVertices[i]);
                ++index;
            }
        }

        /// <summary>Merges the identical points in the polygon.</summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns></returns>
        public static List<Vector2> MergeIdenticalPoints(IEnumerable<Vector2> vertices)
        {
            return new HashSet<Vector2>(vertices).ToList();
        }
    }
}