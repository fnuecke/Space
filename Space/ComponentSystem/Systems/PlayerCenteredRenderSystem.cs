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
        #region Properties

        /// <summary>
        /// The current camera position used in this renderer.
        /// </summary>
        public Vector2 CameraPositon { get { return _cameraPosition; } }

        #endregion

        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private IClientSession _session;

        /// <summary>
        /// The last frame we updated our camera offset (interpolated). We need
        /// this to avoid sped-up interpolation due to rollbacks.
        /// </summary>
        private long _lastFrame;

        /// <summary>
        /// Previous offset to the ship, use to slowly interpolate, giving a
        /// more organic feel.
        /// </summary>
        private Vector2 _previousOffset;

        /// <summary>
        /// The current camera position.
        /// </summary>
        private Vector2 _cameraPosition;

        /// <summary>
        /// Flag to tell if the current camera position was set from outside,
        /// or was dynamically computed.
        /// </summary>
        private bool _isCameraFixed;

        #endregion

        public PlayerCenteredRenderSystem(Game game, SpriteBatch spriteBatch, IGraphicsDeviceService graphics, IClientSession session)
            : base(game, spriteBatch, graphics)
        {
            this._session = session;
        }

        #region Accessors

        /// <summary>
        /// Sets the camera position this renderer should use. The camera
        /// position will no longer be computed dynamically, based on the
        /// local player's ship's position. To re-enable this behavior,
        /// pass <c>null</c>.
        /// </summary>
        /// <param name="cameraPosition"></param>
        public void SetCameraPosition(Vector2? cameraPosition)
        {
            if (cameraPosition.HasValue)
            {
                _cameraPosition = cameraPosition.Value;
                _isCameraFixed = true;
            }
            else
            {
                _isCameraFixed = false;
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Used to update the camera position. We don't do this in the draw,
        /// to make sure it's up-to-date before *anything* else is drawn,
        /// especially stuff outside the simulation, to avoid "lagging".
        /// </summary>
        /// <param name="frame">The frame the update applies to.</param>
        public override void Update(long frame)
        {
            base.Update(frame);

            // Only update the offset if we didn't roll-back.
            if (frame > _lastFrame)
            {
                if (!_isCameraFixed && _session.ConnectionState == ClientState.Connected)
                {
                    var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
                    if (avatar != null)
                    {
                        // Get viewport, get mouse position relative to center
                        // of screen.
                        var viewport = _parameterization.SpriteBatch.GraphicsDevice.Viewport;

                        // Non-fixed camera, update our offset based on the mouse
                        // position, relative to the ship.
                        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
                        var avatarPosition = avatar.GetComponent<Transform>().Translation;

                        // Get the relative position of the mouse to the ship and
                        // apply some factoring to it (so that the maximum distance
                        // of cursor to ship is not half the screen size).
                        Vector2 currentOffset;
                        currentOffset.X = (mouse.X - viewport.Width / 2f) / 3;
                        currentOffset.Y = (mouse.Y - viewport.Height / 2f) / 3;
                        // The interpolate to our new offset, slowly to make the
                        // effect less brain-melting.
                        _previousOffset = Vector2.Lerp(_previousOffset, currentOffset, 0.05f);

                        // The camera *position* is then the avatar position, plus
                        // the offset, correcting for the viewport center which was
                        // subtracted to make the mouse position's origin centered
                        // to the screen.
                        _cameraPosition = avatarPosition + _previousOffset;
                    }
                }

                _lastFrame = frame;
            }
        }

        /// <summary>
        /// Returns a translation that is the negative player avatar position, making the local player centered.
        /// </summary>
        protected override Vector3 GetTranslation()
        {
            // Get viewport, to center objects around the camera position.
            var viewport = _parameterization.SpriteBatch.GraphicsDevice.Viewport;

            // Return the *negative* camera position, because that's the
            // actual amount we need to translate game objects to be drawn
            // at the correct position.
            Vector3 result;
            result.X = -_cameraPosition.X + viewport.Width / 2f;
            result.Y = -_cameraPosition.Y + viewport.Height / 2f;
            result.Z = 0;
            return result;
        }

        #endregion

        #region Copying

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (PlayerCenteredRenderSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._cameraPosition = _cameraPosition;
                copy._isCameraFixed = _isCameraFixed;
                copy._lastFrame = _lastFrame;
                copy._previousOffset = _previousOffset;
                copy._session = _session;
            }

            return copy;
        }

        #endregion
    }
}
