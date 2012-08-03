using Engine.FarMath;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Sent by the <c>Transform</c> component, to notify others that the
    /// translation has changed.
    /// </summary>
    /// <remarks>
    /// This message is sent by the TranslationSystem, which executes synchronously,
    /// meaning it's safe to manipulate the system in handlers for this message.
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
        public FarPosition PreviousPosition;

        /// <summary>
        /// The current translation after the change.
        /// </summary>
        public FarPosition CurrentPosition;
    }
}
