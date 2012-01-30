using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.Util;
using Space.ComponentSystem.Messages;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components
{
    class CellChangedComponent:AbstractComponent
    {
       
        private ulong prevCellID;
        public bool PositionChanged;
        /// <summary>
        /// Uses <c>TranslationChanged</c> messages to set our changed flag.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is TranslationChanged)
            {
               
                if (!PositionChanged)
                {
                    var previousPosition = ((TranslationChanged)(ValueType)message).PreviousPosition;
                    prevCellID = CoordinateIds.Combine(
                   (int)previousPosition.X >> CellSystem.CellSizeShiftAmount,
                   (int)previousPosition.Y >> CellSystem.CellSizeShiftAmount);
                }
                PositionChanged = true;
                var position = Entity.GetComponent<Transform>().Translation;
                var cellId = CoordinateIds.Combine(
                   (int)position.X >> CellSystem.CellSizeShiftAmount,
                   (int)position.Y >> CellSystem.CellSizeShiftAmount);
                if (cellId!= prevCellID)
                {
                    EntityChangedCell m = new EntityChangedCell();
                    m.NewCellID = cellId;
                    m.OldCellID = prevCellID;
                    m.EntityID = Entity.UID;
                    Entity.SendMessage<EntityChangedCell>(ref m);
                    
                }
            }
        }
    }
}
