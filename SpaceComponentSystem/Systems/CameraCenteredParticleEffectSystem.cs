using Engine.ComponentSystem.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a particle system which always translates the view to be
    /// centered to the camera.
    /// </summary>
    public sealed class CameraCenteredParticleEffectSystem : ParticleEffectSystem
    {
        #region Constructor

        public CameraCenteredParticleEffectSystem(Game game, IGraphicsDeviceService graphics)
            : base(game, graphics)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Returns the <em>transformation</em> for rendered content.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        protected override Matrix GetTransform()
        {
            return Manager.GetSystem<CameraSystem>().GetTransformation();
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Receives the specified message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            if (message is Depacketized)
            {
                // Manually look up any instance belonging to our system. This
                // is necessary because we don't want visual/sound systems on
                // pure servers, so they won't tell us which components we need.
                // Note that this will be unnecessary work in case we got the
                // game state from a client/server hybrid, but it won't happen
                // often (ideally once per session).
                RebuildComponentList();
            }

            base.Receive(ref message);
        }

        #endregion
    }
}
