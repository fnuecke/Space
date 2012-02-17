using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Util;
using Space.ComponentSystem.Messages;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Tracks the cell an entity currently resides in, dispatches messages
    /// when the cell changes.
    /// </summary>
    public sealed class CellId : AbstractComponent
    {
        #region Fields
        
        /// <summary>
        /// The id of the cell the entity is currently in.
        /// </summary>
        public ulong Value;

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

                // If the cell changed, send a message.
                if (cellId != Value)
                {
                    // Build our message and send it.
                    EntityChangedCell changedMessage = new EntityChangedCell();
                    changedMessage.OldCellID = Value;
                    changedMessage.NewCellID = cellId;
                    changedMessage.Entity = Entity;
                    Entity.Manager.SystemManager.SendMessage(ref changedMessage);

                    // Save the new cell id.
                    Value = cellId;
                }
            }
        }

        #endregion
    }
}
