using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nuclex.Input.Devices;
using Space.Input;

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
        public Vector2 CameraPositon
        { 
            get
            {
                return _customCameraPosition ?? _cameraPosition;
            } 
            set 
            {
                _customCameraPosition = value; 
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private IClientSession _session;

        /// <summary>
        /// The graphics device (used to look up the current viewport).
        /// </summary>
        private GraphicsDevice _graphicsDevice;

        /// <summary>
        /// Used for dynamic camera offset.
        /// </summary>
        private IMouse _mouse;

        /// <summary>
        /// Used for dynamic camera offset.
        /// </summary>
        private IGamePad _gamePad;

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
        private Vector2? _customCameraPosition;

        #endregion

        public PlayerCenteredRenderSystem(Game game, SpriteBatch spriteBatch, IGraphicsDeviceService graphics, IClientSession session)
            : base(game, spriteBatch, graphics)
        {
            _session = session;
            _graphicsDevice = game.GraphicsDevice;
            _mouse = (IMouse)game.Services.GetService(typeof(IMouse));
            _gamePad = (IGamePad)game.Services.GetService(typeof(IGamePad));
        }

        #region Logic

        /// <summary>
        /// Used to update the camera position. We don't do this in the draw,
        /// to make sure it's up-to-date before *anything* else is drawn,
        /// especially stuff outside the simulation, to avoid "lagging".
        /// </summary>
        /// <param name="frame">The frame the update applies to.</param>
        public override void Update(long frame)
        {
            // Only update the offset if we didn't roll-back.
            if (frame > _lastFrame)
            {
                if (!_customCameraPosition.HasValue && _session.ConnectionState == ClientState.Connected)
                {
                    var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
                    if (avatar != null)
                    {
                        // Non-fixed camera, update our offset based on the game pad
                        // or mouse position, relative to the ship.
                        Vector2 currentOffset = GetInputInducedOffset();
                        var avatarPosition = avatar.GetComponent<Transform>().Translation;

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

            base.Update(frame);
        }

        private Vector2 GetInputInducedOffset()
        {
            Vector2 offset;
            offset.X = 0;
            offset.Y = 0;

            // Get viewport, for mouse position scaling and offset scaling.
            var viewport = _graphicsDevice.Viewport;
            float offsetScale = (float)(System.Math.Sqrt(viewport.Width * viewport.Width + viewport.Height * viewport.Height) / 6.0);

            // If we have a game pad attached, get the stick tilt.
            if (Settings.Instance.EnableGamepad && _gamePad != null)
            {
                offset = GamePadHelper.GetLook(_gamePad);
            }
            else if (_mouse != null)
            {
                // Otherwise use the mouse.
                var state = _mouse.GetState();
                
                // Get the relative position of the mouse to the ship and
                // apply some factoring to it (so that the maximum distance
                // of cursor to ship is not half the screen size).
                if (state.X >= 0 && state.X < viewport.Width)
                {
                    offset.X = ((state.X / (float)viewport.Width) - 0.5f) * 2;
                }
                if (state.Y >= 0 && state.Y < viewport.Height)
                {
                    offset.Y = ((state.Y / (float)viewport.Height) - 0.5f) * 2;
                }
            }

            if (offset.LengthSquared() > 1)
            {
                offset.Normalize();
            }
            return offset * offsetScale;
        }

        /// <summary>
        /// Returns a translation that is the negative player avatar position, making the local player centered.
        /// </summary>
        protected override Vector3 GetTranslation()
        {
            // Get viewport, to center objects around the camera position.
            var viewport = _graphicsDevice.Viewport;

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
                copy._session = _session;
                copy._graphicsDevice = _graphicsDevice;
                copy._previousOffset = _previousOffset;
                copy._cameraPosition = _cameraPosition;
                copy._customCameraPosition = _customCameraPosition;
            }

            return copy;
        }

        #endregion
    }
}
