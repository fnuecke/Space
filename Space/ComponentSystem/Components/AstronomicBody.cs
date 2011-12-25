using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    class AstronomicBody:AbstractComponent
    {

        #region Fields

        public  AstronomicBodyType Type;

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet
                .Write((byte)Type);
        }

        public override void Depacketize(Packet packet)
        {
            Type = (AstronomicBodyType)packet.ReadByte();
            
        }

        

        #endregion
    }
}
