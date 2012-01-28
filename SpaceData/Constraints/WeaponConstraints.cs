using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Constraints
{
    /// <summary>
    /// Constraints for generating weapons.
    /// </summary>
    public sealed class WeaponConstraints : ItemConstraints
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
        /// The minimum cooldown time to wait between shots, in seconds.
        /// </summary>
        public float MinCooldown;

        /// <summary>
        /// The maximum cooldown time to wait between shots, in seconds.
        /// </summary>
        public float MaxCooldown;

        /// <summary>
        /// The minimum energy this weapon consumes per shot.
        /// </summary>
        public float MinEnergyConsumption;

        /// <summary>
        /// The maximum energy this weapon consumes per shot.
        /// </summary>
        public float MaxEnergyConsumption;

        /// <summary>
        /// The minimum damage this weapon's projectiles inflict.
        /// </summary>
        public float MinDamage;

        /// <summary>
        /// The maximum damage this weapon's projectiles inflict.
        /// </summary>
        public float MaxDamage;

        /// <summary>
        /// Possible projectiles this weapon fires.
        /// </summary>
        public Projectile[] Projectiles;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples the cooldown of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled cooldown.</returns>
        public float SampleCooldown(IUniformRandom random)
        {
            return MinCooldown + (float)random.NextDouble() * (MaxCooldown - MinCooldown);
        }

        /// <summary>
        /// Samples the energy consumption of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled energy consumption.</returns>
        public float SampleEnergyConsumption(IUniformRandom random)
        {
            return MinEnergyConsumption + (float)random.NextDouble() * (MaxEnergyConsumption - MinEnergyConsumption);
        }

        /// <summary>
        /// Samples the damage of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled damage.</returns>
        public float SampleDamage(IUniformRandom random)
        {
            return MinDamage + (float)random.NextDouble() * (MaxDamage - MinDamage);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Texture)
                .Write(Sound)
                .Write(MinCooldown)
                .Write(MaxCooldown)
                .Write(MinEnergyConsumption)
                .Write(MaxEnergyConsumption)
                .Write(MinDamage)
                .Write(MaxDamage)
                .Write(Projectiles);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            Texture = packet.ReadString();
            Sound = packet.ReadString();
            MinCooldown = packet.ReadSingle();
            MaxCooldown = packet.ReadSingle();
            MinEnergyConsumption = packet.ReadSingle();
            MaxEnergyConsumption = packet.ReadSingle();
            MinDamage = packet.ReadSingle();
            MaxDamage = packet.ReadSingle();
            Projectiles = packet.ReadPacketizables<Projectile>();
        }

        #endregion
    }
}
