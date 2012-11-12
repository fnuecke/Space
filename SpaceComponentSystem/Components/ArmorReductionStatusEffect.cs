using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    public sealed class ArmorReductionStatusEffect : AttributeStatusEffect<AttributeType>
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

        #region Initialization

        /// <summary>
        /// Initializes the specified amount.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public ArmorReductionStatusEffect Initialize(float amount, AttributeType type)
        {
            base.Initialize(new AttributeModifier<AttributeType>(type, amount));

            return this;
        }

        #endregion
    }
}
