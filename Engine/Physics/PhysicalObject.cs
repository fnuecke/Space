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
    public abstract class PhysicalObject<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        : AbstractSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>, IPacketizable<TPlayerData, TPacketizerContext>
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

        /// <summary>
        /// The directed speed of the object.
        /// </summary>
        public FPoint Velocity { get { return velocity; } }

        /// <summary>
        /// The current rotation speed of the object.
        /// </summary>
        public Fixed Spin { get { return spin; } }

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
        protected FPoint velocity;

        /// <summary>
        /// Current rotation speed of the object.
        /// </summary>
        protected Fixed spin;

        #endregion

        #region Logic

        public virtual void PreUpdate()
        {
        }

        public override void Update()
        {
            rotation += spin;
            if (rotation < -Fixed.PI)
            {
                rotation += Fixed.PI * 2;
            }
            else if (rotation > Fixed.PI)
            {
                rotation -= Fixed.PI * 2;
            }
            previousPosition = position;
            velocity += acceleration;
            position += velocity;
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
            hasher.Put(BitConverter.GetBytes(this.velocity.X.RawValue));
            hasher.Put(BitConverter.GetBytes(this.velocity.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(this.spin.RawValue));
        }

        #endregion

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write(position);
            packet.Write(previousPosition);
            packet.Write(rotation);
            packet.Write(acceleration);
            packet.Write(velocity);
            packet.Write(spin);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, TPacketizerContext context)
        {
            position = packet.ReadFPoint();
            previousPosition = packet.ReadFPoint();
            rotation = packet.ReadFixed();
            acceleration = packet.ReadFPoint();
            velocity = packet.ReadFPoint();
            spin = packet.ReadFixed();

            base.Depacketize(packet, context);
        }

        #endregion
    }
}
