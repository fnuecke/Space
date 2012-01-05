using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Used to keep update up to date when positions of entities change.
    /// </summary>
    public sealed class IndexParameterization
    {
        /// <summary>
        /// The groups the index component belongs to.
        /// </summary>
        public ulong IndexGroups;

        /// <summary>
        /// Set in index components to indicate their entities position has
        /// changed since the last update, meaning the index has to be
        /// validated.
        /// </summary>
        public bool PositionChanged;

        /// <summary>
        /// The position of the index component's entity before the change.
        /// </summary>
        public Vector2 PreviousPosition;
    }
}
