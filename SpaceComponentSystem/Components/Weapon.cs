﻿using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.ComponentSystem.Factories;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single weapon item.
    /// </summary>
    public sealed class Weapon : SpaceItem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

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
        public ProjectileFactory[] Projectiles;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherWeapon = (Weapon)other;
            Sound = otherWeapon.Sound;
            Cooldown = otherWeapon.Cooldown;
            EnergyConsumption = otherWeapon.EnergyConsumption;
            Damage = otherWeapon.Damage;
            Projectiles = otherWeapon.Projectiles;

            return this;
        }

        /// <summary>
        /// Creates a new weapon with the specified parameters.
        /// </summary>
        /// <param name="sound">The sound to play when the weapon is fired.</param>
        /// <param name="cooldown">The cooldown in ticks betweens shots.</param>
        /// <param name="energyConsumption">The amount of energy consumed per
        /// shot</param>
        /// <param name="damage">The amount of damage a single projectile does.</param>
        /// <param name="projectiles">The info on projectiles being shot.</param>
        /// <returns></returns>
        public Weapon Initialize(string sound, float cooldown, float energyConsumption,
                                 float damage, ProjectileFactory[] projectiles)
        {
            Sound = sound;
            Cooldown = cooldown;
            EnergyConsumption = energyConsumption;
            Damage = damage;
            Projectiles = projectiles;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Sound = null;
            Cooldown = 0;
            EnergyConsumption = 0;
            Damage = 0;
            Projectiles = null;
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

            Sound = packet.ReadString();
            Cooldown = packet.ReadSingle();
            EnergyConsumption = packet.ReadSingle();
            Damage = packet.ReadSingle();
            Projectiles = packet.ReadPacketizables<ProjectileFactory>();
        }

        /// <summary>
        /// Hashes the specified hasher.
        /// </summary>
        /// <param name="hasher">The hasher.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Sound);
            hasher.Put(Cooldown);
            hasher.Put(EnergyConsumption);
            hasher.Put(Damage);
            hasher.Put(Projectiles);
        }

        #endregion
    }
}
