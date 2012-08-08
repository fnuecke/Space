using System;
using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a render system which always translates the view to be
    /// centered to the camera.
    /// </summary>
    public sealed class CameraCenteredTextureRenderSystem : TextureRenderSystem
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraCenteredTextureRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="speed">A function getting the speed of the simulation.</param>
        public CameraCenteredTextureRenderSystem(ContentManager content, SpriteBatch spriteBatch, Func<float> speed)
            : base(content, spriteBatch, speed)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        protected override FarRectangle ComputeViewport()
        {
            return ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).ComputeVisibleBounds(SpriteBatch.GraphicsDevice.Viewport);
        }

        /// <summary>
        /// Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected override FarTransform GetTransform()
        {
            return ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        #endregion
    }
}
