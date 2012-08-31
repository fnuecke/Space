using System.Globalization;
using Engine.ComponentSystem.RPG.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// An effect that, while active, damages the entity it is applied to.
    /// </summary>
    public sealed class DamagingStatusEffect : StatusEffect
    {
        #region Fields

        /// <summary>
        /// The damage to apply, per tick / update.
        /// </summary>
        public float Value;

        /// <summary>
        /// The ID of the entity that created this effect.
        /// </summary>
        public int Owner;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Engine.ComponentSystem.Components.Component Initialize(Engine.ComponentSystem.Components.Component other)
        {
            base.Initialize(other);

            var otherDamage = (DamagingStatusEffect)other;
            Value = otherDamage.Value;
            Owner = otherDamage.Owner;

            return this;
        }

        /// <summary>
        /// Initializes the specified tick damage.
        /// </summary>
        /// <param name="duration">The duration of the effect, in ticks.</param>
        /// <param name="tickDamage">The damage to apply per tick / update.</param>
        /// <param name="owner">The id of the entity that caused the creation of the effect.</param>
        /// <returns></returns>
        public DamagingStatusEffect Initialize(int duration, float tickDamage, int owner = 0)
        {
            Initialize(duration);

            Value = tickDamage;
            Owner = owner;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Value = 0f;
            Owner = 0;
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
        public override Engine.Serialization.Packet Packetize(Engine.Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(Value)
                .Write(Owner);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadSingle();
            Owner = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Engine.Serialization.Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Value);
            hasher.Put(Owner);
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
            return base.ToString() + ", Value=" + Value.ToString(CultureInfo.InvariantCulture) + ", Owner=" + Owner;
        }

        #endregion
    }
}
