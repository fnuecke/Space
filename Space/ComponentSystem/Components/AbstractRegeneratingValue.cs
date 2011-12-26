using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    public abstract class AbstractRegeneratingValue : AbstractComponent
    {
        #region Properties

        public Fixed Value { get; set; }

        public Fixed MaxValue { get; set; }

        public Fixed Regeneration { get; set; }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            Value = Fixed.Min(MaxValue, Value + Regeneration);
        }

        public override bool SupportsParameterization(System.Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Value)
                .Write(MaxValue)
                .Write(Regeneration);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadFixed();
            MaxValue = packet.ReadFixed();
            Regeneration = packet.ReadFixed();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Value.RawValue));
            hasher.Put(BitConverter.GetBytes(MaxValue.RawValue));
            hasher.Put(BitConverter.GetBytes(Regeneration.RawValue));
        }

        #endregion
    }
}
