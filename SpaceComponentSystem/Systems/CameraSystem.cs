using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Nuclex.Input.Devices;
using Space.Input;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Tracks camera position, either based on player's position and input
    /// state, or via a set position.
    /// </summary>
    public sealed class CameraSystem : AbstractSystem
    {
        #region Properties

        /// <summary>
        /// The current camera position.
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
        /// The game this system belongs to.
        /// </summary>
        private readonly Game _game;

        /// <summary>
        /// The session this system belongs to, for fetching the local player.
        /// </summary>
        private readonly IClientSession _session;

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

        #region Constructor
        #endregion

        public CameraSystem(Game game, IClientSession session)
        {
            _game = game;
            _session = session;
        }

        #region Accessors

        /// <summary>
        /// Returns a translation, that when applied to world objects will
        /// make them appear relative in a way as if the camera were centered
        /// on the screen.
        /// </summary>
        /// <returns>The offset to apply to objects when rendering them.</returns>
        public Vector2 GetTranslation()
        {
            // Get viewport, to center objects around the camera position.
            var viewport = _game.GraphicsDevice.Viewport;
            var cameraPosition = Manager.GetSystem<CameraSystem>().CameraPositon;

            // Return the *negative* camera position, because that's the
            // actual amount we need to translate game objects to be drawn
            // at the correct position.
            Vector2 result;
            result.X = viewport.Width / 2f - cameraPosition.X;
            result.Y = viewport.Height / 2f - cameraPosition.Y;
            return result;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Used to update the camera position. We don't do this in the draw,
        /// to make sure it's up-to-date before *anything* else is drawn,
        /// especially stuff outside the simulation, to avoid "lagging".
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame the update applies to.</param>
        public override void Update(GameTime gameTime, long frame)
        {
            // Only update the offset if we didn't roll-back.
            if (frame > _lastFrame)
            {
                if (!_customCameraPosition.HasValue &&
                    _session.ConnectionState == ClientState.Connected)
                {
                    var avatar = Manager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
                    if (avatar.HasValue)
                    {
                        // Non-fixed camera, update our offset based on the game pad
                        // or mouse position, relative to the ship.
                        var currentOffset = GetInputInducedOffset();
                        var avatarPosition = Manager.GetComponent<Transform>(avatar.Value).Translation;

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

            base.Update(gameTime, frame);
        }

        /// <summary>
        /// Gets the input induced camera offset, based on mouse position or
        /// game pad state.
        /// </summary>
        /// <returns>The offset based on player input.</returns>
        private Vector2 GetInputInducedOffset()
        {
            Vector2 offset;
            offset.X = 0;
            offset.Y = 0;

            // Get viewport, for mouse position scaling and offset scaling.
            var viewport = _game.GraphicsDevice.Viewport;
            var offsetScale = (float)(Math.Sqrt(viewport.Width * viewport.Width + viewport.Height * viewport.Height) / 6.0);

            var mouse = (IMouse)_game.Services.GetService(typeof(IMouse));
            var gamePad = (IGamePad)_game.Services.GetService(typeof(IGamePad));

            // If we have a game pad attached, get the stick tilt.
            if (Settings.Instance.EnableGamepad && gamePad != null)
            {
                offset = GamePadHelper.GetLook(gamePad);
            }
            else if (mouse != null)
            {
                // Otherwise use the mouse.
                var state = mouse.GetState();
                
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

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (CameraSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._previousOffset = _previousOffset;
                copy._cameraPosition = _cameraPosition;
                copy._customCameraPosition = _customCameraPosition;
            }

            return copy;
        }

        #endregion
    }
}
