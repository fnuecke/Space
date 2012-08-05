using System;
using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Base class for modules that represent regenerating values.
    /// </summary>
    public abstract class AbstractRegeneratingValue : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The current value.
        /// </summary>
        public float Value { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The maximum value.
        /// </summary>
        public float MaxValue;

        /// <summary>
        /// The amount the value is regenerated per tick.
        /// </summary>
        public float Regeneration;

        /// <summary>
        /// The timeout in ticks to wait after the last reducing change, before
        /// applying regeneration again.
        /// </summary>
        public int Timeout;

        /// <summary>
        /// Time to wait before triggering regeneration again, in ticks.
        /// </summary>
        internal int TimeToWait;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherRegeneratingValue = (AbstractRegeneratingValue)other;
            MaxValue = otherRegeneratingValue.MaxValue;
            Regeneration = otherRegeneratingValue.Regeneration;
            Timeout = otherRegeneratingValue.Timeout;
            Value = otherRegeneratingValue.Value;
            TimeToWait = otherRegeneratingValue.TimeToWait;

            return this;
        }

        /// <summary>
        /// Initialize with the specified timeout.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        public AbstractRegeneratingValue Initialize(int timeout)
        {
            this.Timeout = timeout;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            MaxValue = 0;
            Regeneration = 0;
            Timeout = 0;
            TimeToWait = 0;
            Value = 0;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="value">The value.</param>
        public virtual void SetValue(float value)
        {
            if (value < Value)
            {
                TimeToWait = Timeout;
            }
            Value = Math.Max(0, Math.Min(MaxValue, value));
        }

        #endregion

        #region Logic

        /// <summary>
        /// Recomputes the maximum value and regeneration speed.
        /// </summary>
        internal abstract void RecomputeValues();

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
                .Write(Value)
                .Write(TimeToWait)
                .Write(MaxValue)
                .Write(Regeneration)
                .Write(Timeout);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadSingle();
            TimeToWait = packet.ReadInt32();
            MaxValue = packet.ReadSingle();
            Regeneration = packet.ReadSingle();
            Timeout = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Value);
            hasher.Put(MaxValue);
            hasher.Put(Regeneration);
            hasher.Put(Timeout);
            hasher.Put(TimeToWait);
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
            return base.ToString() + ", Value=" + Value.ToString(CultureInfo.InvariantCulture) + ", MaxValue=" + MaxValue.ToString(CultureInfo.InvariantCulture) + ", Regeneration=" + Regeneration.ToString(CultureInfo.InvariantCulture) + ", Timeout=" + Timeout + ", TimeToWait=" + TimeToWait;
        }

        #endregion
    }
}
