using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Detail.Collision
{
    internal static partial class Algorithms
    {
        /// Clipping for contact manifolds.
        private static int ClipSegmentToLine(out FixedArray2<ClipVertex> vOut, FixedArray2<ClipVertex> vIn,
                                             Vector2 normal, float offset, int vertexIndexA)
        {
            // Satisfy outs.
            vOut = new FixedArray2<ClipVertex>();

            // Start with no output points
            var numOut = 0;

            // Calculate the distance of end points to the line
            var distance0 = Vector2.Dot(normal, vIn.Item1.Vertex) - offset;
            var distance1 = Vector2.Dot(normal, vIn.Item2.Vertex) - offset;

            // If the points are behind the plane
            if (distance0 <= 0.0f)
            {
                vOut.Item1 = vIn.Item1;
                ++numOut;
                if (distance1 <= 0.0f)
                {
                    vOut.Item2 = vIn.Item2;
                    ++numOut;
                }
            }
            else if (distance1 <= 0.0f)
            {
                vOut.Item1 = vIn.Item2;
                ++numOut;
            }

            // If the points are on different sides of the plane
            if (distance0 * distance1 < 0.0f)
            {
                System.Diagnostics.Debug.Assert(numOut == 1);

                // Find intersection point of edge and plane
                var interp = distance0 / (distance0 - distance1);
                vOut.Item2.Vertex = vIn.Item1.Vertex + interp * (vIn.Item2.Vertex - vIn.Item1.Vertex);

                // VertexA is hitting edgeB.
                vOut.Item2.Id.Feature.IndexA = (byte)vertexIndexA;
                vOut.Item2.Id.Feature.IndexB = vIn.Item1.Id.Feature.IndexB;
                vOut.Item2.Id.Feature.TypeA = (byte)ContactFeature.FeatureType.Vertex;
                vOut.Item2.Id.Feature.TypeB = (byte)ContactFeature.FeatureType.Face;
                ++numOut;
            }

            return numOut;
        }

        /// Used for computing contact manifolds.
        private struct ClipVertex
        {
            public LocalPoint Vertex;

            public ContactID Id;
        }
    }
}
