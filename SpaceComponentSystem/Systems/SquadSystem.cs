using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Cleans up squads if a squad component is removed.
    /// </summary>
    public sealed class SquadSystem : AbstractSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Fields



        #endregion

        #region Logic

        /// <summary>
        /// Called when a component is removed.
        /// </summary>
        /// <param name="component">The component.</param>
        public override void OnComponentRemoved(Component component)
        {
            var cc = component as Squad;
            if (cc != null)
            {
                cc.RemoveMember(component.Entity);
            }
        }

        #endregion
    }
}
