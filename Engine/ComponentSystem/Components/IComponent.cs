using System;
using Engine.Serialization;
using Engine.Simulation;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    public interface IComponent<TPlayerData> : ICloneable, IHashable
        where TPlayerData : IPacketizable<TPlayerData>
    {
        void Update(IEntity<TPlayerData> entity, object parameterization);
    }
}
