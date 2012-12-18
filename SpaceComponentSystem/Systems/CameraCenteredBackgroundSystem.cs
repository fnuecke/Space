using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system uses the camera's offset to render the background.
    /// </summary>
    public sealed class CameraCenteredBackgroundSystem : BackgroundRenderSystem
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraCenteredBackgroundSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device.</param>
        public CameraCenteredBackgroundSystem(ContentManager content, GraphicsDevice graphics)
            : base(content, graphics)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>
        /// The transformation.
        /// </returns>
        protected override FarTransform GetTransform()
        {
            return ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        #endregion
    }
}
