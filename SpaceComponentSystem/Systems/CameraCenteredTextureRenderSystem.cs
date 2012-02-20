using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a render system which always translates the view to be centered to the local player's avatar.
    /// </summary>
    public sealed class CameraCenteredTextureRenderSystem : TextureRenderSystem
    {
        #region Fields

        /// <summary>
        /// The game this system belongs to.
        /// </summary>
        private readonly Game _game;

        #endregion

        #region Constructor
        
        public CameraCenteredTextureRenderSystem(Game game, SpriteBatch spriteBatch)
            : base(game.Content, spriteBatch)
        {
            _game = game;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns a translation that is the negative player avatar position, making the local player centered.
        /// </summary>
        protected override Vector3 GetTranslation()
        {
            // Get viewport, to center objects around the camera position.
            var translation = Manager.GetSystem<CameraSystem>().GetTranslation();

            // Return the *negative* camera position, because that's the
            // actual amount we need to translate game objects to be drawn
            // at the correct position.
            Vector3 result;
            result.X = translation.X;
            result.Y = translation.Y;
            result.Z = 0;
            return result;
        }

        #endregion
    }
}
