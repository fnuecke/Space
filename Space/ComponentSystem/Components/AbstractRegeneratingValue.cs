using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
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

        /// <summary>
        /// The maximum value.
        /// </summary>
        public float MaxValue { get; set; }

        /// <summary>
        /// The amount the value is regenerated per tick.
        /// </summary>
        public float Regeneration { get; set; }

        /// <summary>
        /// The timeout in ticks to wait after the last reducing change, before
        /// applying regeneration again.
        /// </summary>
        public int Timeout { get; set; }

        #endregion

        #region Fields

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
#if DEBUG
            base.Update(parameterization);
#endif
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
        public override bool SupportsParameterization(System.Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(_value)
                .Write(_timeToWait)
                .Write(MaxValue)
                .Write(Regeneration)
                .Write(Timeout);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _value = packet.ReadSingle();
            _timeToWait = packet.ReadInt32();
            MaxValue = packet.ReadSingle();
            Regeneration = packet.ReadSingle();
            Timeout = packet.ReadInt32();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(_value));
        }

        #endregion
    }
}
