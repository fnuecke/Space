using Engine.ComponentSystem.Spatial.Systems;
using Engine.FarMath;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    ///     Provides entity position and rotation interpolation of entities in the currently visible area of the
    ///     simulation, based on camera position.
    /// </summary>
    public sealed class CameraCenteredInterpolationSystem : InterpolationSystem
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="CameraCenteredTextureRenderSystem"/> class.
        /// </summary>
        public CameraCenteredInterpolationSystem()
            : base(Settings.TicksPerSecond) {}

        #endregion

        #region Logic

        /// <summary>Returns the current bounds of the viewport, i.e. the rectangle of the world to actually render.</summary>
        protected override FarRectangle ComputeViewport()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).ComputeVisibleBounds();
        }

        #endregion
    }
}