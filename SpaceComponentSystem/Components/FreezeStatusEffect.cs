using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// This effect freezes an entity, taking away its ability to accelerate, rotate and shoot.
    /// </summary>
    public sealed class FreezeStatusEffect : AttributeStatusEffect<AttributeType>
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

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ColdSlowStatusEffect"/> class.
        /// </summary>
        public FreezeStatusEffect()
        {
            // Disable acceleration and rotation.
            Modifiers.Add(new AttributeModifier<AttributeType>(AttributeType.AccelerationForce,
                0, AttributeComputationType.Multiplicative));
            Modifiers.Add(new AttributeModifier<AttributeType>(AttributeType.RotationForce,
                0, AttributeComputationType.Multiplicative));
            // Also disable weapons. We do this by just making it incredibly expensive to shoot ;)
            Modifiers.Add(new AttributeModifier<AttributeType>(AttributeType.WeaponEnergyConsumption,
                float.PositiveInfinity, AttributeComputationType.Multiplicative));
        }

        #endregion
    }
}
