using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating shields.
    /// </summary>
    public sealed class ShieldFactory : ItemFactory
    {
        #region Properties

        /// <summary>
        /// Gets or sets the coverage of the shield.
        /// </summary>
        /// <value>
        /// The coverage.
        /// </value>
        [Category("Logic")]
        [Description("The coverage of this shield, as a percentage. A value of 0 means the shield will cover zero degrees, 1 means it'll cover 360 degrees.")]
        public FloatInterval Coverage
        {
            get { return _coverage; }
            set { _coverage = value; }
        }

        #endregion

        #region Backing fields

        private FloatInterval _coverage = FloatInterval.Zero;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new shield based on these constraints.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled shield.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            var coverage = SampleCoverage(random);

            manager.AddComponent<Shield>(entity)
                .Initialize(coverage)
                .Initialize(Name, Icon, Quality, RequiredSlotSize, ModelOffset, ModelBelowParent);

            return SampleAttributes(manager, entity, random);
        }

        /// <summary>
        /// Samples the coverage.
        /// </summary>
        /// <param name="random">The randomizer to use.</param>
        /// <returns></returns>
        private float SampleCoverage(IUniformRandom random)
        {
            return (random == null) ? _coverage.Low
                : MathHelper.Lerp(_coverage.Low, _coverage.High, (float)random.NextDouble());
        }

        #endregion
    }
}
