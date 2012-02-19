using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a final death, i.e. when health reaches zero the entity will
    /// be removed from the simulation.
    /// </summary>
    public sealed class Death : Component
    {
        #region Logic

        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityDied)
            {
                Entity.Manager.AddEntity(EntityFactory.CreateExplosion(Entity.GetComponent<Transform>().Translation));

                Entity.Manager.RemoveEntity(Entity);
            }
        }

        #endregion
    }
}
