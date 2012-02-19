using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.RPG.Systems
{
    public abstract class UsablesSystem<TResponse> : AbstractComponentSystem<Usable<TResponse>>
        where TResponse : struct
    {
        public void Use(Usable<TResponse> usable)
        {
            if (usable.Enabled)
            {
                Activate(usable.Response, usable.Entity);
            }
        }

        protected abstract void Activate(TResponse response, int entity)
        {
        }
    }
}
