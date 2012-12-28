using Engine.Physics.Components;
using Engine.Physics.Detail.Math;
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
    /// <summary>
    /// Methods for edge vs. circle and edge vs. polygon collisions.
    /// </summary>
    internal static partial class Algorithms
    {
        public static bool CollideCircleAndEdge(Fixture fixtureA, WorldTransform xfA,
                                                Fixture fixtureB, WorldTransform xfB,
                                                out Manifold manifold)
        {
            return CollideEdgeAndCircle(fixtureB, xfB, fixtureA, xfA, out manifold);
        }

        // Compute contact points for edge versus circle.
        // This accounts for edge connectivity.
        public static bool CollideEdgeAndCircle(Fixture fixtureA, WorldTransform xfA,
                                                Fixture fixtureB, WorldTransform xfB,
                                                out Manifold manifold)
        {
            manifold = new Manifold();

            var edgeA = fixtureA as EdgeFixture;
            var circleB = fixtureB as CircleFixture;

            System.Diagnostics.Debug.Assert(edgeA != null);
            System.Diagnostics.Debug.Assert(circleB != null);

            // Compute circle in frame of edge
            var q = xfA.ToLocal(xfB.ToGlobal(circleB.Center));

            var a = edgeA.Vertex1;
            var b = edgeA.Vertex2;
            var e = b - a;

            // Barycentric coordinates
            var u = Vector2.Dot(e, b - q);
            var v = Vector2.Dot(e, q - a);

            var radius = edgeA.Radius + circleB.Radius;

            ContactFeature cf;
            cf.IndexB = 0;
            cf.TypeB = (byte)ContactFeature.FeatureType.Vertex;

            // Region A
            if (v <= 0.0f)
            {
                var p = a;
                var d = q - p;
                var dd = Vector2.Dot(d, d);
                if (dd > radius * radius)
                {
                    return false;
                }

                // Is there an edge connected to A?
                if (edgeA.HasVertex0)
                {
                    var a1 = edgeA.Vertex0;
                    var b1 = a;
                    var e1 = b1 - a1;
                    var u1 = Vector2.Dot(e1, b1 - q);

                    // Is the circle in Region AB of the previous edge?
                    if (u1 > 0.0f)
                    {
                        return false;
                    }
                }

                cf.IndexA = 0;
                cf.TypeA = (byte)ContactFeature.FeatureType.Vertex;

                manifold.Type = Manifold.ManifoldType.Circles;
                manifold.LocalPoint = p;
                manifold.LocalNormal = Vector2.Zero;
                manifold.PointCount = 1;
                manifold.Points.Item1.Id.Key = 0;
                manifold.Points.Item1.Id.Feature = cf;
                manifold.Points.Item1.LocalPoint = circleB.Center;

                return true;
            }

            // Region B
            if (u <= 0.0f)
            {
                var p = b;
                var d = q - p;
                var dd = Vector2.Dot(d, d);
                if (dd > radius * radius)
                {
                    return false;
                }

                // Is there an edge connected to B?
                if (edgeA.HasVertex3)
                {
                    var a2 = b;
                    var b2 = edgeA.Vertex3;
                    var e2 = b2 - a2;
                    var v2 = Vector2.Dot(e2, q - a2);

                    // Is the circle in Region AB of the next edge?
                    if (v2 > 0.0f)
                    {
                        return false;
                    }
                }

                cf.IndexA = 1;
                cf.TypeA = (byte)ContactFeature.FeatureType.Vertex;

                manifold.Type = Manifold.ManifoldType.Circles;
                manifold.LocalPoint = p;
                manifold.LocalNormal = Vector2.Zero;
                manifold.PointCount = 1;
                manifold.Points.Item1.Id.Key = 0;
                manifold.Points.Item1.Id.Feature = cf;
                manifold.Points.Item1.LocalPoint = circleB.Center;

                return true;
            }

            // Region AB
            var den = Vector2.Dot(e, e);
            System.Diagnostics.Debug.Assert(den > 0.0f);
            {
                var p = (1.0f / den) * (u * a + v * b);
                var d = q - p;
                var dd = Vector2.Dot(d, d);
                if (dd > radius * radius)
                {
                    return false;
                }
            }

            Vector2 n;
            n.X = -e.Y;
            n.Y = e.X;
            if (Vector2.Dot(n, q - a) < 0.0f)
            {
                n = -n;
            }
            n.Normalize();

            cf.IndexA = 0;
            cf.TypeA = (byte)ContactFeature.FeatureType.Face;

            manifold.Type = Manifold.ManifoldType.FaceA;
            manifold.LocalPoint = a;
            manifold.LocalNormal = n;
            manifold.PointCount = 1;
            manifold.Points.Item1.Id.Key = 0;
            manifold.Points.Item1.Id.Feature = cf;
            manifold.Points.Item1.LocalPoint = circleB.Center;

            return true;
        }

        public static bool CollidePolygonAndEdge(Fixture fixtureA, WorldTransform xfA,
                                                 Fixture fixtureB, WorldTransform xfB,
                                                 out Manifold manifold)
        {
            return CollideEdgeAndPolygon(fixtureB, xfB, fixtureA, xfA, out manifold);
        }

        // This function collides and edge and a polygon.
        // This takes into account edge adjacency.
        // Algorithm:
        // 1. Classify v1 and v2
        // 2. Classify polygon centroid as front or back
        // 3. Flip normal if necessary
        // 4. Initialize normal range to [-pi, pi] about face normal
        // 5. Adjust normal range according to adjacent edges
        // 6. Visit each separating axes, only accept axes within the range
        // 7. Return if _any_ axis indicates separation
        // 8. Clip
        public static bool CollideEdgeAndPolygon(Fixture fixtureA, WorldTransform xfA,
                                                 Fixture fixtureB, WorldTransform xfB,
                                                 out Manifold manifold)
        {
            manifold = new Manifold();

            var edgeA = fixtureA as EdgeFixture;
            var polygonB = fixtureB as PolygonFixture;

            System.Diagnostics.Debug.Assert(edgeA != null);
            System.Diagnostics.Debug.Assert(polygonB != null);

            // This holds polygon B expressed in frame A.
            var tpv = new Vector2[Settings.MaxPolygonVertices];
            var tpn = new Vector2[Settings.MaxPolygonVertices];

            Vector2 normal0 = Vector2.Zero, normal1, normal2 = Vector2.Zero;

            var xf = xfA.MulT(xfB);

            var centroidB = xf.ToOther(polygonB.Centroid);

            var v0 = edgeA.Vertex0;
            var v1 = edgeA.Vertex1;
            var v2 = edgeA.Vertex2;
            var v3 = edgeA.Vertex3;

            var hasVertex0 = edgeA.HasVertex0;
            var hasVertex3 = edgeA.HasVertex3;

            var edge1 = v2 - v1;
            edge1.Normalize();
            normal1.X = edge1.Y;
            normal1.Y = -edge1.X;
            var offset1 = Vector2.Dot(normal1, centroidB - v1);
            var offset0 = 0.0f;
            var offset2 = 0.0f;
            var convex1 = false;
            var convex2 = false;

            // Is there a preceding edge?
            if (hasVertex0)
            {
                var edge0 = v1 - v0;
                edge0.Normalize();
                normal0.X = edge0.Y;
                normal0.Y = -edge0.X;
                convex1 = Vector2Util.Cross(edge0, edge1) >= 0.0f;
                offset0 = Vector2.Dot(normal0, centroidB - v0);
            }

            // Is there a following edge?
            if (hasVertex3)
            {
                var edge2 = v3 - v2;
                edge2.Normalize();
                normal2.X = edge2.Y;
                normal2.Y = -edge2.X;
                convex2 = Vector2Util.Cross(edge1, edge2) > 0.0f;
                offset2 = Vector2.Dot(normal2, centroidB - v2);
            }

            // Determine front or back collision. Determine collision normal limits.
            bool front;
            Vector2 normal, lowerLimit, upperLimit;
            if (hasVertex0 && hasVertex3)
            {
                if (convex1 && convex2)
                {
                    front = offset0 >= 0.0f || offset1 >= 0.0f || offset2 >= 0.0f;
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = normal0;
                        upperLimit = normal2;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = -normal1;
                        upperLimit = -normal1;
                    }
                }
                else if (convex1)
                {
                    front = offset0 >= 0.0f || (offset1 >= 0.0f && offset2 >= 0.0f);
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = normal0;
                        upperLimit = normal1;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = -normal2;
                        upperLimit = -normal1;
                    }
                }
                else if (convex2)
                {
                    front = offset2 >= 0.0f || (offset0 >= 0.0f && offset1 >= 0.0f);
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = normal1;
                        upperLimit = normal2;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = -normal1;
                        upperLimit = -normal0;
                    }
                }
                else
                {
                    front = offset0 >= 0.0f && offset1 >= 0.0f && offset2 >= 0.0f;
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = normal1;
                        upperLimit = normal1;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = -normal2;
                        upperLimit = -normal0;
                    }
                }
            }
            else if (hasVertex0)
            {
                if (convex1)
                {
                    front = offset0 >= 0.0f || offset1 >= 0.0f;
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = normal0;
                        upperLimit = -normal1;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = normal1;
                        upperLimit = -normal1;
                    }
                }
                else
                {
                    front = offset0 >= 0.0f && offset1 >= 0.0f;
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = normal1;
                        upperLimit = -normal1;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = normal1;
                        upperLimit = -normal0;
                    }
                }
            }
            else if (hasVertex3)
            {
                if (convex2)
                {
                    front = offset1 >= 0.0f || offset2 >= 0.0f;
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = -normal1;
                        upperLimit = normal2;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = -normal1;
                        upperLimit = normal1;
                    }
                }
                else
                {
                    front = offset1 >= 0.0f && offset2 >= 0.0f;
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = -normal1;
                        upperLimit = normal1;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = -normal2;
                        upperLimit = normal1;
                    }
                }
            }
            else
            {
                front = offset1 >= 0.0f;
                if (front)
                {
                    normal = normal1;
                    lowerLimit = -normal1;
                    upperLimit = -normal1;
                }
                else
                {
                    normal = -normal1;
                    lowerLimit = normal1;
                    upperLimit = normal1;
                }
            }

            // Get polygonB in frameA.
            var tpc = polygonB.Count;
            for (var i = 0; i < tpc; ++i)
            {
                tpv[i] = xf.ToOther(polygonB.Vertices[i]);
                tpn[i] = xf.Rotation * polygonB.Normals[i];
            }

            const float radius = 2.0f * Settings.PolygonRadius;

            Axis edgeAxis;
            edgeAxis.Type = Axis.AxisType.EdgeA;
            edgeAxis.Index = front ? 0 : 1;
            edgeAxis.Separation = float.MaxValue;

            for (var i = 0; i < tpc; ++i)
            {
                var s = Vector2.Dot(normal, tpv[i] - v1);
                if (s < edgeAxis.Separation)
                {
                    edgeAxis.Separation = s;
                }
            }

            // If no valid normal can be found than this edge should not collide.
            if (edgeAxis.Type == Axis.AxisType.None)
            {
                return false;
            }

            if (edgeAxis.Separation > radius)
            {
                return false;
            }

            Axis polygonAxis;
            polygonAxis.Type = Axis.AxisType.None;
            polygonAxis.Index = -1;
            polygonAxis.Separation = float.MinValue;

            Vector2 perp;
            perp.X = -normal.Y;
            perp.Y = normal.X;

            for (var i = 0; i < tpc; ++i)
            {
                var n = -tpn[i];

                var s1 = Vector2.Dot(n, tpv[i] - v1);
                var s2 = Vector2.Dot(n, tpv[i] - v2);
                var s = System.Math.Min(s1, s2);

                if (s > radius)
                {
                    // No collision
                    polygonAxis.Type = Axis.AxisType.EdgeB;
                    polygonAxis.Index = i;
                    polygonAxis.Separation = s;
                    break;
                }

                // Adjacency
                if (Vector2.Dot(n, perp) >= 0.0f)
                {
                    if (Vector2.Dot(n - upperLimit, normal) < -Settings.AngularSlop)
                    {
                        continue;
                    }
                }
                else
                {
                    if (Vector2.Dot(n - lowerLimit, normal) < -Settings.AngularSlop)
                    {
                        continue;
                    }
                }

                if (s > polygonAxis.Separation)
                {
                    polygonAxis.Type = Axis.AxisType.EdgeB;
                    polygonAxis.Index = i;
                    polygonAxis.Separation = s;
                }
            }

            if (polygonAxis.Type != Axis.AxisType.None && polygonAxis.Separation > radius)
            {
                return false;
            }

            // Use hysteresis for jitter reduction.
            const float relativeTol = 0.98f;
            const float absoluteTol = 0.001f;

            Axis primaryAxis;
            if (polygonAxis.Type == Axis.AxisType.None)
            {
                primaryAxis = edgeAxis;
            }
            else if (polygonAxis.Separation > relativeTol * edgeAxis.Separation + absoluteTol)
            {
                primaryAxis = polygonAxis;
            }
            else
            {
                primaryAxis = edgeAxis;
            }

            FixedArray2<ClipVertex> incidentEdge;

            // Reference face used for clipping
            int rfi1, rfi2;
            Vector2 rfv1, rfv2;
            Vector2 rfnormal;
            Vector2 rfsideNormal1;

            if (primaryAxis.Type == Axis.AxisType.EdgeA)
            {
                manifold.Type = Manifold.ManifoldType.FaceA;

                // Search for the polygon normal that is most anti-parallel to the edge normal.
                var bestIndex = 0;
                var bestValue = Vector2.Dot(normal, tpn[0]);
                for (var i = 1; i < tpc; ++i)
                {
                    var value = Vector2.Dot(normal, tpn[i]);
                    if (value < bestValue)
                    {
                        bestValue = value;
                        bestIndex = i;
                    }
                }

                var i1 = bestIndex;
                var i2 = i1 + 1 < tpc ? i1 + 1 : 0;

                incidentEdge.Item1 = new ClipVertex
                {
                    Vertex = tpv[i1],
                    Id =
                    {
                        Feature =
                        {
                            IndexA = 0,
                            IndexB = (byte)i1,
                            TypeA = (byte)ContactFeature.FeatureType.Face,
                            TypeB = (byte)ContactFeature.FeatureType.Vertex
                        }
                    }
                };

                incidentEdge.Item2 = new ClipVertex
                {
                    Vertex = tpv[i2],
                    Id =
                    {
                        Feature =
                        {
                            IndexA = 0,
                            IndexB = (byte)i2,
                            TypeA = (byte)ContactFeature.FeatureType.Face,
                            TypeB = (byte)ContactFeature.FeatureType.Vertex
                        }
                    }
                };

                if (front)
                {
                    rfi1 = 0;
                    rfi2 = 1;
                    rfv1 = v1;
                    rfv2 = v2;
                    rfnormal = normal1;
                }
                else
                {
                    rfi1 = 1;
                    rfi2 = 0;
                    rfv1 = v2;
                    rfv2 = v1;
                    rfnormal = -normal1;
                }
            }
            else
            {
                manifold.Type = Manifold.ManifoldType.FaceB;

                incidentEdge.Item1 = new ClipVertex
                {
                    Vertex = v1,
                    Id =
                    {
                        Feature =
                        {
                            IndexA = 0,
                            IndexB = (byte)primaryAxis.Index,
                            TypeA = (byte)ContactFeature.FeatureType.Vertex,
                            TypeB = (byte)ContactFeature.FeatureType.Face
                        }
                    }
                };

                incidentEdge.Item2 = new ClipVertex
                {
                    Vertex = v2,
                    Id =
                    {
                        Feature =
                        {
                            IndexA = 0,
                            IndexB = (byte)primaryAxis.Index,
                            TypeA = (byte)ContactFeature.FeatureType.Vertex,
                            TypeB = (byte)ContactFeature.FeatureType.Face
                        }
                    }
                };

                rfi1 = primaryAxis.Index;
                rfi2 = rfi1 + 1 < tpc ? rfi1 + 1 : 0;
                rfv1 = tpv[rfi1];
                rfv2 = tpv[rfi2];
                rfnormal = tpn[rfi1];
            }

            rfsideNormal1.X = rfnormal.Y;
            rfsideNormal1.Y = -rfnormal.X;
            var rfsideNormal2 = -rfsideNormal1;
            var rfsideOffset1 = Vector2.Dot(rfsideNormal1, rfv1);
            var rfsideOffset2 = Vector2.Dot(rfsideNormal2, rfv2);

            // Clip incident edge against extruded edge1 side edges.
            FixedArray2<ClipVertex> clipPoints1, clipPoints2;

            // Clip to box side 1
            var np = ClipSegmentToLine(out clipPoints1, incidentEdge,
                                       rfsideNormal1, rfsideOffset1, rfi1);

            if (np < 2)
            {
                return false;
            }

            // Clip to negative box side 1
            np = ClipSegmentToLine(out clipPoints2, clipPoints1,
                                   rfsideNormal2, rfsideOffset2, rfi2);

            if (np < 2)
            {
                return false;
            }

            // Now clipPoints2 contains the clipped points.
            if (primaryAxis.Type == Axis.AxisType.EdgeA)
            {
                manifold.LocalPoint = rfv1;
                manifold.LocalNormal = rfnormal;
            }
            else
            {
                manifold.LocalPoint = polygonB.Vertices[rfi1];
                manifold.LocalNormal = polygonB.Normals[rfi1];
            }

            var pointCount = 0;
            for (var i = 0; i < 2; ++i)
            {
                if (Vector2.Dot(rfnormal, clipPoints2[i].Vertex - rfv1) <= radius)
                {
                    var cp = manifold.Points[pointCount];
                    if (primaryAxis.Type == Axis.AxisType.EdgeA)
                    {
                        cp.LocalPoint = xf.FromOther(clipPoints2[i].Vertex);
                        cp.Id = clipPoints2[i].Id;
                    }
                    else
                    {
                        cp.LocalPoint = clipPoints2[i].Vertex;
                        cp.Id.Feature.TypeA = clipPoints2[i].Id.Feature.TypeB;
                        cp.Id.Feature.TypeB = clipPoints2[i].Id.Feature.TypeA;
                        cp.Id.Feature.IndexA = clipPoints2[i].Id.Feature.IndexB;
                        cp.Id.Feature.IndexB = clipPoints2[i].Id.Feature.IndexA;
                    }
                    manifold.Points[pointCount] = cp;

                    ++pointCount;
                }
            }

            manifold.PointCount = pointCount;

            return pointCount > 0;
        }

        // This structure is used to keep track of the best separating axis.
        private struct Axis
        {
            public enum AxisType
            {
                None,

                EdgeA,

                EdgeB
            };

            public AxisType Type;

            public int Index;

            public float Separation;
        }
    }
}
