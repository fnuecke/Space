using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a render system which always translates the view to be
    /// centered to the camera.
    /// </summary>
    public class CameraCenteredTextureRenderSystem : TextureRenderSystem
    {
        #region Constructor
        
        public CameraCenteredTextureRenderSystem(ContentManager content, SpriteBatch spriteBatch)
            : base(content, spriteBatch)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the <em>translation</em> for offsetting rendered content.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        protected override Vector2 GetTranslation()
        {
            var translation = Manager.GetSystem<CameraSystem>().GetTranslation();

            Vector2 result;
            result.X = translation.X;
            result.Y = translation.Y;
            return result;
        }

        #endregion
    }
}
