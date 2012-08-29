using Engine.ComponentSystem.RPG.Components;

namespace Engine.ComponentSystem.RPG.Messages
{
    /// <summary>
    /// A message notifying of changes in the current level of experience.
    /// This is sent on either level up or down.
    /// </summary>
    public struct ExperienceLevelChanged
    {
        /// <summary>
        /// The component that triggered the message.
        /// </summary>
        public Experience Component;

        /// <summary>
        /// The previous level.
        /// </summary>
        public int OldLevel;

        /// <summary>
        /// The new level.
        /// </summary>
        public int NewLevel;
    }
}
