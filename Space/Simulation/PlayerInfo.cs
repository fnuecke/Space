using Engine.Serialization;
using Space.Data;

namespace Space.Simulation
{
    public class PlayerInfo : IPacketizable
    {
        public ShipData Ship { get; set; }

        public Packet Packetize(Packet packet)
        {
            return packet.Write(Ship);
        }

        public void Depacketize(Packet packet)
        {
            Ship = packet.ReadPacketizable<ShipData>();
        }
    }
}
