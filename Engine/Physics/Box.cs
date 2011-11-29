using Engine.Math;
using Engine.Physics.Intersection;
using Engine.Serialization;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for box shaped world objects.
    /// </summary>
    public abstract class Box<TState, TSteppable, TCommandType, TPlayerData> : Collideable<TState, TSteppable, TCommandType, TPlayerData>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TCommandType : struct
        where TPlayerData : IPacketizable
    {
        #region Properties

        /// <summary>
        /// The minimal AABB surrounding this object.
        /// </summary>
        public FRectangle Bounds { get { return FRectangle.Create(position, size.X, size.Y); } }

        #endregion

        #region Fields

        /// <summary>
        /// Width and height of the object.
        /// </summary>
        protected FPoint size;

        #endregion

        #region Constructor

        /// <summary>
        /// For deserializing.
        /// </summary>
        protected Box()
        {
        }

        protected Box(Fixed width, Fixed height)
        {
            this.size = FPoint.Create(width, height);
        }

        #endregion

        #region Intersection

        public override bool Intersects(ref FPoint extents, ref FPoint previousPosition, ref FPoint position)
        {
            return AABBSweep.Test(ref this.size, ref this.previousPosition, ref this.position, ref extents, ref previousPosition, ref position);
        }

        public override bool Intersects(Fixed radius, ref FPoint previousPosition, ref FPoint position)
        {
            return SphereAABBSweep.Test(radius, ref previousPosition, ref position, ref size, ref previousPosition, ref position);
        }

        #endregion

        #region Serialization

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(size);

            base.Packetize(packet);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            size = packet.ReadFPoint();

            base.Depacketize(packet);
        }

        #endregion
    }
}
