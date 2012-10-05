using Microsoft.Xna.Framework;

namespace FarseerPhysics.Common.PolygonManipulation
{
    public static class SimplifyTools
    {
        /// <summary>
        /// Removes all collinear points on the polygon.
        /// </summary>
        /// <param name="vertices">The polygon that needs simplification.</param>
        /// <param name="collinearityTolerance">The collinearity tolerance.</param>
        /// <returns>A simplified polygon.</returns>
        public static Vertices CollinearSimplify(Vertices vertices, float collinearityTolerance)
        {
            //We can't simplify polygons under 3 vertices
            if (vertices.Count < 3)
                return vertices;

            Vertices simplified = new Vertices();

            for (int i = 0; i < vertices.Count; i++)
            {
                int prevId = vertices.PreviousIndex(i);
                int nextId = vertices.NextIndex(i);

                Vector2 prev = vertices[prevId];
                Vector2 current = vertices[i];
                Vector2 next = vertices[nextId];

                //If they collinear, continue
                if (MathUtils.Collinear(ref prev, ref current, ref next, collinearityTolerance))
                    continue;

                simplified.Add(current);
            }

            return simplified;
        }

        /// <summary>
        /// Removes all collinear points on the polygon.
        /// Has a default bias of 0
        /// </summary>
        /// <param name="vertices">The polygon that needs simplification.</param>
        /// <returns>A simplified polygon.</returns>
        public static Vertices CollinearSimplify(Vertices vertices)
        {
            return CollinearSimplify(vertices, 0);
        }
    }
}