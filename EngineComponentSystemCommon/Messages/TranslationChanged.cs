using Engine.ComponentSystem.Common.Components;
using Engine.FarMath;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    ///     Sent to notify others that the translation of a component has changed.
    /// </summary>
    /// <remarks>
    ///     This message is sent by the TranslationSystem, which executes synchronously, meaning it's safe to manipulate
    ///     the system in handlers for this message.
    /// </remarks>
    public struct TranslationChanged
    {
        /// <summary>The component for which the translation changed.</summary>
        public IIndexable Component;

        /// <summary>The previous translation before the change.</summary>
        public FarPosition PreviousPosition;

        /// <summary>The current translation after the change.</summary>
        public FarPosition CurrentPosition;
    }
}