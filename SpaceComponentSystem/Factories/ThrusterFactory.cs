using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Constraints for generating thrusters.
    /// </summary>
    public sealed class ThrusterFactory : ItemFactory
    {
        #region Properties

        /// <summary>
        /// Asset name of the particle effect to trigger when this thruster is
        /// active (accelerating).
        /// </summary>
        [Editor("Space.Tools.DataEditor.EffectAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Media")]
        [Description("The asset name of the particle effect to use for this thruster when accelerating.")]
        public string Effect
        {
            get { return _effect; }
            set { _effect = value; }
        }

        /// <summary>
        /// Offset for the thruster effect relative to the texture.
        /// </summary>
        [ContentSerializer(Optional = true)]
        [DefaultValue(null)]
        [Category("Media")]
        [Description("The offset relative to the slot the item is equipped in at which to emit particle effects when accelerating.")]
        public Vector2? EffectOffset
        {
            get { return _effectOffset; }
            set { _effectOffset = value; }
        }

        #endregion

        #region Backing fields

        private string _effect;

        private Vector2? _effectOffset;

        #endregion

        #region Sampling

        /// <summary>
        /// Samples a new thruster based on these constraints.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled thruster.
        /// </returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Thruster>(entity).Initialize(Name, Icon, Quality, RequiredSlotSize,
                                                              ModelOffset.HasValue ? ModelOffset.Value : Vector2.Zero,
                                                              ModelBelowParent, _effect,
                                                              _effectOffset.HasValue
                                                                  ? _effectOffset.Value
                                                                  : Vector2.Zero);

            return SampleAttributes(manager, entity, random);
        }

        #endregion
    }
}
