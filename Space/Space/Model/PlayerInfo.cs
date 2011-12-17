using Engine.Serialization;

namespace Space.Model
{
    enum PlayerInfoField
    {
        ShipId,
        ShipType
    }

    public class PlayerInfo : IPacketizable<PlayerInfo>
    {
        public long ShipUID { get; set; }

        public string ShipType { get; set; }

        public void Packetize(Packet packet)
        {
            packet.Write(ShipUID);
            packet.Write(ShipType);
        }

        public void Depacketize(Packet packet, IPacketizerContext<PlayerInfo> context)
        {
            ShipUID = packet.ReadInt64();
            ShipType = packet.ReadString();
        }
    }
}
