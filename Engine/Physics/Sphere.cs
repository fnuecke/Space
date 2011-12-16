using Engine.Math;
using Engine.Physics.Intersection;
using Engine.Serialization;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for spherical world objects.
    /// </summary>
    public abstract class Sphere<TPlayerData, TPacketizerContext>
        : AbstractCollideable<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// The minimal AABB surrounding this object.
        /// </summary>
        public FRectangle Bounds { get { return FRectangle.Create(position, radius * 2, radius * 2); } }

        /// <summary>
        /// Radius of the object.
        /// </summary>
        public Fixed Radius { get; protected set; }

        #endregion

        #region Fields

        /// <summary>
        /// Radius of the object.
        /// </summary>
        protected Fixed radius;

        #endregion

        #region Constructor

        /// <summary>
        /// For deserializing.
        /// </summary>
        protected Sphere()
        {
        }

        protected Sphere(Fixed radius)
        {
            this.radius = radius;
        }

        #endregion

        #region Intersection

        public override bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position)
        {
            return SphereAABBSweep.Test(this.radius, ref this.previousPosition, ref this.position, ref extents, ref previousPosition, ref position);
        }

        public override bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position)
        {
            return SphereSweep.Test(this.radius, ref this.previousPosition, ref this.position, radius, ref previousPosition, ref position);
        }

        #endregion

        #region Serialization

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(radius);

            base.Packetize(packet);
        }

        public override void Depacketize(Serialization.Packet packet, TPacketizerContext context)
        {
            radius = packet.ReadFixed();

            base.Depacketize(packet, context);
        }

        #endregion
    }
}
