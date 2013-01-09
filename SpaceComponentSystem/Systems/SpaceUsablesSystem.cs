using Engine.ComponentSystem.RPG.Systems;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Implements usable item logic.</summary>
    public sealed class SpaceUsablesSystem : UsablesSystem<UsableResponse>
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Logic

        /// <summary>Provides implementations of logic for "usables", e.g. healing items.</summary>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        protected override void Activate(UsableResponse action, int entity) {}

        #endregion
    }
}