using Engine.Serialization;

namespace Space.Model
{
    class PlayerInfo : IPacketizable
    {
        public long ShipUID { get; set; }

        public void Packetize(Packet packet)
        {
        }

        public void Depacketize(Packet packet)
        {
        }
    }
}
