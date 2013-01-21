using System;
using Engine.FarMath;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Defines a particle system which always translates the view to be centered to the camera.</summary>
    public sealed class CameraCenteredParticleEffectSystem : ParticleEffectSystem
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="CameraCenteredParticleEffectSystem"/> class.
        /// </summary>
        /// <param name="simulationSpeed">A function getting the current simulation framerate.</param>
        public CameraCenteredParticleEffectSystem(Func<float> simulationSpeed)
            : base(simulationSpeed) {}

        #endregion

        #region Logic
        
        /// <summary>
        ///     Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected override Matrix GetTransform()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        /// <summary>
        ///     Returns the <em>translation</em> for globally offsetting rendered content.
        /// </summary>
        /// <returns>The translation.</returns>
        protected override FarPosition GetTranslation()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Translation;
        }

        #endregion
    }
}