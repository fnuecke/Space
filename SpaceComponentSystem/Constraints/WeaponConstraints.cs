using Engine.ComponentSystem.Entities;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Data;

namespace Space.ComponentSystem.Constraints
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
        /// Samples a new sensor based on these constraints.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled sensor.</returns>
        public Entity SampleSensor(IUniformRandom random)
        {
            var entity = new Entity();

            entity.AddComponent(new Weapon(Texture, Sound, SampleCooldown(random), SampleEnergyConsumption(random), SampleDamage(random), Projectiles));

            return SampleAttributes(entity, random);
        }

        /// <summary>
        /// Samples the cooldown of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled cooldown.</returns>
        private int SampleCooldown(IUniformRandom random)
        {
            return (int)((MinCooldown + (float)random.NextDouble() * (MaxCooldown - MinCooldown)) * 60f);
        }

        /// <summary>
        /// Samples the energy consumption of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled energy consumption.</returns>
        private float SampleEnergyConsumption(IUniformRandom random)
        {
            return MinEnergyConsumption + (float)random.NextDouble() * (MaxEnergyConsumption - MinEnergyConsumption);
        }

        /// <summary>
        /// Samples the damage of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled damage.</returns>
        private float SampleDamage(IUniformRandom random)
        {
            return MinDamage + (float)random.NextDouble() * (MaxDamage - MinDamage);
        }

        #endregion
    }
}
