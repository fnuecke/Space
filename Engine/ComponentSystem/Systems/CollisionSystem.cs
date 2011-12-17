using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    public class CollisionSystem : AbstractComponentSystem<CollisionParameterization>
    {
        public override void Update()
        {
            foreach (var collidable in components)
            {
                // TODO
            }
        }
    }
}
