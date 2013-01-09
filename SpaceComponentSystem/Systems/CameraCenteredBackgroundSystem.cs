using Engine.ComponentSystem.Common.Systems;
using Engine.FarMath;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system uses the camera's offset to render the background.</summary>
    public sealed class CameraCenteredBackgroundSystem : BackgroundRenderSystem
    {
        #region Logic

        /// <summary>
        ///     Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected override FarTransform GetTransform()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        #endregion
    }
}