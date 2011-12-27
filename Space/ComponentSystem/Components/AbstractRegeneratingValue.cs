using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    public abstract class AbstractRegeneratingValue : AbstractComponent
    {
        #region Properties

        public float Value { get; set; }

        public float MaxValue { get; set; }

        public float Regeneration { get; set; }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            Value = System.Math.Min(MaxValue, Value + Regeneration);
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

            Value = packet.ReadSingle();
            MaxValue = packet.ReadSingle();
            Regeneration = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Value));
            hasher.Put(BitConverter.GetBytes(MaxValue));
            hasher.Put(BitConverter.GetBytes(Regeneration));
        }

        #endregion
    }
}
