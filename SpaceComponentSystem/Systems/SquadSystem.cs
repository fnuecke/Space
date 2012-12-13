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
    }
}
