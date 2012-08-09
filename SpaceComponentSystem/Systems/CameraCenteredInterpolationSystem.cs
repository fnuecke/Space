using System;
using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Provides entity position and rotation interpolation of entities in the currently
    /// visible area of the simulation, based on camera position.
    /// </summary>
    public sealed class CameraCenteredInterpolationSystem : InterpolationSystem
    {
        #region Fields

        /// <summary>
        /// The graphics device we render to, to get the viewport.
        /// </summary>
        private readonly GraphicsDevice _graphics;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraCenteredTextureRenderSystem"/> class.
        /// </summary>
        /// <param name="graphics">The graphics device we render to (to get the viewport).</param>
        /// <param name="speed">A function getting the speed of the simulation.</param>
        /// <param name="renderFps">The frames per second we render.</param>
        /// <param name="simulationFps">The frames per second the simulation is updated.</param>
        public CameraCenteredInterpolationSystem(GraphicsDevice graphics, Func<float> speed, float renderFps, float simulationFps)
            : base(speed, renderFps, simulationFps)
        {
            _graphics = graphics;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        protected override FarRectangle ComputeViewport()
        {
            return ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).ComputeVisibleBounds(_graphics.Viewport);
        }

        #endregion
    }
}
