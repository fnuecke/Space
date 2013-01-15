using Engine.ComponentSystem.Spatial.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;

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
        protected override Matrix GetTransform()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        /// <summary>
        ///     Returns the camera <em>translation</em> of globally offsetting the rendered content.
        /// </summary>
        /// <returns>The translation.</returns>
        protected override FarPosition GetTranslation()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Translation;
        }

        #endregion
    }
}