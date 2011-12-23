using Engine.Data;
using Engine.Serialization;

namespace Space.Data
{
    /// <summary>
    /// Represents a single weapon item.
    /// </summary>
    public class WeaponModule : AbstractEntityModule<EntityAttributeType>
    {
        /// <summary>
        /// The texture used to render this weapon.
        /// </summary>
        public string Texture;

        /// <summary>
        /// The sound this weapon emits when firing.
        /// </summary>
        public string Sound;

        /// <summary>
        /// The cooldown time to wait between shots.
        /// </summary>
        public int Cooldown;

        /// <summary>
        /// The projectiles this weapon fires.
        /// </summary>
        public ProjectileData[] Projectiles = new ProjectileData[0];

        #region Serialization / Hashing / Cloning

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

        #endregion
    }
}
