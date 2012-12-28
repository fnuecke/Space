using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
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
            var distance0 = Vector2.Dot(normal, vIn.Item0.Vertex) - offset;
            var distance1 = Vector2.Dot(normal, vIn.Item1.Vertex) - offset;

            // If the points are behind the plane
            if (distance0 <= 0.0f)
            {
                vOut.Item0 = vIn.Item0;
                ++numOut;
                if (distance1 <= 0.0f)
                {
                    vOut.Item1 = vIn.Item1;
                    ++numOut;
                }
            }
            else if (distance1 <= 0.0f)
            {
                vOut.Item0 = vIn.Item1;
                ++numOut;
            }

            // If the points are on different sides of the plane
            if (distance0 * distance1 < 0.0f)
            {
                System.Diagnostics.Debug.Assert(numOut == 1);

                // Find intersection point of edge and plane
                var interp = distance0 / (distance0 - distance1);
                vOut.Item1.Vertex = vIn.Item0.Vertex + interp * (vIn.Item1.Vertex - vIn.Item0.Vertex);

                // VertexA is hitting edgeB.
                vOut.Item1.Id.Feature.IndexA = (byte)vertexIndexA;
                vOut.Item1.Id.Feature.IndexB = vIn.Item0.Id.Feature.IndexB;
                vOut.Item1.Id.Feature.TypeA = (byte)ContactFeature.FeatureType.Vertex;
                vOut.Item1.Id.Feature.TypeB = (byte)ContactFeature.FeatureType.Face;
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
