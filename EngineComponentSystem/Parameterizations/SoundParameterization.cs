﻿using System.Collections.Generic;
using Engine.Math;

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
        public List<string> SoundCues { get; set; }

        /// <summary>
        /// The source position of the sound's source / emitter.
        /// </summary>
        public FPoint Position { get; set; }

        /// <summary>
        /// The velocity of the sound's source / emitter.
        /// </summary>
        public FPoint Velocity { get; set; }

        public SoundParameterization()
        {
            this.SoundCues = new List<string>();
        }
    }
}