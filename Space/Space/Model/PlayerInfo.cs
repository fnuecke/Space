using Engine.Serialization;

namespace Space.Model
{
    class PlayerInfo : IPacketizable
    {
        public long ShipUID { get; set; }

        public string ShipName { get; set; }

        public void Packetize(Packet packet)
        {
            packet.Write(ShipName);
        }

        public void Depacketize(Packet packet)
        {
            ShipName = packet.ReadString();
        }
    }
}
