using System;
using Engine.Serialization;
using Engine.Simulation;

namespace Engine.ComponentSystem
{
    public class ComponentSystem<TPlayerData, TUpdateParameterization> : IComponentSystem<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
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

        public virtual void Update(IEntity<TPlayerData> entity)
        {
            foreach (var item in entity.Components)
            {
                item.Update(entity, parameterization);
            }
        }

        public virtual object Clone()
        {
            return new ComponentSystem<TPlayerData, TUpdateParameterization>((TUpdateParameterization)parameterization.Clone());
        }
    }
}
