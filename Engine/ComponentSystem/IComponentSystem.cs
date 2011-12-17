using System;
using Engine.Serialization;
using Engine.Simulation;

namespace Engine.ComponentSystem
{
    public interface IComponentSystem<TPlayerData> : ICloneable
        where TPlayerData : IPacketizable<TPlayerData>
    {
        void Update(IEntity<TPlayerData> entity);
    }
}
