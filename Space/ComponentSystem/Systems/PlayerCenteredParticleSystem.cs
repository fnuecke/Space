using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Makes sure particles are rendered based on the player's position.
    /// </summary>
    public class PlayerCenteredParticleSystem : ParticleSystem
    {
        #region Fields

        /// <summary>
        /// The graphics device we use (to get width and height of screen).
        /// </summary>
        private readonly IGraphicsDeviceService _graphics;

        /// <summary>
        /// The session we're currently in (to get player position).
        /// </summary>
        private readonly IClientSession _session;

        #endregion

        #region Constructor

        public PlayerCenteredParticleSystem(ContentManager content, IGraphicsDeviceService graphics, IClientSession session)
            : base(content, graphics)
        {
            _graphics = graphics;
            _session = session;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Override in subclasses for specific translation of the view.
        /// </summary>
        /// <returns>The translation of the view to use when rendering.</returns>
        protected override Vector3 GetTranslation()
        {
            var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
            if (avatar != null)
            {
                var transform = (Vector2)avatar.GetComponent<Transform>().Translation;
                Vector3 result;
                result.X = _graphics.GraphicsDevice.Viewport.Width / 2 - transform.X;
                result.Y = _graphics.GraphicsDevice.Viewport.Height / 2 - transform.Y;
                result.Z = 0;
                return result;
            }
            return Vector3.Zero;
        }

        #endregion
    }
}
