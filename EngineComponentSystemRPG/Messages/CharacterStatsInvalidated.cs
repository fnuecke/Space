namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    ///     Sent by <see cref="Components.Attributes{TAttribute}"/> when stats have changed.
    /// </summary>
    public struct CharacterStatsInvalidated
    {
        /// <summary>The entity for which the stats were invalidated.</summary>
        public int Entity;
    }
}