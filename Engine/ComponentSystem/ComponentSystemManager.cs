using System.Collections.Generic;
using Engine.Serialization;
using Engine.Simulation;

namespace Engine.ComponentSystem
{
    public class CompositeComponentSystem<TPlayerData>
        : List<IComponentSystem<TPlayerData>>, IComponentSystem<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        public void Update(IEntity<TPlayerData> entity)
        {
            foreach (var item in this)
            {
                item.Update(entity);
            }
        }

        public object Clone()
        {
            var copy = new CompositeComponentSystem<TPlayerData>();
            foreach (var item in this)
            {
                copy.Add((IComponentSystem<TPlayerData>)item.Clone());
            }
            return copy;
        }
    }
}
