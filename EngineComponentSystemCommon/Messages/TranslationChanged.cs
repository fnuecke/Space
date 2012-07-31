using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Sent by the <c>Transform</c> component, to notify others that the
    /// translation has changed.
    /// </summary>
    /// <remarks>
    /// This message is sent by some systems with parallel execution, meaning
    /// receivers should take of locking as necessary.
    /// </remarks>
    public struct TranslationChanged
    {
        /// <summary>
        /// The entity for which the translation changed.
        /// </summary>
        public int Entity;

        /// <summary>
        /// The previous translation before the change.
        /// </summary>
        public Vector2 PreviousPosition;

        /// <summary>
        /// The current translation after the change.
        /// </summary>
        public Vector2 CurrentPosition;
    }
}
