﻿using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    public sealed class AstronomicBody : AbstractComponent
    {
        #region Fields

        public AstronomicBodyType Type;

        #endregion

        #region Constructor

        public AstronomicBody(AstronomicBodyType type)
        {
            this.Type = type;
        }

        public AstronomicBody()
        {
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)Type);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Type = (AstronomicBodyType)packet.ReadByte();
        }

        #endregion
    }
}
