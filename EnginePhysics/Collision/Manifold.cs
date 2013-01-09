using System;
using System.Runtime.InteropServices;
using Engine.Collections;
using Engine.Physics.Math;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Collision
{
    /// <summary>
    ///     A manifold for two touching convex shapes. As Box2D we support multiple types of contact:
    ///     <list type="bullet">
    ///         <item>clip point versus plane with radius</item>
    ///         <item>point versus point with radius (circles)</item>
    ///     </list>
    ///     <para/>
    ///     The local point and normal usage depends on the manifold type. We store contacts in this way so that position
    ///     correction can account for movement, which is critical for continuous physics. All contact scenarios must be
    ///     expressed in one of these types. This structure is stored across time steps, so we keep it small.
    /// </summary>
    internal struct Manifold
    {
        static Manifold()
        {
            Packetizable.AddValueTypeOverloads(typeof (PacketManifoldExtensions));
        }

        /// <summary>Possibly types of manifolds, i.e. what kind of overlap it represents (between what kind of shapes).</summary>
        public enum ManifoldType : byte
        {
            Circles,
            FaceA,
            FaceB
        }

        /// <summary>
        ///     Usage depends on manifold type:
        ///     <list type="bullet">
        ///         <item>Circles: not used.</item>
        ///         <item>FaceA: the normal on polygonA.</item>
        ///         <item>FaceB: the normal on polygonB.</item>
        ///     </list>
        /// </summary>
        public Vector2 LocalNormal;

        /// <summary>
        ///     Usage depends on manifold type:
        ///     <list type="bullet">
        ///         <item>Circles: the local center of circleA.</item>
        ///         <item>FaceA: the center of faceA.</item>
        ///         <item>FaceB: the center of faceB.</item>
        ///     </list>
        /// </summary>
        public LocalPoint LocalPoint;

        /// <summary>The number of manifold points.</summary>
        public int PointCount;

        /// <summary>The points of contact.</summary>
        public FixedArray2<ManifoldPoint> Points;

        /// <summary>The type of this manifold.</summary>
        public ManifoldType Type;

        /// <summary>
        ///     Computes the world manifold data from this manifold with the specified properties for the two involved
        ///     objects.
        /// </summary>
        /// <param name="xfA">The transform of object A.</param>
        /// <param name="radiusA">The radius of object A.</param>
        /// <param name="xfB">The transform of object B.</param>
        /// <param name="radiusB">The radius of object B.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="points">The world contact points.</param>
        public void ComputeWorldManifold(
            WorldTransform xfA,
            float radiusA,
            WorldTransform xfB,
            float radiusB,
            out Vector2 normal,
            out FixedArray2<WorldPoint> points)
        {
            points = new FixedArray2<WorldPoint>(); // satisfy out
            switch (Type)
            {
                case ManifoldType.Circles:
                {
                    normal = Vector2.UnitX;
                    var pointA = xfA.ToGlobal(LocalPoint);
                    var pointB = xfB.ToGlobal(Points[0].LocalPoint);
                    if (WorldPoint.DistanceSquared(pointA, pointB) > Settings.Epsilon * Settings.Epsilon)
                    {
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        normal = (Vector2) (pointB - pointA);
// ReSharper restore RedundantCast
                        normal.Normalize();
                    }

                    var cA = pointA + radiusA * normal;
                    var cB = pointB - radiusB * normal;
                    points.Item1 = 0.5f * (cA + cB);
                    break;
                }

                case ManifoldType.FaceA:
                {
                    normal = xfA.Rotation * LocalNormal;
                    var planePoint = xfA.ToGlobal(LocalPoint);

                    for (var i = 0; i < PointCount; ++i)
                    {
                        var clipPoint = xfB.ToGlobal(Points[i].LocalPoint);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        var cA = clipPoint +
                                 (radiusA - Vector2.Dot((Vector2) (clipPoint - planePoint), normal)) * normal;
// ReSharper restore RedundantCast
                        var cB = clipPoint - radiusB * normal;
                        points[i] = 0.5f * (cA + cB);
                    }
                    break;
                }

                case ManifoldType.FaceB:
                {
                    normal = xfB.Rotation * LocalNormal;
                    var planePoint = xfB.ToGlobal(LocalPoint);

                    for (var i = 0; i < PointCount; ++i)
                    {
                        var clipPoint = xfA.ToGlobal(Points[i].LocalPoint);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        var cB = clipPoint +
                                 (radiusB - Vector2.Dot((Vector2) (clipPoint - planePoint), normal)) * normal;
// ReSharper restore RedundantCast
                        var cA = clipPoint - radiusA * normal;
                        points[i] = 0.5f * (cA + cB);
                    }

                    // Ensure normal points from A to B.
                    normal = -normal;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (PointCount > 1)
            {
                return string.Format(
                    "{{Type:{0} PointCount:{1} LocalNormal:{2} LocalPoint:{3} Point1:{4} Point2:{5}}}",
                    Type,
                    PointCount,
                    LocalNormal,
                    LocalPoint,
                    Points[0],
                    Points[1]);
            }
            if (PointCount > 0)
            {
                return string.Format(
                    "{{Type:{0} PointCount:{1} LocalNormal:{2} LocalPoint:{3} Point1:{4}}}",
                    Type,
                    PointCount,
                    LocalNormal,
                    LocalPoint,
                    Points[0]);
            }
            return string.Format("{{Type:{0} PointCount:{1}}}", Type, PointCount);
        }
    }

    /// <summary>
    ///     A manifold point is a contact point belonging to a contact manifold. It holds details related to the geometry and
    ///     dynamics of the contact points. The local point usage depends on the manifold type: This structure is stored across
    ///     time steps, so we keep it small.
    ///     <para/>
    ///     Note: the impulses are used for internal caching and may not provide reliable contact forces, especially for high
    ///     speed collisions.
    /// </summary>
    internal struct ManifoldPoint
    {
        /// <summary>Uniquely identifies a contact point between two shapes.</summary>
        public ContactId Id;

        /// <summary>
        ///     Usage depends on manifold type:
        ///     <list type="bullet">
        ///         <item>Circles: the local center of circleB.</item>
        ///         <item>FaceA: the local center of circleB or the clip point of polygonB.</item>
        ///         <item>FaceB: the clip point of polygonA.</item>
        ///     </list>
        /// </summary>
        public LocalPoint LocalPoint;

        /// <summary>The non-penetration impulse.</summary>
        public float NormalImpulse;

        /// <summary>The friction impulse.</summary>
        public float TangentImpulse;

        /// <summary>
        ///     Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "{{Id:{0} LocalPoint:{1} NormalImpulse:{2} TangentImpulse:{3}}}",
                Id.Key,
                LocalPoint,
                NormalImpulse,
                TangentImpulse);
        }
    }

    /// <summary>Contact ids to facilitate warm starting.</summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct ContactId
    {
        /// <summary>Describes the features of the parties involved in a contact.</summary>
        [FieldOffset(0)]
        public ContactFeature Feature;

        /// <summary>Used to quickly compare contact ids.</summary>
        [FieldOffset(0)]
        public uint Key;
    }

    /// <summary>
    ///     The features that intersect to form the contact point This must be 4 bytes or less, as if forms a union with a uint
    ///     in <see cref="ContactId"/>.
    /// </summary>
    internal struct ContactFeature
    {
        /// <summary>Possible feature types.</summary>
        public enum FeatureType
        {
            Vertex = 0,
            Face = 1
        }

        /// <summary>Feature index on shapeA.</summary>
        public byte IndexA;

        /// <summary>Feature index on shapeB.</summary>
        public byte IndexB;

        /// <summary>The feature type on shapeA.</summary>
        public byte TypeA;

        /// <summary>The feature type on shapeB.</summary>
        public byte TypeB;
    }

    internal static class PacketManifoldExtensions
    {
        /// <summary>Writes the specified manifold value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static IWritablePacket Write(this IWritablePacket packet, Manifold data)
        {
            return packet
                .Write(data.Points.Item1)
                .Write(data.Points.Item2)
                .Write(data.LocalNormal)
                .Write(data.LocalPoint)
                .Write((byte) data.Type)
                .Write(data.PointCount);
        }

        /// <summary>Reads a Manifold value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="result">The read value.</param>
        /// <returns>This packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static IReadablePacket Read(this IReadablePacket packet, out Manifold result)
        {
            result = packet.ReadManifold();
            return packet;
        }

        /// <summary>Reads a Manifold value.</summary>
        /// <param name="packet">The packet.</param>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        public static Manifold ReadManifold(this IReadablePacket packet)
        {
            var result = new Manifold();
            packet
                .Read(out result.Points.Item1)
                .Read(out result.Points.Item2)
                .Read(out result.LocalNormal)
                .Read(out result.LocalPoint);
            result.Type = (Manifold.ManifoldType) packet.ReadByte();
            packet.Read(out result.PointCount);
            return result;
        }

        /// <summary>Writes the specified manifold point value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        private static IWritablePacket Write(this IWritablePacket packet, ManifoldPoint data)
        {
            return packet
                .Write(data.LocalPoint)
                .Write(data.NormalImpulse)
                .Write(data.TangentImpulse)
                .Write(data.Id.Key);
        }

        /// <summary>Reads a ManifoldPoint value.</summary>
        /// <param name="packet">The packet.</param>
        /// <param name="result">The read value.</param>
        /// <returns>The packet, for call chaining.</returns>
        /// <exception cref="PacketException">The packet has not enough available data for the read operation.</exception>
        private static IReadablePacket Read(this IReadablePacket packet, out ManifoldPoint result)
        {
            result.Id.Feature = new ContactFeature();
            return packet
                .Read(out result.LocalPoint)
                .Read(out result.NormalImpulse)
                .Read(out result.TangentImpulse)
                .Read(out result.Id.Key);
        }
    }
}