using System;
using Engine.Simulation;

namespace Engine.ComponentSystem
{
    public class ComponentSystem<TUpdateParameterization> : IComponentSystem
        where TUpdateParameterization : ICloneable
    {
        protected TUpdateParameterization parameterization;

        protected ComponentSystem(TUpdateParameterization parameterization)
        {
            this.parameterization = parameterization;
        }

        protected ComponentSystem()
        {
        }

        public virtual void Update(IEntity entity)
        {
            foreach (var item in entity.Components)
            {
                item.Update(parameterization);
            }
        }

        public virtual object Clone()
        {
            return new ComponentSystem<TUpdateParameterization>((TUpdateParameterization)parameterization.Clone());
        }
    }
}
