using System;
using Engine.FarMath;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Defines a particle system which always translates the view to be centered to the camera.</summary>
    public sealed class CameraCenteredParticleEffectSystem : ParticleEffectSystem
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="CameraCenteredParticleEffectSystem"/> class.
        /// </summary>
        /// <param name="simulationFps">A function getting the current simulation framerate.</param>
        public CameraCenteredParticleEffectSystem(Func<float> simulationFps)
            : base(simulationFps) {}

        #endregion

        #region Logic

        /// <summary>
        ///     Returns the <em>transformation</em> for rendered content.
        /// </summary>
        /// <returns>The translation.</returns>
        protected override FarTransform GetTransform()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        #endregion
    }
}