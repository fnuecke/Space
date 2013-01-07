using System.Globalization;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components
{
    public sealed class ItemEffect : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

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
        /// The group to which the effect belongs.
        /// </summary>
        public ParticleEffects.EffectGroup Group;

        /// <summary>
        /// The name of the effect asset to use.
        /// </summary>
        public string Name;

        /// <summary>
        /// The scale at which to render.
        /// </summary>
        public float Scale;

        /// <summary>
        /// The offset at which to render the effect.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// The direction to emit the effect in, in radians.
        /// </summary>
        public float Direction;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherEffect = (ItemEffect)other;
            Group = otherEffect.Group;
            Name = otherEffect.Name;
            Scale = otherEffect.Scale;
            Offset = otherEffect.Offset;
            Direction = otherEffect.Direction;

            return this;
        }

        /// <summary>
        /// Initializes the component using the specified effect.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="effect">The effect.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public ItemEffect Initialize(ParticleEffects.EffectGroup group, string effect, float scale, Vector2 offset, float direction)
        {
            Group = group;
            Name = effect;
            Scale = scale;
            Offset = offset;
            Direction = direction;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Group = ParticleEffects.EffectGroup.None;
            Name = null;
            Scale = 0f;
            Offset = Vector2.Zero;
            Direction = 0f;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Group=" + Group + ", Name=" + Name + ", Scale=" + Scale.ToString(CultureInfo.InvariantCulture) +
                ", Offset=" + Offset.X.ToString(CultureInfo.InvariantCulture) + Offset.Y.ToString(CultureInfo.InvariantCulture) +
                ", Direction=" + Direction.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
