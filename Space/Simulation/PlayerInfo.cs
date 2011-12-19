using Engine.Serialization;
using SpaceData;

namespace Space.Simulation
{
    public class PlayerInfo : IPacketizable
    {
        public ShipData Ship { get; set; }

        public void Packetize(Packet packet)
        {
            packet.Write(Ship);
        }

        public void Depacketize(Packet packet)
        {
            Ship = new ShipData();
            Ship.Depacketize(packet);
        }
    }
}
