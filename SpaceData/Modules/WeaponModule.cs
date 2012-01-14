using System;
using Engine.Data;
using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Modules
{
    /// <summary>
    /// Represents a single weapon item.
    /// </summary>
    public class WeaponModule : AbstractEntityModule<EntityAttributeType>
    {
        #region Fields
        
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
        /// The amount of energy this weapon requires for a single shot.
        /// </summary>
        public float EnergyConsumption;

        /// <summary>
        /// The projectiles this weapon fires.
        /// </summary>
        public ProjectileData[] Projectiles = new ProjectileData[0];

        #endregion

        #region Constructor

        public WeaponModule()
        {
            AddAttributeTypeToInvalidate(EntityAttributeType.WeaponCooldown);
            AddAttributeTypeToInvalidate(EntityAttributeType.WeaponEnergyConsumption);
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Texture)
                .Write(Sound)
                .Write(Cooldown)
                .Write(EnergyConsumption)
                .Write(Projectiles);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Texture = packet.ReadString();
            Sound = packet.ReadString();
            Cooldown = packet.ReadInt32();
            EnergyConsumption = packet.ReadSingle();
            Projectiles = packet.ReadPacketizables<ProjectileData>();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Cooldown));
            foreach (var projectile in Projectiles)
            {
                projectile.Hash(hasher);
            }
        }

        #endregion

        #region Copying

        public override AbstractEntityModule<EntityAttributeType> DeepCopy(AbstractEntityModule<EntityAttributeType> into)
        {
            var copy = (WeaponModule)base.DeepCopy(into is WeaponModule ? into : null);

            if (copy == into)
            {
                // Copying into other instance.
                copy.Texture = Texture;
                copy.Sound = Sound;
                copy.Cooldown = Cooldown;
                copy.EnergyConsumption = EnergyConsumption;
                if (copy.Projectiles.Length != Projectiles.Length)
                {
                    copy.Projectiles = new ProjectileData[Projectiles.Length];
                }
            }
            else
            {
                // Shallow copy, new instance for reference types.
                copy.Projectiles = new ProjectileData[Projectiles.Length];
            }
            Projectiles.CopyTo(copy.Projectiles, 0);

            return copy;
        }

        #endregion
    }
}
