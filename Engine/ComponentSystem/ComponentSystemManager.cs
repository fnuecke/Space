using System.Collections.Generic;

namespace Engine.ComponentSystem
{
    public class CompositeComponentSystem
        : List<IComponentSystem>, IComponentSystem
    {
        public void Update()
        {
            foreach (var item in this)
            {
                item.Update();
            }
        }
    }
}
