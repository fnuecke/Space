using Engine.ComponentSystem.Components;

namespace Space.ComponentSystem.Components.Logic
{
    /// <summary>
    /// This component does nothing, it just marks an entity for removal on
    /// cell death.
    /// </summary>
    public sealed class CellDeath : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = Engine.ComponentSystem.Manager.GetComponentTypeId(typeof(CellDeath));

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion
    }
}
