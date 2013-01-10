using System;
using System.Collections.Generic;
using System.IO;
using Engine.ComponentSystem.Components;
using Engine.Physics.Math;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.Physics.Components
{
    /// <summary>
    ///     A polygonal fixture, which defines a shape based on a set of vertices positioned relative to the local origin
    ///     of the body.
    /// </summary>
    public sealed class PolygonFixture : Fixture
    {
        #region Fields

        /// <summary>The vertices that make up this polygon.</summary>
        [PacketizerIgnore]
        internal readonly LocalPoint[] Vertices = new LocalPoint[Settings.MaxPolygonVertices];

        /// <summary>The surface normals of the edges of this polygon.</summary>
        [PacketizerIgnore]
        internal readonly Vector2[] Normals = new Vector2[Settings.MaxPolygonVertices];

        /// <summary>The number of vertices in this fixture.</summary>
        internal int Count;

        /// <summary>The centroid of this polygon.</summary>
        internal LocalPoint Centroid;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonFixture"/> class.
        /// </summary>
        public PolygonFixture() : base(FixtureType.Polygon)
        {
            Radius = Settings.PolygonRadius;
        }

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherPolygon = (PolygonFixture) other;
            for (var i = 0; i < otherPolygon.Count; ++i)
            {
                Vertices[i] = otherPolygon.Vertices[i];
                Normals[i] = otherPolygon.Normals[i];
            }
            Count = otherPolygon.Count;
            Centroid = otherPolygon.Centroid;

            return this;
        }

        /// <summary>
        ///     Create a convex hull from the given array of local points. The count must be in the range [3,
        ///     b2_maxPolygonVertices]. @warning the points may be re-ordered, even if they form a convex polygon @warning
        ///     collinear points are handled but not removed. Collinear points may lead to poor stacking behavior.
        /// </summary>
        /// <param name="points">The points to generate the polygon from.</param>
        public PolygonFixture InitializeShape(IList<Vector2> points)
        {
            if (points.Count < 3 || points.Count > Settings.MaxPolygonVertices)
            {
                throw new ArgumentOutOfRangeException("points", "Invalid number of vertices.");
            }

            // Fallback handling if we have too many points.
            var n = System.Math.Min(points.Count, Settings.MaxPolygonVertices);

            // Copy vertices into local buffer
            var ps = new LocalPoint[Settings.MaxPolygonVertices];
            for (var i = 0; i < n; ++i)
            {
                ps[i] = points[i];
            }

            // Create the convex hull using the Gift wrapping algorithm
            // http://en.wikipedia.org/wiki/Gift_wrapping_algorithm

            // Find the right most point on the hull
            var i0 = 0;
            var x0 = ps[0].X;
            for (var i = 1; i < points.Count; ++i)
            {
                var x = ps[i].X;
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                if (x > x0 || (x == x0 && ps[i].Y < ps[i0].Y))
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    i0 = i;
                    x0 = x;
                }
            }

            var hull = new int[Settings.MaxPolygonVertices];
            var m = 0;
            var ih = i0;

            for (;;)
            {
                hull[m] = ih;

                var ie = 0;
                for (var j = 1; j < n; ++j)
                {
                    if (ie == ih)
                    {
                        ie = j;
                        continue;
                    }

                    var r = ps[ie] - ps[hull[m]];
                    var v = ps[j] - ps[hull[m]];
                    var c = Vector2Util.Cross(ref r, ref v);
                    if (c < 0.0f)
                    {
                        ie = j;
                    }

                    // Collinearity check
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional.
                    if (c == 0.0f && v.LengthSquared() > r.LengthSquared())
// ReSharper restore CompareOfFloatsByEqualityOperator
                    {
                        ie = j;
                    }
                }

                ++m;
                ih = ie;

                if (ie == i0)
                {
                    break;
                }
            }

            Count = m;

            // Copy vertices.
            for (var i = 0; i < m; ++i)
            {
                Vertices[i] = ps[hull[i]];
            }

            // Compute normals. Ensure the edges have non-zero length.
            for (var i = 0; i < m; ++i)
            {
                var i1 = i;
                var i2 = i + 1 < m ? i + 1 : 0;
                var edge = Vertices[i2] - Vertices[i1];
                System.Diagnostics.Debug.Assert(edge.LengthSquared() > Settings.Epsilon * Settings.Epsilon);
                Normals[i] = Vector2Util.Cross(ref edge, 1.0f);
                Normals[i].Normalize();
            }

            // Compute the polygon centroid.
            Centroid = ComputeCentroid();

            Synchronize();

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            for (var i = 0; i < Settings.MaxPolygonVertices; ++i)
            {
                Vertices[i] = LocalPoint.Zero;
                Normals[i] = Vector2.Zero;
            }
            Count = 0;
            Centroid = LocalPoint.Zero;
        }

        #endregion

        #region Methods

        /// <summary>Test the specified point for containment in this fixture.</summary>
        /// <param name="localPoint">The point in local coordinates.</param>
        /// <returns>Whether the point is contained in this fixture or not.</returns>
        public override bool TestPoint(Vector2 localPoint)
        {
            for (var i = 0; i < Count; ++i)
            {
                if (Vector2Util.Dot(Normals[i], localPoint - Vertices[i]) > 0.0f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Get the mass data for this fixture. The mass data is based on the density and the shape. The rotational
        ///     inertia is about the shape's origin. This operation may be expensive.
        /// </summary>
        /// <param name="mass">The mass of this fixture.</param>
        /// <param name="center">The center of mass relative to the local origin.</param>
        /// <param name="inertia">The inertia about the local origin.</param>
        internal override void GetMassData(out float mass, out LocalPoint center, out float inertia)
        {
            // Polygon mass, centroid, and inertia.
            // Let rho be the polygon density in mass per unit area.
            // Then:
            // mass = rho * int(dA)
            // centroid.X = (1/mass) * rho * int(x * dA)
            // centroid.Y = (1/mass) * rho * int(y * dA)
            // I = rho * int((x*x + y*y) * dA)
            //
            // We can compute these integrals by summing all the integrals
            // for each triangle of the polygon. To evaluate the integral
            // for a single triangle, we make a change of variables to
            // the (u,v) coordinates of the triangle:
            // x = x0 + e1x * u + e2x * v
            // y = y0 + e1y * u + e2y * v
            // where 0 <= u && 0 <= v && u + v <= 1.
            //
            // We integrate u from [0,1-v] and then v from [0,1].
            // We also need to use the Jacobian of the transformation:
            // D = cross(e1, e2)
            //
            // Simplification: triangle centroid = (1/3) * (p1 + p2 + p3)
            //
            // The rest of the derivation is handled by computer algebra.

            System.Diagnostics.Debug.Assert(Count >= 3);

            var centroid = LocalPoint.Zero;
            var area = 0f;
            var I = 0f;

            // s is the reference point for forming triangles.
            // It's location doesn't change the result (except for rounding error).
            var s = LocalPoint.Zero;

            // This code would put the reference point inside the polygon.
            for (var i = 0; i < Count; ++i)
            {
                s += Vertices[i];
            }
            s /= Count;

            const float inv3 = 1.0f / 3.0f;

            for (var i = 0; i < Count; ++i)
            {
                // Triangle vertices.
                var e1 = Vertices[i] - s;
                var e2 = i + 1 < Count
                             ? Vertices[i + 1] - s
                             : Vertices[0] - s;

                var d = Vector2Util.Cross(ref e1, ref e2);

                var triangleArea = 0.5f * d;
                area += triangleArea;

                // Area weighted centroid
                centroid += triangleArea * inv3 * (e1 + e2);

                var intx2 = e1.X * e1.X + e2.X * e1.X + e2.X * e2.X;
                var inty2 = e1.Y * e1.Y + e2.Y * e1.Y + e2.Y * e2.Y;

                I += (0.25f * inv3 * d) * (intx2 + inty2);
            }

            // Total mass
            mass = Density * area;

            // Center of mass
            System.Diagnostics.Debug.Assert(area > Settings.Epsilon);
            centroid *= 1.0f / area;
            center = centroid + s;

            // Inertia tensor relative to the local origin (point s).
            inertia = Density * I;

            // Shift to center of mass then to original body origin.
            inertia += mass * (Vector2Util.Dot(ref center, ref center) - Vector2Util.Dot(ref centroid, ref centroid));
        }

        /// <summary>Computes the global bounds of this fixture given the specified body transform.</summary>
        /// <param name="transform">The world transform of the body.</param>
        /// <returns>The global bounds of this fixture.</returns>
        internal override WorldBounds ComputeBounds(WorldTransform transform)
        {
            var lower = transform.ToGlobal(Vertices[0]);
            var upper = lower;

            for (var i = 1; i < Count; ++i)
            {
                var v = transform.ToGlobal(Vertices[i]);
                lower.X = (lower.X < v.X) ? lower.X : v.X;
                lower.Y = (lower.Y < v.Y) ? lower.Y : v.Y;
                upper.X = (upper.X > v.X) ? upper.X : v.X;
                upper.Y = (upper.Y > v.Y) ? upper.Y : v.Y;
            }

            Vector2 size;
// ReSharper disable RedundantCast Necessary for FarPhysics.
            size.X = (float) (upper.X - lower.X) + 2 * Radius;
            size.Y = (float) (upper.Y - lower.Y) + 2 * Radius;
// ReSharper restore RedundantCast

            WorldBounds bounds;
            bounds.X = lower.X - Radius;
            bounds.Y = lower.Y - Radius;
            bounds.Width = size.X;
            bounds.Height = size.Y;
            return bounds;
        }

        #endregion

        #region Utility

        /// <summary>Computes the centroid of the vertices, used in initialization.</summary>
        /// <returns>The centroid.</returns>
        private LocalPoint ComputeCentroid()
        {
            var c = LocalPoint.Zero;
            var area = 0.0f;

            const float inv3 = 1.0f / 3.0f;

            for (var i = 0; i < Count; ++i)
            {
                // Triangle vertices.
                var p1 = LocalPoint.Zero;
                var p2 = Vertices[i];
                var p3 = i + 1 < Count ? Vertices[i + 1] : Vertices[0];

                var e1 = p2 - p1;
                var e2 = p3 - p1;

                var triangleArea = 0.5f * Vector2Util.Cross(ref e1, ref e2);
                area += triangleArea;

                // Area weighted centroid
                c += triangleArea * inv3 * (p1 + p2 + p3);
            }

            // Centroid
            System.Diagnostics.Debug.Assert(area > float.Epsilon);
            c *= 1.0f / area;
            return c;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            for (var i = 0; i < Count; ++i)
            {
                packet.Write(Vertices[i]);
                packet.Write(Normals[i]);
            }

            return packet;
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            for (var i = 0; i < Count; ++i)
            {
                Vertices[i] = packet.ReadVector2();
                Normals[i] = packet.ReadVector2();
            }
        }

        /// <summary>Writes a string representation of the object to a string builder.</summary>
        /// <param name="w"> </param>
        /// <param name="indent">The indentation level.</param>
        /// <returns>The string builder, for call chaining.</returns>
        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Vertices = {");
            for (var i = 0; i < Count; i++)
            {
                w.AppendIndent(indent + 1).Write("{");
                w.AppendIndent(indent + 2).Write("Position = ");
                w.Write(Vertices[i]);
                w.AppendIndent(indent + 2).Write("Normal = ");
                w.Write(Normals[i]);
                w.AppendIndent(indent + 1).Write("}");
            }
            w.AppendIndent(indent).Write("}");

            return w;
        }

        #endregion
    }
}