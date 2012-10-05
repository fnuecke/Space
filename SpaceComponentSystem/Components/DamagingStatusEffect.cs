using System.Globalization;
using Engine.ComponentSystem.RPG.Components;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// An effect that, while active, damages the entity it is applied to.
    /// </summary>
    public sealed class DamagingStatusEffect : StatusEffect
    {
        #region Constants

        /// <summary>
        /// Value that can be passed as the duration in the initialization method
        /// to signal that the damage should be applied indefinitely.
        /// </summary>
        public const int InfiniteDamageDuration = -1;

        #endregion

        #region Fields

        /// <summary>
        /// The damage to apply per tick (see <c>Interval</c>).
        /// </summary>
        public float Value;

        /// <summary>
        /// The damage tick interval, in game frames.
        /// </summary>
        public int Interval = 1;

        /// <summary>
        /// The ID of the entity that created this effect.
        /// </summary>
        public int Owner;

        /// <summary>
        /// The number of ticks to delay until the next damage tick.
        /// </summary>
        internal int Delay;

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
            Interval = otherDamage.Interval;
            Owner = otherDamage.Owner;
            Delay = otherDamage.Delay;

            return this;
        }

        /// <summary>
        /// Initializes the specified damage once.
        /// </summary>
        /// <param name="tickDamage">The damage to apply per tick (not game frame, see next param).</param>
        /// <param name="owner">The id of the entity that caused the creation of the effect.</param>
        /// <returns></returns>
        public DamagingStatusEffect Initialize(float tickDamage, int owner = 0)
        {
            return Initialize(0, tickDamage, 1, owner);
        }

        /// <summary>
        /// Initializes the specified tick damage.
        /// </summary>
        /// <param name="duration">The duration of the effect, in ticks.</param>
        /// <param name="tickDamage">The damage to apply per tick (not game frame, see next param).</param>
        /// <param name="tickInterval">The tick interval, i.e. every how many game frames to apply the damage.</param>
        /// <param name="owner">The id of the entity that caused the creation of the effect.</param>
        /// <returns></returns>
        public DamagingStatusEffect Initialize(int duration, float tickDamage, int tickInterval = 1, int owner = 0)
        {
            base.Initialize(duration);

            Value = tickDamage;
            Interval = System.Math.Max(1, tickInterval);
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
            Interval = 1;
            Owner = 0;
            Delay = 0;
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
                .Write(Interval)
                .Write(Owner)
                .Write(Delay);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadSingle();
            Interval = packet.ReadInt32();
            Owner = packet.ReadInt32();
            Delay = packet.ReadInt32();
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
            hasher.Put(Interval);
            hasher.Put(Owner);
            hasher.Put(Delay);
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
            return base.ToString() + ", Value=" + Value.ToString(CultureInfo.InvariantCulture) + ", Interval=" + Interval + ", Owner=" + Owner + ", Delay=" + Delay;
        }

        #endregion
    }
}
