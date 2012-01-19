using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a render system which always translates the view to be centered to the local player's avatar.
    /// </summary>
    public class PlayerCenteredRenderSystem : ParticleSystem
    {
        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private IClientSession _session;

        #endregion

        public PlayerCenteredRenderSystem(Game game, SpriteBatch spriteBatch, IGraphicsDeviceService graphics, IClientSession session)
            : base(game, spriteBatch, graphics)
        {
            this._session = session;
        }

        /// <summary>
        /// Returns a translation that is the negative player avatar position, making the local player centered.
        /// </summary>
        protected override Vector3 GetTranslation()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            if (avatar != null)
            {
                var viewport = _parameterization.SpriteBatch.GraphicsDevice.Viewport;

                Vector2 tmp = -avatar.GetComponent<Transform>().Translation;
                Vector3 result;
                result.X = tmp.X + viewport.Width / 2;
                result.Y = tmp.Y + viewport.Height / 2;
                result.Z = 0;
                return result;
            }
            return Vector3.Zero;
        }
    }
}
