using System;
using Engine.ComponentSystem.Entities;

namespace Engine.ComponentSystem.Systems
{
    public interface IComponentSystem : ICloneable
    {
        void Update(IEntity entity);
    }
}
