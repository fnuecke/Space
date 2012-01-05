using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Parameterization for sound components / sound system.
    /// </summary>
    public sealed class SoundParameterization
    {
        /// <summary>
        /// The name of the sound queue that should be played. Leave untouched if
        /// no sound is to be played.
        /// </summary>
        public List<string> SoundCues = new List<string>();

        /// <summary>
        /// The source position of the sound's source / emitter.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// The velocity of the sound's source / emitter.
        /// </summary>
        public Vector2 Velocity;
    }
}
