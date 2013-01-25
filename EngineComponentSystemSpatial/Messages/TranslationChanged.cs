using Engine.ComponentSystem.Spatial.Components;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Spatial.Messages
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
        public ITransform Component;

        /// <summary>The previous translation before the change.</summary>
        public WorldPoint PreviousPosition;

        /// <summary>The current translation after the change.</summary>
        public WorldPoint CurrentPosition;
    }
}