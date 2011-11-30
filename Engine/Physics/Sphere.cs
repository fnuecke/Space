using Engine.Math;
using Engine.Physics.Intersection;
using Engine.Serialization;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for spherical world objects.
    /// </summary>
    public abstract class Sphere<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : Collideable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// The minimal AABB surrounding this object.
        /// </summary>
        public FRectangle Bounds { get { return FRectangle.Create(position, radius * 2, radius * 2); } }

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
            return SphereAABBSweep.Test(radius, ref previousPosition, ref position, ref extents, ref previousPosition, ref position);
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
