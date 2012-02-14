using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Util;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single weapon item.
    /// </summary>
    public sealed class Weapon : SpaceItem
    {
        #region Fields

        /// <summary>
        /// The texture used to render this weapon.
        /// </summary>
        public string ModelName;

        /// <summary>
        /// The sound this weapon emits when firing.
        /// </summary>
        public string Sound;

        /// <summary>
        /// The cooldown time to wait between shots, in seconds.
        /// </summary>
        public float Cooldown;

        /// <summary>
        /// The amount of energy this weapon requires for a single shot.
        /// </summary>
        public float EnergyConsumption;

        /// <summary>
        /// The amount of energy this weapon requires for a single shot.
        /// </summary>
        public float Damage;

        /// <summary>
        /// The projectiles this weapon fires.
        /// </summary>
        public ProjectileConstraints[] Projectiles;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new armor with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The item's quality level.</param>
        /// <param name="modelName">The texture used for rendering the weapon
        /// on the ship.</param>
        /// <param name="sound">The sound to play when the weapon is fired.</param>
        /// <param name="cooldown">The cooldown in ticks betweens shots.</param>
        /// <param name="energyConsumption">The amount of energy consumed per
        /// shot</param>
        /// <param name="damage">The amount of damage a single projectile does.</param>
        /// <param name="projectiles">The info on projectiles being shot.</param>
        public Weapon(string name, string iconName, ItemQuality quality,
            string modelName, string sound,
            float cooldown, float energyConsumption,
            float damage, ProjectileConstraints[] projectiles)
            : base(name, iconName, quality)
        {
            this.ModelName = modelName;
            this.Sound = sound;
            this.Cooldown = cooldown;
            this.EnergyConsumption = energyConsumption;
            this.Damage = damage;
            this.Projectiles = projectiles;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Weapon()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Puts item specific information into the given descripton object.
        /// </summary>
        /// <param name="descripton">The object to write the object information
        /// into.</param>
        public override void GetDescription(ref ItemDescription descripton)
        {
            base.GetDescription(ref descripton);
            
            descripton.IsWeapon = true;
            descripton.WeaponDamage = Damage;
            descripton.WeaponCooldown = Cooldown;
            descripton.WeaponEnergyConsumption = EnergyConsumption;
            descripton.WeaponProjectileCount = Projectiles.Length;
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
                .Write(ModelName)
                .Write(Sound)
                .Write(Cooldown)
                .Write(EnergyConsumption)
                .Write(Damage)
                .Write(Projectiles);
        }

        /// <summary>
        /// Depacketizes the specified packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            ModelName = packet.ReadString();
            Sound = packet.ReadString();
            Cooldown = packet.ReadSingle();
            EnergyConsumption = packet.ReadSingle();
            Damage = packet.ReadSingle();
            Projectiles = packet.ReadPacketizables<ProjectileConstraints>();
        }

        /// <summary>
        /// Hashes the specified hasher.
        /// </summary>
        /// <param name="hasher">The hasher.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Cooldown));
            hasher.Put(BitConverter.GetBytes(EnergyConsumption));
            hasher.Put(BitConverter.GetBytes(Damage));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Weapon)base.DeepCopy(into);

            if (copy == into)
            {
                // Copying into other instance.
                copy.ModelName = ModelName;
                copy.Sound = Sound;
                copy.Cooldown = Cooldown;
                copy.EnergyConsumption = EnergyConsumption;
                copy.Damage = Damage;
                copy.Projectiles = Projectiles;
            }

            return copy;
        }

        #endregion
    }
}
