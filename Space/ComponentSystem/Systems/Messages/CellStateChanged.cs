
namespace Space.ComponentSystem.Systems.Messages
{
    public struct CellStateChanged
    {
        /// <summary>
        /// The x coordinate of the cell that changed its state.
        /// </summary>
        int X;

        /// <summary>
        /// The y coordinate of the cell that changed its state.
        /// </summary>
        int Y;

        /// <summary>
        /// The new state of the cell (active: <c>true</c>, inactive: <c>false</c>).
        /// </summary>
        bool State;

        public static CellStateChanged Create(int x, int y, bool state)
        {
            CellStateChanged result;
            result.X = x;
            result.Y = y;
            result.State = state;
            return result;
        }
    }
}
