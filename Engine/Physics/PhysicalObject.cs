using System;
using Engine.Math;
using Engine.Serialization;
using Engine.Simulation;
using Engine.Util;

namespace Engine.Physics
{
    /// <summary>
    /// Base class for all physical objects.
    /// 
    /// This means the objects have a position, an orientation and a
    /// movement vector (speed / acceleration).
    /// </summary>
    public abstract class PhysicalObject<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : AbstractSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>, IPacketizable<TPlayerData, TPacketizerContext>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// Current position of the object.
        /// </summary>
        public FPoint Position { get { return position; } }

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        public Fixed Rotation { get { return rotation; } }

        #endregion

        #region Fields

        /// <summary>
        /// Current position of the object.
        /// </summary>
        protected FPoint position;

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        protected Fixed rotation;

        /// <summary>
        /// Position before the last update.
        /// </summary>
        protected FPoint previousPosition;

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

        private static readonly Fixed Dampening = Fixed.Create(0.99);

        private static readonly Fixed Epsilon = Fixed.Create(0.01);

        public override void Update()
        {
            rotation += speedRotation;
            if (rotation < -Fixed.PI)
            {
                rotation += Fixed.PI * 2;
            }
            else if (rotation > Fixed.PI)
            {
                rotation -= Fixed.PI * 2;
            }
            previousPosition = position;
            Fixed previousSpeed = speedMovement.Norm;
            speedMovement += acceleration;
            speedMovement *= Dampening;
            if (previousSpeed > Fixed.Zero && speedMovement.Norm < Epsilon)
            {
                speedMovement = FPoint.Zero;
            }
            position += speedMovement;
        }

        public virtual void PostUpdate()
        {
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(this.position.X.RawValue));
            hasher.Put(BitConverter.GetBytes(this.position.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(this.rotation.RawValue));
            hasher.Put(BitConverter.GetBytes(this.previousPosition.X.RawValue));
            hasher.Put(BitConverter.GetBytes(this.previousPosition.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(this.acceleration.X.RawValue));
            hasher.Put(BitConverter.GetBytes(this.acceleration.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(this.speedMovement.X.RawValue));
            hasher.Put(BitConverter.GetBytes(this.speedMovement.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(this.speedRotation.RawValue));
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

        public override void Depacketize(Packet packet, TPacketizerContext context)
        {
            position = packet.ReadFPoint();
            previousPosition = packet.ReadFPoint();
            rotation = packet.ReadFixed();
            acceleration = packet.ReadFPoint();
            speedMovement = packet.ReadFPoint();
            speedRotation = packet.ReadFixed();

            base.Depacketize(packet, context);
        }

        #endregion
    }
}
