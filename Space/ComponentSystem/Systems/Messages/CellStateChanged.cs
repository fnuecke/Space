namespace Space.ComponentSystem.Systems.Messages
{
    /// <summary>
    /// Message dispatched when a cells state changes.
    /// </summary>
    public struct CellStateChanged
    {
        /// <summary>
        /// The x coordinate of the cell that changed its state.
        /// </summary>
        public int X;

        /// <summary>
        /// The y coordinate of the cell that changed its state.
        /// </summary>
        public int Y;

        /// <summary>
        /// The id of the cell that changed. This id is calculated from the
        /// coordinates, so it is guaranteed to always be the same for a
        /// given pair (X,Y).
        /// </summary>
        public ulong Id;

        /// <summary>
        /// The new state of the cell (active: <c>true</c>, inactive: <c>false</c>).
        /// </summary>
        public bool State;

        public static CellStateChanged Create(int x, int y, ulong id, bool state)
        {
            CellStateChanged result;
            result.X = x;
            result.Y = y;
            result.Id = id;
            result.State = state;
            return result;
        }
    }
}
