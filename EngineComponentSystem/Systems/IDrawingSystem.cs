namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Interface for presentation implementing systems.
    /// </summary>
    public interface IDrawingSystem
    {
        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        void Draw(long frame);
    }
}
