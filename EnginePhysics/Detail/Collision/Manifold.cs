using System.Runtime.InteropServices;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Detail.Collision
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

    internal static class ManifoldPacketExtensions
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
}
