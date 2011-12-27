using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Used to keep update up to date when positions of entities change.
    /// </summary>
    public sealed class IndexParameterization
    {
        /// <summary>
        /// Set in index components to indicate their entities position has
        /// changed since the last update, meaning the index has to be
        /// validated.
        /// </summary>
        public bool PositionChanged { get; set; }

        /// <summary>
        /// The position of the index component's entity before the change.
        /// </summary>
        public Vector2 PreviousPosition { get; set; }
    }
}
