using Engine.Collections;
using Engine.Physics.Components;
using Engine.Physics.Math;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Collision
{
    /// <summary>
    /// Methods for polygon vs. polygon collisions.
    /// </summary>
    internal static partial class Algorithms
    {
        // Find edge normal of max separation on A - return if separating axis is found
        // Find edge normal of max separation on B - return if separation axis is found
        // Choose reference edge as min(minA, minB)
        // Find incident edge
        // Clip

        // The normal points from 1 to 2
        public static bool CollidePolygons(Fixture fixtureA, WorldTransform transformA,
                                           Fixture fixtureB, WorldTransform transformB,
                                           out Manifold manifold)
        {
            manifold = new Manifold();

            var polygonA = fixtureA as PolygonFixture;
            var polygonB = fixtureB as PolygonFixture;

            System.Diagnostics.Debug.Assert(polygonA != null);
            System.Diagnostics.Debug.Assert(polygonB != null);

            var totalRadius = polygonA.Radius + polygonB.Radius;

            int edgeA;
            var separationA = FindMaxSeparation(out edgeA, polygonA, transformA, polygonB, transformB);
            if (separationA > totalRadius)
            {
                return false;
            }

            int edgeB;
            var separationB = FindMaxSeparation(out edgeB, polygonB, transformB, polygonA, transformA);
            if (separationB > totalRadius)
            {
                return false;
            }

            PolygonFixture polygon1; // reference polygon
            PolygonFixture polygon2; // incident polygon
            WorldTransform transform1, transform2;
            int edge1; // reference edge
            bool flip;
            const float relativeTol = 0.98f;
            const float absoluteTol = 0.001f;

            if (separationB > relativeTol * separationA + absoluteTol)
            {
                polygon1 = polygonB;
                polygon2 = polygonA;
                transform1 = transformB;
                transform2 = transformA;
                edge1 = edgeB;
                manifold.Type = Manifold.ManifoldType.FaceB;
                flip = true;
            }
            else
            {
                polygon1 = polygonA;
                polygon2 = polygonB;
                transform1 = transformA;
                transform2 = transformB;
                edge1 = edgeA;
                manifold.Type = Manifold.ManifoldType.FaceA;
                flip = false;
            }

            // Transformation mapping from the second polygon's frame of reference to
            // first one's. We use this to directly map points around, without getting
            // into the global coordinate system.
            var transform21 = transform1.MulT(transform2);

            // Begin inlined FindIncidentEdge()
            FixedArray2<ClipVertex> incidentEdge;
            {
                System.Diagnostics.Debug.Assert(0 <= edge1 && edge1 < polygon1.Count);

                var normals1 = polygon1.Normals;

                var vertices2 = polygon2.Vertices;
                var normals2 = polygon2.Normals;
                var count2 = polygon2.Count;

                // Get the normal of the reference edge in poly2's frame.
                var normal12 = -transform2.Rotation * (transform1.Rotation * normals1[edge1]);

                // Find the incident edge on poly2 by finding the clip vertices
                // for the incident edge.
                // Get the face whose own normal has the smallest angular
                // difference to the incident normal.
                var edge2 = 0;
                var minDot = float.MaxValue;
                for (var i = 0; i < count2; ++i)
                {
                    var dot = Vector2.Dot(normal12, normals2[i]);
                    if (dot < minDot)
                    {
                        minDot = dot;
                        edge2 = i;
                    }
                }

                // The edge's index coincides with the first vertex used to define
                // that edge, so we can use that and wrap around as necessary for the
                // second one.
                var index21 = edge2;
                var index22 = index21 + 1 < count2 ? index21 + 1 : 0;

                // Get the incident edge as defined by its two vertices, in the first
                // polygon's frame of reference.
                incidentEdge = new FixedArray2<ClipVertex>
                {
                    Item1 = new ClipVertex
                    {
                        //Vertex = xf2.ToGlobal(vertices2[i1]),
                        Vertex = transform21.ToOther(vertices2[index21]),
                        Id =
                        {
                            Feature =
                            {
                                IndexA = (byte)edge1,
                                IndexB = (byte)index21,
                                TypeA = (byte)ContactFeature.FeatureType.Face,
                                TypeB = (byte)ContactFeature.FeatureType.Vertex
                            }
                        }
                    },
                    Item2 = new ClipVertex
                    {
                        //Vertex = xf2.ToGlobal(vertices2[i2]),
                        Vertex = transform21.ToOther(vertices2[index22]),
                        Id =
                        {
                            Feature =
                            {
                                IndexA = (byte)edge1,
                                IndexB = (byte)index22,
                                TypeA = (byte)ContactFeature.FeatureType.Face,
                                TypeB = (byte)ContactFeature.FeatureType.Vertex
                            }
                        }
                    }
                };
            }
            // End inlined FindIncidentEdge()

            var vertices1 = polygon1.Vertices;
            var count1 = polygon1.Count;

            var index11 = edge1;
            var index12 = edge1 + 1 < count1 ? edge1 + 1 : 0;

            var vertex11 = vertices1[index11];
            var vertex12 = vertices1[index12];

            var tangent1 = vertex12 - vertex11;
            tangent1.Normalize();

            var normal1 = Vector2Util.Cross(tangent1, 1);
            var planePoint1 = 0.5f * (vertex11 + vertex12);

            // Face offset.
            var frontOffset = Vector2.Dot(normal1, vertex11);

            // Side offsets, extended by polytope skin thickness.
            var sideOffset1 = -Vector2.Dot(tangent1, vertex11) + totalRadius;
            var sideOffset2 = Vector2.Dot(tangent1, vertex12) + totalRadius;

            // Clip incident edge against extruded edge1 side edges.
            FixedArray2<ClipVertex> clipPoints1, clipPoints2;

            // Clip to box side 1
            var np = ClipSegmentToLine(out clipPoints1, incidentEdge, -tangent1, sideOffset1, index11);

            if (np < 2)
            {
                return false;
            }

            // Clip to negative box side 1
            np = ClipSegmentToLine(out clipPoints2, clipPoints1, tangent1, sideOffset2, index12);

            if (np < 2)
            {
                return false;
            }

            // Now clipPoints2 contains the clipped points.
            manifold.LocalNormal = normal1;
            manifold.LocalPoint = planePoint1;

            var pointCount = 0;
            for (var i = 0; i < 2; ++i)
            {
                //if (Vector2.Dot(normal1g, clipPoints2[i].Vertex) - frontOffset <= totalRadius)
                if (Vector2.Dot(normal1, clipPoints2[i].Vertex) - frontOffset <= totalRadius)
                {
                    var cp = manifold.Points[pointCount];
                    //cp.localPoint = transform2.ToLocal(clipPoints2[i].Vertex);
                    cp.LocalPoint = transform21.FromOther(clipPoints2[i].Vertex);
                    cp.Id = clipPoints2[i].Id;
                    if (flip)
                    {
                        // Swap features
                        var cf = cp.Id.Feature;
                        cp.Id.Feature.IndexA = cf.IndexB;
                        cp.Id.Feature.IndexB = cf.IndexA;
                        cp.Id.Feature.TypeA = cf.TypeB;
                        cp.Id.Feature.TypeB = cf.TypeA;
                    }
                    manifold.Points[pointCount] = cp;
                    ++pointCount;
                }
            }

            manifold.PointCount = pointCount;

            return pointCount > 0;
        }

        // Find the max separation between poly1 and poly2 using edge normals from poly1.
        private static float FindMaxSeparation(out int edgeIndex,
                                               PolygonFixture poly1, WorldTransform xf1,
                                               PolygonFixture poly2, WorldTransform xf2)
        {
            var count1 = poly1.Count;
            var normals1 = poly1.Normals;

            // Vector pointing from the centroid of poly1 to the centroid of poly2.
            var d = (Vector2)(xf2.ToGlobal(poly2.Centroid) - xf1.ToGlobal(poly1.Centroid));
            var dLocal1 = -xf1.Rotation * d;

            // Find edge normal on poly1 that has the largest projection onto d.
            var edge = 0;
            var maxDot = float.MinValue;
            for (var i = 0; i < count1; ++i)
            {
                var dot = Vector2.Dot(normals1[i], dLocal1);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    edge = i;
                }
            }

            // Get the separation for the edge normal.
            var s = EdgeSeparation(edge, poly1, xf1, poly2, xf2);

            // Check the separation for the previous edge normal.
            var prevEdge = edge - 1 >= 0 ? edge - 1 : count1 - 1;
            var sPrev = EdgeSeparation(prevEdge, poly1, xf1, poly2, xf2);

            // Check the separation for the next edge normal.
            var nextEdge = edge + 1 < count1 ? edge + 1 : 0;
            var sNext = EdgeSeparation(nextEdge, poly1, xf1, poly2, xf2);

            // Find the best edge and the search direction.
            int bestEdge;
            float bestSeparation;
            int increment;
            if (sPrev > s && sPrev > sNext)
            {
                increment = -1;
                bestEdge = prevEdge;
                bestSeparation = sPrev;
            }
            else if (sNext > s)
            {
                increment = 1;
                bestEdge = nextEdge;
                bestSeparation = sNext;
            }
            else
            {
                edgeIndex = edge;
                return s;
            }

            // Perform a local search for the best edge normal.
            for (;;)
            {
                if (increment == -1)
                {
                    edge = bestEdge - 1 >= 0 ? bestEdge - 1 : count1 - 1;
                }
                else
                {
                    edge = bestEdge + 1 < count1 ? bestEdge + 1 : 0;
                }

                s = EdgeSeparation(edge, poly1, xf1, poly2, xf2);

                if (s <= bestSeparation)
                {
                    break;
                }

                bestEdge = edge;
                bestSeparation = s;
            }

            edgeIndex = bestEdge;
            return bestSeparation;
        }

        // Find the separation between poly1 and poly2 for a give edge normal on poly1.
        private static float EdgeSeparation(int edge1,
                                            PolygonFixture poly1, WorldTransform xf1,
                                            PolygonFixture poly2, WorldTransform xf2)
        {
            var vertices1 = poly1.Vertices;
            var normals1 = poly1.Normals;

            var count2 = poly2.Count;
            var vertices2 = poly2.Vertices;

            System.Diagnostics.Debug.Assert(0 <= edge1 && edge1 < poly1.Count);

            // Convert normal from poly1's frame into poly2's frame.
            var normal1World = xf1.Rotation * normals1[edge1];
            var normal1 = -xf2.Rotation * normal1World;

            // Find support vertex on poly2 for -normal.
            var index = 0;
            var minDot = float.MaxValue;

            for (var i = 0; i < count2; ++i)
            {
                var dot = Vector2.Dot(vertices2[i], normal1);
                if (dot < minDot)
                {
                    minDot = dot;
                    index = i;
                }
            }

            var v1 = xf1.ToGlobal(vertices1[edge1]);
            var v2 = xf2.ToGlobal(vertices2[index]);
            var separation = Vector2.Dot((Vector2)(v2 - v1), normal1World);
            return separation;
        }
    }
}
