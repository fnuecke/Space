using Engine.Serialization;
using Space.Data;

namespace Space.Simulation
{
    public class PlayerInfo : IPacketizable
    {
        public ShipData Ship { get; set; }
        public WeaponData Weapon { get; set; }

        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Ship)
                .Write(Weapon);
        }

        public void Depacketize(Packet packet)
        {
            Ship = packet.ReadPacketizable<ShipData>();
            Weapon = packet.ReadPacketizable<WeaponData>();
        }
    }
}
