using Engine.ComponentSystem.Components;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    ///     Sent to notify others that the rotation of a component has changed.
    /// </summary>
    /// <remarks>
    ///     This message is triggered by the TranslationSystem, which executes synchronously, meaning it's safe to manipulate
    ///     the system in handlers for this message.
    /// </remarks>
    public struct RotationChanged
    {
        /// <summary>The component for which the rotation changed.</summary>
        public Component Component;

        /// <summary>The previous rotation before the change.</summary>
        public float PreviousRotation;

        /// <summary>The current rotation after the change.</summary>
        public float CurrentRotation;
    }
}