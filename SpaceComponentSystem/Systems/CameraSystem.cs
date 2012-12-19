using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
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
    public sealed class CameraSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

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

        /// <summary>
        /// Index group mask for the index we use to track positions of stuff
        /// that can be seen by the camera (and can therefore appear in the
        /// list of visible entities).
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The current camera position.
        /// </summary>
        public FarPosition CameraPositon
        {
            get { return _customCameraPosition ?? (_cameraPosition + _currentOffset); }
            set { _customCameraPosition = value; }
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
            get { return _customZoom ?? _currentZoom ; }
            set { _customZoom = value; }
        }

        /// <summary>
        /// The zoom set in the camera must not neccesarily represent the current actually used zoom
        /// </summary>
        public float CameraZoom
        {
            get { return _currentZoom; }
        }

        /// <summary>
        /// The list of currently visible entities.
        /// </summary>
        public IEnumerable<int> VisibleEntities
        {
            get { return _drawablesInView; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The graphics device we render to.
        /// </summary>
        private readonly GraphicsDevice _graphics;

        /// <summary>
        /// Services provided by our game.
        /// </summary>
        private readonly IServiceProvider _services;

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
        /// The current zoom of the camera which is interpolated towards the
        /// actual target zoom.
        /// </summary>
        private float? _customZoom;
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

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private ISet<int> _drawablesInView = new HashSet<int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraSystem"/> class.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="services">The services.</param>
        public CameraSystem(GraphicsDevice graphics, IServiceProvider services)
        {
            _graphics = graphics;
            _services = services;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        /// <returns>The visible bounds, in world coordinates.</returns>
        public FarRectangle ComputeVisibleBounds()
        {
            var center = CameraPositon;
            var zoom = Zoom;
            var width = (int)(_graphics.Viewport.Width / zoom);
            var height = (int)(_graphics.Viewport.Height / zoom);
            // Return scaled viewport bounds, translated to camera position
            // with a margin as safety against rounding errors and interpolation.
            return new FarRectangle
                   {
                       X = (center.X - (width >> 1)) - 100,
                       Y = (center.Y - (height >> 1)) - 100,
                       Width = width + 200,
                       Height = height + 200
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

        public void ResetCamera()
        {
            _customCameraPosition = null;
        }

        public void ResetZoom()
        {
            _customZoom = null;
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
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Don't update if our position is fixed or we're not in a game/don't have an avatar.
            var avatar = ((LocalPlayerSystem)Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            if (_customCameraPosition.HasValue || avatar <= 0)
            {
                // Update the transformation.
                UpdateTransformation();
                return;
            }

            // Non-fixed camera, update our offset based on the game pad
            // or mouse position, relative to the ship.
            var targetOffset = GetInputInducedOffset();
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);
            interpolation.GetInterpolatedPosition(avatar, out _cameraPosition);

            // The interpolate to our new offset, slowly to make the
            // effect less brain-melting.
            _currentOffset = FarPosition.SmoothStep(_currentOffset, (FarPosition)targetOffset, 0.15f);

            // Interpolate new zoom moving slowly in or out.
            _currentZoom = MathHelper.SmoothStep(_currentZoom, _targetZoom, 0.15f);

            // Update the transformation.
            UpdateTransformation();
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
            var viewport = _graphics.Viewport;
            var offsetScale = (float)(Math.Sqrt(viewport.Width * viewport.Width + viewport.Height * viewport.Height) / 6.0);

            var inputManager = (InputManager)_services.GetService(typeof(InputManager));
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
            var viewport = _graphics.Viewport;
            // Use far position for camera translation.
            _transform.Translation = -CameraPositon;
            // Apply zoom and viewport offset via normal matrix.
            _transform.Matrix = Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                                Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
            // Update the list of visible entities. This method is called each
            // draw, so we can do this here.
            _drawablesInView.Clear();
            var view = ComputeVisibleBounds();
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, IndexGroupMask);
        }

        #endregion
    }
}
