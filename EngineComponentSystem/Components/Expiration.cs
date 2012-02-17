using System;

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

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Serialization.Packet Packetize(Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(TimeToLive);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);
            
            TimeToLive = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(TimeToLive));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
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

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", TimeToLive = " + TimeToLive.ToString();
        }

        #endregion
    }
}
