using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Base class for modules that represent regenerating values.
    /// </summary>
    public abstract class AbstractRegeneratingValue : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The current value.
        /// </summary>
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value < _value)
                {
                    _timeToWait = Timeout;
                }
                _value = System.Math.Max(0, value);
            }
        }

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
        /// Actual value for the value property.
        /// </summary>
        private float _value;

        /// <summary>
        /// Time to wait before triggering regeneration again, in ticks.
        /// </summary>
        private int _timeToWait;

        #endregion

        #region Constructor

        protected AbstractRegeneratingValue(int timeout)
        {
            this.Timeout = timeout;
        }

        protected AbstractRegeneratingValue()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Apply regeneration of our value.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
            if (_timeToWait > 0)
            {
                --_timeToWait;
            }
            else
            {
                Value = System.Math.Min(MaxValue, Value + Regeneration);
            }
        }

        /// <summary>
        /// Supports <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether it's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        /// <summary>
        /// Test for change in equipment.
        /// </summary>
        /// <param name="message">Handles module added / removed messages.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is CharacterStatsInvalidated)
            {
                RecomputeValues();
            }
        }

        protected virtual void RecomputeValues()
        {
            // Adjust current energy so it does not exceed our new maximum.
            if (Value > MaxValue)
            {
                Value = MaxValue;
            }
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
                .Write(_value)
                .Write(_timeToWait)
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

            _value = packet.ReadSingle();
            _timeToWait = packet.ReadInt32();
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

            hasher.Put(BitConverter.GetBytes(_value));
            hasher.Put(BitConverter.GetBytes(_timeToWait));
            hasher.Put(BitConverter.GetBytes(MaxValue));
            hasher.Put(BitConverter.GetBytes(Regeneration));
            hasher.Put(BitConverter.GetBytes(Timeout));
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
            var copy = (AbstractRegeneratingValue)base.DeepCopy(into);

            if (copy == into)
            {
                copy.MaxValue = MaxValue;
                copy.Regeneration = Regeneration;
                copy.Timeout = Timeout;
                copy._value = _value;
                copy._timeToWait = _timeToWait;
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
            return base.ToString() + ", Value = " + Value.ToString() + ", MaxValue = " + MaxValue.ToString() + ", Regeneration = " + Regeneration.ToString() + ", Timeout = " + Timeout.ToString() + ", TimeToWait = " + _timeToWait.ToString();
        }

        #endregion
    }
}
