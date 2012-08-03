using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nuclex.Input;
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
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = Engine.ComponentSystem.Manager.GetSystemTypeId(typeof(CameraSystem));

        #endregion

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
        public FarPosition CameraPositon
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
        /// Gets the transformation to use for perspective projection.
        /// </summary>
        /// <returns>The transformation.</returns>
        public FarTransform Transform
        {
            get { return _transform; }
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
        private FarPosition _currentOffset;

        /// <summary>
        /// The current camera position.
        /// </summary>
        private FarPosition _cameraPosition;

        /// <summary>
        /// Flag to tell if the current camera position was set from outside,
        /// or was dynamically computed.
        /// </summary>
        private FarPosition? _customCameraPosition;

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
        /// The transformation to use for perspective projection.
        /// </summary>
        private FarTransform _transform;

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
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        /// <param name="view">The viewport to compute the visible bounds for.</param>
        public FarRectangle ComputeVisibleBounds(Viewport view)
        {
            var center = CameraPositon;
            var zoom = Zoom;
            var width = (int)(view.Width / zoom);
            var height = (int)(view.Height / zoom);
            // Return scaled viewport bounds, translated to camera position
            // with a 1 pixel increase as safety against rounding errors.
            return new FarRectangle
                   {
                       X = (center.X - (width >> 1)) - 1,
                       Y = (center.Y - (height >> 1)) - 1,
                       Width = width + 2,
                       Height = height + 2
                   };
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
        /// <param name="frame">The frame the update applies to.</param>
        public override void Update(long frame)
        {
            // Only update the offset if we didn't roll-back.
            if (frame > _lastFrame)
            {
                if (!_customCameraPosition.HasValue &&
                    _session.ConnectionState == ClientState.Connected)
                {
                    var avatar = ((AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(_session.LocalPlayer.Number);
                    if (avatar.HasValue)
                    {
                        // Non-fixed camera, update our offset based on the game pad
                        // or mouse position, relative to the ship.
                        var targetOffset = GetInputInducedOffset();
                        var avatarPosition = ((Transform)Manager.GetComponent(avatar.Value, Engine.ComponentSystem.Common.Components.Transform.TypeId)).Translation;

                        // The interpolate to our new offset, slowly to make the
                        // effect less brain-melting.
                        _currentOffset = FarPosition.SmoothStep(_currentOffset, (FarPosition)targetOffset, 0.15f);

                        // The camera *position* is then the avatar position, plus
                        // the offset, correcting for the viewport center which was
                        // subtracted to make the mouse position's origin centered
                        // to the screen.
                        _cameraPosition = avatarPosition + _currentOffset;

                        // Interpolate new zoom moving slowly in or out.
                        _currentZoom = MathHelper.SmoothStep(_currentZoom, _targetZoom, 0.15f);

                        // Update the transformation.
                        UpdateTransformation();
                    }
                }

                _lastFrame = frame;
            }

            base.Update(frame);
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

            var inputManager = (InputManager)_game.Services.GetService(typeof(InputManager));
            var mouse = inputManager.GetMouse();

            // If we have a game pad attached, get the stick tilt.
            if (Settings.Instance.EnableGamepad)
            {
                foreach (var gamepad in inputManager.GamePads)
                {
                    if (gamepad.IsAttached)
                    {
                        offset = GamePadHelper.GetLook(gamepad);

                        // Only use the first gamepad we can find.
                        break;
                    }
                }
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
            // Use far position for camera translation.
            _transform.Translation = -CameraPositon;
            // Apply zoom and viewport offset via normal matrix.
            _transform.Matrix = Matrix.CreateScale(new Vector3(_currentZoom, _currentZoom, 1)) *
                                Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a shallow copy of the object.
        /// </summary>
        /// <returns>The copy.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (CameraSystem)base.NewInstance();

            copy._lastFrame = 0;
            copy._currentOffset = FarPosition.Zero;
            copy._cameraPosition = FarPosition.Zero;
            copy._customCameraPosition = null;
            copy._targetZoom = MaximumZoom;
            copy._currentZoom = MaximumZoom;
            copy._transform = FarTransform.Identity;

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (CameraSystem)into;

            copy._lastFrame = _lastFrame;
            copy._currentOffset = _currentOffset;
            copy._cameraPosition = _cameraPosition;
            copy._customCameraPosition = _customCameraPosition;
            // Do NOT copy zoom, as it's set locally, and we never want to
            // overwrite the leading one. Otherwise the zoom would change
            // on rollbacks, which is very irritating.
            //copy._targetZoom = _targetZoom;
            //copy._currentZoom = _currentZoom;
            copy._transform = _transform;
        }

        #endregion
    }
}
