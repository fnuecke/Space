using System;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Entities with this component have an expiration date, after which they
    /// will be removed from the entity manager.
    /// </summary>
    public sealed class Expiration : AbstractComponent
    {
        #region Fields
        
        /// <summary>
        /// The number remaining updates the entity this component belongs to
        /// is allowed to live.
        /// </summary>
        public int TimeToLive;

        #endregion

        #region Constructor

        public Expiration(int ttl)
        {
            this.TimeToLive = ttl;
        }

        public Expiration()
            : this(0)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Decrements TTL by one and checks if we're past the expiration date.
        /// </summary>
        /// <param name="parameterization">Not used.</param>
        public override void Update(object parameterization)
        {
            if (TimeToLive > 0)
            {
                --TimeToLive;
            }
            else if (Entity != null)
            {
                Entity.Manager.RemoveEntity(Entity);
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization / Hashing

        public override Serialization.Packet Packetize(Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(TimeToLive);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);
            
            TimeToLive = packet.ReadInt32();
        }

        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(TimeToLive));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Expiration)base.DeepCopy(into);

            if (copy == into)
            {
                copy.TimeToLive = TimeToLive;
            }

            return copy;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + TimeToLive.ToString();
        }

        #endregion
    }
}
