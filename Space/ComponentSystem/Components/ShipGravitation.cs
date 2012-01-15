using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Specialized gravitation implementation that dynamically computes the
    /// mass for the ship it is assigned to.
    /// </summary>
    public sealed class ShipGravitation : Gravitation
    {
        #region Constructor

        public ShipGravitation()
            : base(Gravitation.GravitationTypes.Attractee)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Test for change in equipment.
        /// </summary>
        /// <param name="message">Handles module added / removed messages.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is ModuleAdded<EntityAttributeType> || message is ModuleRemoved<EntityAttributeType>)
            {
                // Module removed or added, recompute our values.
                var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

                // Get the mass of the ship and return it.
                Mass = System.Math.Max(1, modules.GetValue(EntityAttributeType.Mass));
            }
        }

        #endregion
    }
}
