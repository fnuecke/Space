using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>This effect slows down a unit.</summary>
    public sealed class ColdSlowStatusEffect : AttributeStatusEffect<AttributeType>
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="ColdSlowStatusEffect"/> class.
        /// </summary>
        public ColdSlowStatusEffect()
        {
            Modifiers.Add(
                new AttributeModifier<AttributeType>(
                    AttributeType.MaximumVelocity,
                    GameLogicConstants.ColdSlowMultiplier,
                    AttributeComputationType.Multiplicative));
            Modifiers.Add(
                new AttributeModifier<AttributeType>(
                    AttributeType.AccelerationForce,
                    GameLogicConstants.ColdSlowMultiplier,
                    AttributeComputationType.Multiplicative));
        }

        #endregion
    }
}