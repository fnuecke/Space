﻿using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Messages
{
    /// <summary>
    /// Sent by the <c>Transform</c> component, to notify others that the
    /// translation has changed.
    /// </summary>
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