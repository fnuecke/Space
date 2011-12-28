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

        #endregion

        #region Fields

        /// <summary>
        /// Actual value for the value property.
        /// </summary>
        private float _value;

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
            Value = System.Math.Min(MaxValue, Value + Regeneration);
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
                .Write(MaxValue)
                .Write(Regeneration);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _value = packet.ReadSingle();
            MaxValue = packet.ReadSingle();
            Regeneration = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(_value));
            hasher.Put(BitConverter.GetBytes(MaxValue));
            hasher.Put(BitConverter.GetBytes(Regeneration));
        }

        #endregion
    }
}
