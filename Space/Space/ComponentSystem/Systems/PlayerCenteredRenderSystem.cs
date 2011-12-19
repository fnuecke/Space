using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a render system which always translates the view to be centered to the local player's avatar.
    /// </summary>
    public class PlayerCenteredRenderSystem : RenderSystem
    {
        #region Fields
        
        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private IClientSession _session;

        #endregion

        public PlayerCenteredRenderSystem(SpriteBatch spriteBatch, ContentManager contentManager, IClientSession session)
            : base(spriteBatch, contentManager)
        {
            this._session = session;
        }

        /// <summary>
        /// Returns a translation that is the negative player avatar position, making the local player centered.
        /// </summary>
        protected override Vector2 GetTranslation()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            if (avatar != null)
            {
                return new Vector2(parameterization.SpriteBatch.GraphicsDevice.Viewport.Width / 2,
                                   parameterization.SpriteBatch.GraphicsDevice.Viewport.Height / 2)
                                   - (Vector2)avatar.GetComponent<StaticPhysics>().Position;
            }
            return Vector2.Zero;
        }
    }
}
