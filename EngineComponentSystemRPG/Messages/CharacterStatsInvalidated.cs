namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    /// Sent by <c>Character</c> when stats have changed.
    /// </summary>
    public struct CharacterStatsInvalidated
    {
        /// <summary>
        /// The entity for which the stats were invalidated.
        /// </summary>
        public int Entity;
    }
}
