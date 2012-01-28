using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Base class for timed status effects that apply to the entity they are
    /// attached to.
    /// </summary>
    public abstract class AbstractStatusEffect : AbstractComponent
    {
        #region Fields
        
        /// <summary>
        /// The remaining number of ticks this effect will stay active.
        /// </summary>
        public int Remaining;

        #endregion

        #region Constructor

        protected AbstractStatusEffect(int duration)
        {
            Remaining = duration;
        }

        protected AbstractStatusEffect()
        {
        }

        #endregion

        #region Logic
        
        /// <summary>
        /// Checks if the buff has expired, and if so removes it.
        /// </summary>
        /// <param name="parameterization">The parameterization to use for this update.</param>
        public override void Update(object parameterization)
        {
            if (Remaining > 0)
            {
                // Still running.
                --Remaining;
            }
            else
            {
                // Expired, remove self.
                Entity.RemoveComponent(this);
            }
        }

        /// <summary>
        /// Support <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The type of parameterization to check.</param>
        /// <returns>
        /// Whether the parameterization is supported.
        /// </returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization

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
                .Write(Remaining);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Remaining = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Remaining));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (AbstractStatusEffect)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Remaining = Remaining;
            }

            return copy;
        }

        #endregion
    }
}
