using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Base class for attributes attached to items.
    /// </summary>
    /// <typeparam name="TAttribute">The enum that holds the possible types of
    /// attributes.</typeparam>
    public sealed class Attribute<TAttribute> : Component
        where TAttribute : struct
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
        /// The actual attribute modifier which is applied.
        /// </summary>
        public readonly AttributeModifier<TAttribute> Value = new AttributeModifier<TAttribute>();

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            ((Attribute<TAttribute>)other).Value.CopyInto(Value);

            return this;
        }

        /// <summary>
        /// Initialize with the specified modifier.
        /// </summary>
        /// <param name="value">The value.</param>
        public Attribute<TAttribute> Initialize(AttributeModifier<TAttribute> value)
        {
            value.CopyInto(Value);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Value.Type = default(TAttribute);
            Value.Value = 0;
            Value.ComputationType = AttributeComputationType.Additive;
        }   
          
        #endregion
    }
}
