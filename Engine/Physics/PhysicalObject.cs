using Engine.Math;
using Engine.Serialization;
using Engine.Simulation;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for all physical objects.
    /// 
    /// This means the objects have a position, an orientation and a
    /// movement vector (speed / acceleration).
    /// </summary>
    public abstract class PhysicalObject<TState, TSteppable, TCommandType> : AbstractSteppable<TState, TSteppable, TCommandType>, IPacketizable
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType>
        where TCommandType : struct
    {
        #region Fields

        /// <summary>
        /// Current position of the object.
        /// </summary>
        protected FPoint position;

        /// <summary>
        /// Position before the last update.
        /// </summary>
        protected FPoint previousPosition;

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        protected Fixed rotation;

        /// <summary>
        /// Current directed acceleration of the object.
        /// </summary>
        protected FPoint acceleration;

        /// <summary>
        /// Current directed speed of the object.
        /// </summary>
        protected FPoint speedMovement;

        /// <summary>
        /// Current rotation speed of the object.
        /// </summary>
        protected Fixed speedRotation;

        #endregion

        #region Logic

        public virtual void PreUpdate()
        {
        }

        public override void Update()
        {
            rotation += speedRotation;
            previousPosition = position;
            speedMovement += acceleration;
            speedMovement *= Fixed.Create(0.99); // dampening
            position += speedMovement;
        }

        public virtual void PostUpdate()
        {
        }

        #endregion

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write(position);
            packet.Write(previousPosition);
            packet.Write(rotation);
            packet.Write(acceleration);
            packet.Write(speedMovement);
            packet.Write(speedRotation);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            position = packet.ReadFPoint();
            previousPosition = packet.ReadFPoint();
            rotation = packet.ReadFixed();
            acceleration = packet.ReadFPoint();
            speedMovement = packet.ReadFPoint();
            speedRotation = packet.ReadFixed();

            base.Packetize(packet);
        }

        #endregion
    }
}
