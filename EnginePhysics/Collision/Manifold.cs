using System;
using System.Globalization;
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
    /// A manifold for two touching convex shapes.
    /// As Box2D we support multiple types of contact:
    /// - clip point versus plane with radius
    /// - point versus point with radius (circles)
    /// The local point and normal usage depends on the manifold type.
    /// We store contacts in this way so that position correction can
    /// account for movement, which is critical for continuous physics.
    /// All contact scenarios must be expressed in one of these types.
    /// This structure is stored across time steps, so we keep it small.
    /// </summary>
    internal struct Manifold
    {
        static Manifold()
        {
            Packetizable.AddValueTypeOverloads(typeof(PacketManifoldExtensions));
        }

        /// <summary>
        /// Possibly types of manifolds, i.e. what kind of overlap it
        /// represents (between what kind of shapes).
        /// </summary>
        public enum ManifoldType
        {
            Circles,
            FaceA,
            FaceB
        }

        /// <summary>
        /// The points of contact.
        /// </summary>
        public FixedArray2<ManifoldPoint> Points;

        /// <summary>
        /// Usage depends on manifold type:
        /// -Circles: not used.
        /// -FaceA: the normal on polygonA.
        /// -FaceB: the normal on polygonB.
        /// </summary>
        public Vector2 LocalNormal;

        /// <summary>
        /// Usage depends on manifold type:
        /// -Circles: the local center of circleA.
        /// -FaceA: the center of faceA.
        /// -FaceB: the center of faceB.
        /// </summary>
        public LocalPoint LocalPoint;

        /// <summary>
        /// The type of this manifold.
        /// </summary>
        public ManifoldType Type;

        /// <summary>
        /// The number of manifold points.
        /// </summary>
        public int PointCount;

        /// <summary>
        /// Computes the world manifold data from this manifold with
        /// the specified properties for the two involved objects.
        /// </summary>
        /// <param name="xfA">The transform of object A.</param>
        /// <param name="radiusA">The radius of object A.</param>
        /// <param name="xfB">The transform of object B.</param>
        /// <param name="radiusB">The radius of object B.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="points">The world contact points.</param>
        public void ComputeWorldManifold(WorldTransform xfA, float radiusA,
                                         WorldTransform xfB, float radiusB,
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
                            normal = (Vector2)(pointB - pointA);
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
                            var cA = clipPoint + (radiusA - Vector2.Dot((Vector2)(clipPoint - planePoint), normal)) * normal;
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
                            var cB = clipPoint + (radiusB - Vector2.Dot((Vector2)(clipPoint - planePoint), normal)) * normal;
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
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "{Manifold: Type=" + Type +
                   ", PointCount=" + PointCount +
                   (PointCount > 0
                        ? ("LocalNormal=" + LocalNormal.X.ToString(CultureInfo.InvariantCulture) + ":" + LocalNormal.Y.ToString(CultureInfo.InvariantCulture) +
                           ", LocalPoint=" + LocalPoint.X.ToString(CultureInfo.InvariantCulture) + ":" + LocalPoint.Y.ToString(CultureInfo.InvariantCulture) +
                           ", Point1=" + Points.Item1 +
                           (PointCount > 1 ? ("Point2=" + Points.Item2) : ""))
                        : "");
        }
    }
    
    /// <summary>
    /// A manifold point is a contact point belonging to a contact
    /// manifold. It holds details related to the geometry and dynamics
    /// of the contact points.
    /// The local point usage depends on the manifold type:
    /// This structure is stored across time steps, so we keep it small.
    /// Note: the impulses are used for internal caching and may not
    /// provide reliable contact forces, especially for high speed collisions.
    /// </summary>
    internal struct ManifoldPoint
    {
        /// <summary>
        /// Usage depends on manifold type:
        /// -Circles: the local center of circleB.
        /// -FaceA: the local center of cirlceB or the clip point of polygonB.
        /// -FaceB: the clip point of polygonA.
        /// </summary>
        public LocalPoint LocalPoint;

        /// <summary>
        /// The non-penetration impulse.
        /// </summary>
        public float NormalImpulse;

        /// <summary>
        /// The friction impulse.
        /// </summary>
        public float TangentImpulse;

        /// <summary>
        /// Uniquely identifies a contact point between two shapes.
        /// </summary>
        public ContactID Id;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "{Id=" + Id.Key +
                ", LocalPoint=" + LocalPoint.X.ToString(CultureInfo.InvariantCulture) + LocalPoint.Y.ToString(CultureInfo.InvariantCulture) +
                ", NormalImpulse=" + NormalImpulse.ToString(CultureInfo.InvariantCulture) +
                ", TangentImpulse=" + TangentImpulse.ToString(CultureInfo.InvariantCulture);
        }
    }
    
    /// <summary>
    /// Contact ids to facilitate warm starting.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct ContactID
    {
        [FieldOffset(0)]
        public ContactFeature Feature;

        /// <summary>
        /// Used to quickly compare contact ids.
        /// </summary>
        [FieldOffset(0)]
        public uint Key;
    }

    /// <summary>
    /// The features that intersect to form the contact point
    /// This must be 4 bytes or less, as if forms a union with
    /// a uint in <see cref="ContactID"/>.
    /// </summary>
    internal struct ContactFeature
    {
        /// <summary>
        /// Possible feature types.
        /// </summary>
        public enum FeatureType
        {
            Vertex = 0,
            Face = 1
        }

        /// <summary>
        /// Feature index on shapeA.
        /// </summary>
        public byte IndexA;

        /// <summary>
        /// Feature index on shapeB.
        /// </summary>
        public byte IndexB;

        /// <summary>
        /// The feature type on shapeA.
        /// </summary>
        public byte TypeA;

        /// <summary>
        /// The feature type on shapeB.
        /// </summary>
        public byte TypeB;
    }

    internal static class PacketManifoldExtensions
    {
        /// <summary>
        /// Writes the specified manifold value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Write(this Packet packet, Manifold data)
        {
            return packet
                .Write(data.Points.Item1)
                .Write(data.Points.Item2)
                .Write(data.LocalNormal)
                .Write(data.LocalPoint)
                .Write((byte)data.Type)
                .Write(data.PointCount);
        }
        
        /// <summary>
        /// Reads a Manifold value.
        /// </summary>
        /// <param name="result">The read value.</param>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        /// <returns>This packet, for call chaining.</returns>
        public static Packet Read(this Packet packet, out Manifold result)
        {
            result = packet.ReadManifold();
            return packet;
        }

        /// <summary>
        /// Reads a Manifold value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        public static Manifold ReadManifold(this Packet packet)
        {
            Manifold result;
            result.Points = new FixedArray2<ManifoldPoint>
            {
                Item1 = packet.ReadManifoldPoint(),
                Item2 = packet.ReadManifoldPoint()
            };
            result.LocalNormal = packet.ReadVector2();
            result.LocalPoint = packet.ReadVector2();
            result.Type = (Manifold.ManifoldType)packet.ReadByte();
            result.PointCount = packet.ReadInt32();
            return result;
        }

        /// <summary>
        /// Writes the specified manifold point value.
        /// </summary>
        /// <param name="data">The value to write.</param>
        /// <returns>This packet, for call chaining.</returns>
        private static Packet Write(this Packet packet, ManifoldPoint data)
        {
            return packet
                .Write(data.LocalPoint)
                .Write(data.NormalImpulse)
                .Write(data.TangentImpulse)
                .Write(data.Id.Key);
        }

        /// <summary>
        /// Reads a ManifoldPoint value.
        /// </summary>
        /// <returns>The read value.</returns>
        /// <exception cref="PacketException">The packet has not enough
        /// available data for the read operation.</exception>
        private static ManifoldPoint ReadManifoldPoint(this Packet packet)
        {
            ManifoldPoint result;
            result.LocalPoint = packet.ReadVector2();
            result.NormalImpulse = packet.ReadSingle();
            result.TangentImpulse = packet.ReadSingle();
            result.Id.Feature = new ContactFeature();
            result.Id.Key = packet.ReadUInt32();
            return result;
        }
    }

    internal static class HasherManifoldExtensions
    {
        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, Manifold value)
        {
            return hasher
                .Put(value.Points.Item1)
                .Put(value.Points.Item2)
                .Put(value.LocalNormal)
                .Put(value.LocalPoint)
                .Put((byte)value.Type)
                .Put(value.PointCount);
        }

        /// <summary>
        /// Put the specified value to the data of which the hash
        /// gets computed.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        /// <param name="value">the data to add.</param>
        /// <returns>a reference to the hasher, for chaining.</returns>
        public static Hasher Put(this Hasher hasher, ManifoldPoint value)
        {
            return hasher
                .Put(value.LocalPoint)
                .Put(value.NormalImpulse)
                .Put(value.TangentImpulse)
                .Put(value.Id.Key);
        }
    }
}
