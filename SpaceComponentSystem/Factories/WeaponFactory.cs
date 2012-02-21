using Engine.ComponentSystem;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Util;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating weapons.
    /// </summary>
    public sealed class WeaponFactory : ItemFactory
    {
        #region Fields

        /// <summary>
        /// The texture used to render this weapon when equipped on a ship.
        /// </summary>
        public string EquippedModel;

        /// <summary>
        /// The sound this weapon emits when firing.
        /// </summary>
        public string Sound;

        /// <summary>
        /// The cooldown time to wait between shots, in seconds.
        /// </summary>
        public Interval<float> Cooldown = Interval<float>.Zero;

        /// <summary>
        /// The energy this weapon consumes per shot.
        /// </summary>
        public Interval<float> EnergyConsumption = Interval<float>.Zero;

        /// <summary>
        /// The damage this weapon's projectiles inflict.
        /// </summary>
        public Interval<float> Damage = Interval<float>.Zero;

        /// <summary>
        /// Possible projectiles this weapon fires.
        /// </summary>
        public ProjectileFactory[] Projectiles;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new weapon based on these constraints.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled weapon.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Weapon>(entity).Initialize(Name, Icon, Quality, EquippedModel, Sound, SampleCooldown(random), SampleEnergyConsumption(random), SampleDamage(random), Projectiles);

            return SampleAttributes(manager, entity, random);
        }

        /// <summary>
        /// Samples the cooldown of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled cooldown.</returns>
        private float SampleCooldown(IUniformRandom random)
        {
            return (random == null) ? Cooldown.Low
                : MathHelper.Lerp(Cooldown.Low, Cooldown.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the energy consumption of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled energy consumption.</returns>
        private float SampleEnergyConsumption(IUniformRandom random)
        {
            return (random == null) ? EnergyConsumption.Low
                : MathHelper.Lerp(EnergyConsumption.Low, EnergyConsumption.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the damage of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled damage.</returns>
        private float SampleDamage(IUniformRandom random)
        {
            return (random == null) ? Damage.Low
                : MathHelper.Lerp(Damage.Low, Damage.High, (float)random.NextDouble());
        }

        #endregion
    }
}
