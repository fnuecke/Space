using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components.Logic
{
    public class Drops : AbstractComponent
    {
        #region Fields
        public string ItemPool;
        #endregion

        #region Logic

        /// <summary>
        /// Handles the Messages and tells the DropSystem to Drop an Item if the entity is dead
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityDied)
            {
                var transform = Entity.GetComponent<Transform>();
                if(transform == null) return;
                Entity.Manager.SystemManager.GetSystem<DropSystem>().Drop(ItemPool,ref transform.Translation);
            }
        }
        #endregion
    }
}
