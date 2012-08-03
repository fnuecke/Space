using Engine.FarMath;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a particle system which always translates the view to be
    /// centered to the camera.
    /// </summary>
    public sealed class CameraCenteredParticleEffectSystem : ParticleEffectSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = Engine.ComponentSystem.Manager.GetSystemTypeId(typeof(CameraCenteredParticleEffectSystem));

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraCenteredParticleEffectSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics.</param>
        public CameraCenteredParticleEffectSystem(ContentManager content, IGraphicsDeviceService graphics)
            : base(content, graphics)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the <em>transformation</em> for rendered content.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        protected override FarTransform GetTransform()
        {
            return ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        #endregion
    }
}
