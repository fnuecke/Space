using System.Collections.Generic;
using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem
{
    public abstract class AbstractComponentSystem<TUpdateParameterization>
        : List<IComponent<TUpdateParameterization>>, IComponentSystem
    {
        protected TUpdateParameterization parameterization;

        protected AbstractComponentSystem(TUpdateParameterization parameterization)
        {
            this.parameterization = parameterization;
        }

        protected AbstractComponentSystem()
        {
        }

        public virtual void Update()
        {
            foreach (var item in this)
            {
                item.Update(parameterization);
            }
        }
    }
}
