using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    class AstronomicBody : AbstractComponent
    {
        #region Fields

        public AstronomicBodyType Type;

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
