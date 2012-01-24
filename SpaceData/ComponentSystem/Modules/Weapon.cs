using System;
using Engine.ComponentSystem.Modules;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Modules
{
    /// <summary>
    /// Represents a single weapon item.
    /// </summary>
    public sealed class Weapon : AbstractModule<SpaceModifier>
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

        public Weapon()
        {
            AddAttributeTypeToInvalidate(SpaceModifier.WeaponCooldown);
            AddAttributeTypeToInvalidate(SpaceModifier.WeaponEnergyConsumption);
        }

        #endregion

        #region Serialization / Hashing / Cloning

        /// <summary>
        /// Packetizes the specified packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns></returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Texture)
                .Write(Sound)
                .Write(Cooldown)
                .Write(EnergyConsumption)
                .Write(Projectiles);
        }

        /// <summary>
        /// Depacketizes the specified packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Texture = packet.ReadString();
            Sound = packet.ReadString();
            Cooldown = packet.ReadInt32();
            EnergyConsumption = packet.ReadSingle();
            Projectiles = packet.ReadPacketizables<ProjectileData>();
        }

        /// <summary>
        /// Hashes the specified hasher.
        /// </summary>
        /// <param name="hasher">The hasher.</param>
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

        public override AbstractModule<SpaceModifier> DeepCopy(AbstractModule<SpaceModifier> into)
        {
            var copy = (Weapon)base.DeepCopy(into);

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
