using Engine.Data;
using Engine.Serialization;

namespace Space.Data
{
    public class WeaponModule : AbstractEntityModule<EntityAttributeType>
    {
        public string Texture;

        public string Sound;

        public int Cooldown;

        public ProjectileData[] Projectiles = new ProjectileData[0];

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(Texture)
                .Write(Sound)
                .Write(Cooldown)
                .Write(Projectiles);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Texture = packet.ReadString();
            Sound = packet.ReadString();
            Cooldown = packet.ReadInt32();
            Projectiles = packet.ReadPacketizables<ProjectileData>();
        }

        public override object Clone()
        {
            var copy = (WeaponModule)base.Clone();
            copy.Projectiles = new ProjectileData[Projectiles.Length];
            Projectiles.CopyTo(copy.Projectiles, 0);
            return copy;
        }
    }
}
