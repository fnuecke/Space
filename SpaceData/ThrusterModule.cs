using Engine.Data;
using Engine.Serialization;

namespace Space.Data
{
    public class ThrusterModule : AbstractEntityModule<EntityAttributeType>
    {
        public float EnergieConsumption;
        public float AccelerationForce;


        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(EnergieConsumption)
                .Write(AccelerationForce);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            
            EnergieConsumption = packet.ReadSingle();
            AccelerationForce = packet.ReadSingle();

        }
    }
}
