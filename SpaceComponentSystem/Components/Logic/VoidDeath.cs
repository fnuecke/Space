using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Util;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Kills an entity if it wanders into an inactive cell.
    /// </summary>
    public sealed class VoidDeath : AbstractComponent
    {
        #region Fields
        
        /// <summary>
        /// The id of the cell the entity is currently in.
        /// </summary>
        private ulong _currentCellId;

        #endregion

        #region Messaging
        
        /// <summary>
        /// Uses <c>TranslationChanged</c> messages to update our current cell.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is TranslationChanged)
            {
                // Check our new cell after the position change.
                var position = Entity.GetComponent<Transform>().Translation;
                var cellId = CoordinateIds.Combine(
                    (int)position.X >> CellSystem.CellSizeShiftAmount,
                    (int)position.Y >> CellSystem.CellSizeShiftAmount);

                // If the cell changed, check if we're out of bounds.
                if (cellId != _currentCellId)
                {
                    if (Entity.Manager.SystemManager.GetSystem<CellSystem>().IsCellActive(cellId))
                    {
                        // Save the new cell id.
                        _currentCellId = cellId;
                    }
                    else
                    {
                        // Dead space, kill self.
                        Entity.Manager.RemoveEntity(Entity);
                    }
                }
            }
        }

        #endregion
    }
}
