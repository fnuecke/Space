using Engine.ComponentSystem;

namespace Space.ComponentSystem.Messages
{
    public struct EntityChangedCell
    {
        /// <summary>
        /// The ID of the old Cell
        /// </summary>
        public ulong OldCellID;
        /// <summary>
        /// The ID of the new Cell
        /// </summary>
        public ulong NewCellID;

        /// <summary>
        /// The Id of the Entity which changed Cells
        /// </summary>
        public Entity Entity;
    }
}
