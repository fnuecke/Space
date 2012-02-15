using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Tracks what items a unit may drop on death, via the item pool id to
    /// draw items from.
    /// </summary>
    public class Drops : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The logical name of the item pool to draw items from when the unit
        /// dies.
        /// </summary>
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
                if (transform != null)
                {
                    Entity.Manager.SystemManager.GetSystem<DropSystem>().Drop(ItemPool, ref transform.Translation);
                }
            }
        }

        #endregion
    }
}
