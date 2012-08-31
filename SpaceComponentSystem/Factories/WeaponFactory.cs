using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating weapons.
    /// </summary>
    public sealed class WeaponFactory : ItemFactory
    {
        #region Properties

        /// <summary>
        /// The sound this weapon emits when firing.
        /// </summary>
        [Category("Media")]
        [Editor("Space.Tools.DataEditor.SoundAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Description("The cue name of the sound to play when the weapon fires its projectiles.")]
        public string Sound
        {
            get { return _sound; }
            set { _sound = value; }
        }

        /// <summary>
        /// The cooldown time to wait between shots, in seconds.
        /// </summary>
        [Category("Logic")]
        [Description("The time to wait between firing shots from this weapon, in seconds.")]
        public FloatInterval Cooldown
        {
            get { return _cooldown; }
            set { _cooldown = value; }
        }

        /// <summary>
        /// The energy this weapon consumes per shot.
        /// </summary>
        [Category("Logic")]
        [Description("The amount of energy this weapon needs to fire one round of projectiles.")]
        public FloatInterval EnergyConsumption
        {
            get { return _energyConsumption; }
            set { _energyConsumption = value; }
        }

        /// <summary>
        /// The damage this weapon's projectiles inflict.
        /// </summary>
        [Category("Logic")]
        [Description("The damage each single projectile fired from this weapon does when hitting an enemy.")]
        public FloatInterval Damage
        {
            get { return _damage; }
            set { _damage = value; }
        }

        /// <summary>
        /// Possible projectiles this weapon fires.
        /// </summary>
        [Category("Logic")]
        [Description("The list of projectiles to emit each time this weapon is fired.")]
        public ProjectileFactory[] Projectiles
        {
            get { return _projectiles; }
            set { _projectiles = value; }
        }

        #endregion

        #region Backing fields

        private string _sound;

        private FloatInterval _cooldown = FloatInterval.Zero;

        private FloatInterval _energyConsumption = FloatInterval.Zero;

        private FloatInterval _damage = FloatInterval.Zero;

        private ProjectileFactory[] _projectiles;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new weapon based on these constraints.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled weapon.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Weapon>(entity).Initialize(Name, Icon, Quality, RequiredSlotSize, ModelOffset, ModelBelowParent, _sound, SampleCooldown(random), SampleEnergyConsumption(random), SampleDamage(random), _projectiles);

            return SampleAttributes(manager, entity, random);
        }

        /// <summary>
        /// Samples the cooldown of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled cooldown.</returns>
        private float SampleCooldown(IUniformRandom random)
        {
            return (random == null) ? _cooldown.Low
                : MathHelper.Lerp(_cooldown.Low, _cooldown.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the energy consumption of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled energy consumption.</returns>
        private float SampleEnergyConsumption(IUniformRandom random)
        {
            return (random == null) ? _energyConsumption.Low
                : MathHelper.Lerp(_energyConsumption.Low, _energyConsumption.High, (float)random.NextDouble());
        }

        /// <summary>
        /// Samples the damage of this weapon.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled damage.</returns>
        private float SampleDamage(IUniformRandom random)
        {
            return (random == null) ? _damage.Low
                : MathHelper.Lerp(_damage.Low, _damage.High, (float)random.NextDouble());
        }

        #endregion
    }
}
