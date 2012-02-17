using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Allows targeting another entity, i.e. storing the target of an entity.
    /// This can be used for auto targeting weapons (missiles) but may also be
    /// used to store an AIs current target entity.
    /// </summary>
    public sealed class Target : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The ID of the entity that's being targeted. Negative values mean
        /// that there is no current target.
        /// </summary>
        public int Value;

        #endregion

        #region Constructor

        public Target(int target)
        {
            this.Value = target;
        }

        public Target()
            : this(-1)
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
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Value);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Value));
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
            var copy = (Target)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Value = Value;
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
            return base.ToString() + ", Value = " + Value.ToString();
        }

        #endregion
    }
}
