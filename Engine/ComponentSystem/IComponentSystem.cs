using System;
using Engine.Simulation;

namespace Engine.ComponentSystem
{
    public interface IComponentSystem : ICloneable
    {
        void Update(IEntity entity);
    }
}
