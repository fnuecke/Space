using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Systems
{
    public class CollisionSystem : AbstractComponentSystem<CollisionParameterization>
    {
        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            if (updateType != ComponentSystemUpdateType.Logic)
            {
                return;
            }

            foreach (var collidable in Components)
            {
                // TODO
            }
        }
    }
}
