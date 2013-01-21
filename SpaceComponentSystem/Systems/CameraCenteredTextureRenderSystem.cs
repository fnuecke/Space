using System.Collections.Generic;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.FarMath;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Defines a render system which always translates the view to be centered to the camera.</summary>
    public sealed class CameraCenteredTextureRenderSystem : TextureRenderSystem
    {
        #region Logic

        /// <summary>Gets the list of currently visible entities.</summary>
        /// <returns>The list of visible entities.</returns>
        protected override IEnumerable<int> GetVisibleEntities()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).VisibleEntities;
        }

        /// <summary>
        ///     Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected override Matrix GetTransform()
        {
            return Matrix.CreateScale(UnitConversion.ToScreenUnits(1f)) *
                   ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Transform;
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