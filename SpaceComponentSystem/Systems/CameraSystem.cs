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
        #region Constants
        
        /// <summary>
        /// The maximum zoom scale.
        /// </summary>
        public const float MaximumZoom = 1.0f;

        /// <summary>
        /// The minimum zoom scale.
        /// </summary>
        public const float MinimumZoom = 0.5f;

        /// <summary>
        /// The maximum zoom scale.
        /// </summary>
        public const float ZoomStep = 0.1f;

        #endregion

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

        /// <summary>
        /// The Current camera zoom
        /// </summary>
        public float Zoom
        {
            get { return _currentZoom; }
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
        private Vector2 _currentOffset;

        /// <summary>
        /// The current camera position.
        /// </summary>
        private Vector2 _cameraPosition;

        /// <summary>
        /// Flag to tell if the current camera position was set from outside,
        /// or was dynamically computed.
        /// </summary>
        private Vector2? _customCameraPosition;

        /// <summary>
        /// The current target zoom of the camera.
        /// </summary>
        private float _targetZoom = 1.0f;

        /// <summary>
        /// The current zoom of the camera which is interpolated towards the
        /// actual target zoom.
        /// </summary>
        private float _currentZoom = 1.0f;

        /// <summary>
        /// The Transform Matrix Containing position and zoom of the camera
        /// </summary>
        private Matrix _transform;

        #endregion

        #region Constructor

        public CameraSystem(Game game, IClientSession session)
        {
            _game = game;
            _session = session;
        }

        #endregion

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

        public Matrix GetTransformation()
        {
            return _transform;
        }

        /// <summary>
        /// Set the current and target zoom to the specified value. This instantly
        /// sets the current zoom.
        /// </summary>
        public void SetZoom(float value)
        {
            _currentZoom = _targetZoom = MathHelper.Clamp(value, MinimumZoom, MaximumZoom);
        }

        /// <summary>
        /// Set the target zoom to the specified value. This slowly interpolates
        /// to the specified zoom value.
        /// </summary>
        public void ZoomTo(float value)
        {
            _targetZoom = MathHelper.Clamp(value, MinimumZoom, MaximumZoom);
        }

        /// <summary>
        /// Zoom in by one <em>ZoomStep</em>.
        /// </summary>
        public void ZoomIn()
        {
            ZoomTo(_targetZoom + ZoomStep);
        }

        /// <summary>
        /// Zoom out by one <em>ZoomStep</em>.
        /// </summary>
        public void ZoomOut()
        {
            ZoomTo(_targetZoom - ZoomStep);
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
                        var targetOffset = GetInputInducedOffset();
                        var avatarPosition = Manager.GetComponent<Transform>(avatar.Value).Translation;

                        // The interpolate to our new offset, slowly to make the
                        // effect less brain-melting.
                        _currentOffset = Vector2.Lerp(_currentOffset, targetOffset, 0.05f);

                        // The camera *position* is then the avatar position, plus
                        // the offset, correcting for the viewport center which was
                        // subtracted to make the mouse position's origin centered
                        // to the screen.
                        _cameraPosition = avatarPosition + _currentOffset;

                        // Interpolate new zoom moving slowly in or out.
                        _currentZoom = MathHelper.Lerp(_currentZoom, _targetZoom, 0.05f);

                        // Update the transformation.
                        UpdateTransformation();
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

        /// <summary>
        /// Updates the Transformation of the Camera including position and scale
        /// </summary>
        private void UpdateTransformation()
        {
            var viewport = _game.GraphicsDevice.Viewport;
            // Thanks to o KB o for this solution
            // fn: wtf is KB?
            _transform = Matrix.CreateTranslation(new Vector3(-CameraPositon.X, -CameraPositon.Y, 0)) *
                         Matrix.CreateScale(new Vector3(_currentZoom, _currentZoom, 1)) *
                         Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
        }

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (CameraSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._currentOffset = _currentOffset;
                copy._cameraPosition = _cameraPosition;
                copy._customCameraPosition = _customCameraPosition;
                copy._targetZoom = _targetZoom;
                copy._currentZoom = _currentZoom;
            }

            return copy;
        }

        #endregion
    }
}
