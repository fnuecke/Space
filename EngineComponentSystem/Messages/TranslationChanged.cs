using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Messages
{
    /// <summary>
    /// Sent by the <c>Transform</c> component, to notify others that the
    /// translation has changed.
    /// </summary>
    public struct TranslationChanged
    {
        /// <summary>
        /// The previous translation.
        /// </summary>
        public Vector2 PreviousPosition;

        /// <summary>
        /// The entity of which the position changed.
        /// </summary>
        public Entity Entity;
    }
}
