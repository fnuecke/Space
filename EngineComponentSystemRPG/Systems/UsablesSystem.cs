using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    /// <summary>
    /// Allows triggering usable items.
    /// </summary>
    /// <typeparam name="TAction">Possible actions taken upon activation.</typeparam>
    public abstract class UsablesSystem<TAction> : AbstractComponentSystem<Usable<TAction>>
        where TAction : struct
    {
        /// <summary>
        /// Activates the specified usable.
        /// </summary>
        /// <param name="usable">The usable.</param>
        public void Use(Usable<TAction> usable)
        {
            if (usable.Enabled)
            {
                Activate(usable.Response, usable.Entity);
            }
        }

        /// <summary>
        /// Handle execution of a specific action.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="entity">The item that triggered the action.</param>
        protected abstract void Activate(TAction action, int entity);
    }
}
