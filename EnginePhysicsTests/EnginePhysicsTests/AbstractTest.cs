using System.Diagnostics;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.Physics.Components;
using Engine.Physics.Systems;
using Microsoft.Xna.Framework.Input;

using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using Microsoft.Xna.Framework;

namespace EnginePhysicsTests
{
    internal abstract class AbstractTest
    {
        #region Properties

        /// <summary>
        /// Gets the manager representing the system in which this test runs.
        /// </summary>
        protected IManager Manager { get; private set; }

        /// <summary>
        /// Gets the physics system that runs this test.
        /// </summary>
        protected PhysicsSystem Physics { get; private set; }

        /// <summary>
        /// Gets the renderer used to display the world.
        /// </summary>
        protected DebugPhysicsRenderSystem Renderer { get; private set; }

        /// <summary>
        /// Gets the mouse cursor position in world coordinates.
        /// </summary>
        protected WorldPoint MouseWorldPoint
        {
            get
            {
                var viewport = _graphics.Graphics.GraphicsDevice.Viewport;
                WorldPoint result;
                result.X = Mouse.GetState().X - viewport.Width / 2;
                result.Y = -(Mouse.GetState().Y - viewport.Height / 2);
                result /= Renderer.Scale;
                result.X -= Renderer.Offset.X;
                result.Y += Renderer.Offset.Y;
                return result;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The body currently being dragged.
        /// </summary>
        private Body _pickedBody;

        /// <summary>
        /// The point in the body's local coordinate system from which we drag.
        /// </summary>
        private LocalPoint _pickedPoint;

        /// <summary>
        /// The force to apply to the dragged body.
        /// </summary>
        private float _force;

        /// <summary>
        /// Whether we're currently dragging the world around (moving the camera).
        /// </summary>
        private bool _dragging;

        /// <summary>
        /// The last mouse state, to check for changes.
        /// </summary>
        private MouseState _lastMouseState;

        /// <summary>
        /// The system providing information on the graphics device.
        /// </summary>
        private GraphicsDeviceSystem _graphics;

        #endregion

        #region Base Logic

        /// <summary>
        /// Initializes the test with the specified manager.
        /// </summary>
        /// <param name="manager">The manager.</param>
        public void Initialize(IManager manager)
        {
            Manager = manager;
            Physics = Manager.GetSystem(PhysicsSystem.TypeId) as PhysicsSystem;
            Renderer = Manager.GetSystem(Engine.ComponentSystem.Manager.GetSystemTypeId<DebugPhysicsRenderSystem>()) as DebugPhysicsRenderSystem;
            _graphics = Manager.GetSystem(GraphicsDeviceSystem.TypeId) as GraphicsDeviceSystem;
            Create();
        }

        /// <summary>
        /// Updates the test, each time the simulation was advanced.
        /// </summary>
        public void Update()
        {
            // Check for left clicks.
            if (Mouse.GetState().LeftButton == ButtonState.Pressed &&
                _lastMouseState.LeftButton == ButtonState.Released)
            {
                OnLeftButtonDown();
            }
            else if (Mouse.GetState().LeftButton == ButtonState.Released &&
                     _lastMouseState.LeftButton == ButtonState.Pressed)
            {
                OnLeftButtonUp();
            }

            // Check for right clicks.
            if (Mouse.GetState().RightButton == ButtonState.Pressed &&
                _lastMouseState.RightButton == ButtonState.Released)
            {
                OnRightButtonDown();
            }
            else if (Mouse.GetState().RightButton == ButtonState.Released &&
                     _lastMouseState.RightButton == ButtonState.Pressed)
            {
                OnRightButtonUp();
            }
            
            // Check for scrolling, used to zoom in or out.
            if (Mouse.GetState().ScrollWheelValue > _lastMouseState.ScrollWheelValue)
            {
                Renderer.Scale *= 1.5f;
            }
            else if (Mouse.GetState().ScrollWheelValue < _lastMouseState.ScrollWheelValue)
            {
                Renderer.Scale /= 1.5f;
            }
            Renderer.Scale = MathHelper.Clamp(Renderer.Scale, 5, 50);

            // Check for scrolling in the world.
            if (_dragging)
            {
                var deltaX = Mouse.GetState().X - _lastMouseState.X;
                var deltaY = Mouse.GetState().Y - _lastMouseState.Y;
                Renderer.Offset += new Vector2(deltaX, deltaY) / Renderer.Scale;
            }

            // Check if dragging a body.
            if (_pickedBody != null)
            {
                var pickWorldPoint = _pickedBody.GetWorldPoint(_pickedPoint);
                var direction = (Vector2)(MouseWorldPoint - pickWorldPoint);
                _pickedBody.LinearVelocity *= 0.9f;
                //Character.AngularVelocity *= 0.9f;
                _pickedBody.ApplyForce(direction * _force, pickWorldPoint);
            }

            _lastMouseState = Mouse.GetState();

            Step();
        }

        #endregion

        #region Implementation Logic

        /// <summary>
        /// Called when the left button was pressed. In the abstract class this
        /// method initialized dragging shapes, so override and don't call to
        /// the base implementation to disable that behavior.
        /// </summary>
        protected virtual void OnLeftButtonDown()
        {
            var fixture = Physics.GetFixtureAt(MouseWorldPoint);
            if (fixture != null)
            {
                _pickedBody = Manager.GetComponent(fixture.Entity, Body.TypeId) as Body;
                Debug.Assert(_pickedBody != null);
                _pickedPoint = _pickedBody.GetLocalPoint(MouseWorldPoint);
                _force = _pickedBody.Mass * 50;
            }
        }

        /// <summary>
        /// Called when the left button was released. In the abstract class this
        /// method finishes dragging shapes, so override and don't call to the
        /// base implementation to disable that behavior.
        /// </summary>
        protected virtual void OnLeftButtonUp()
        {
            if (_pickedBody != null)
            {
                _pickedBody = null;
                _pickedPoint = Vector2.Zero;
                _force = 0;
            }
        }

        /// <summary>
        /// Called when the right button was pressed. In the abstract class this
        /// method initializes dragging the world/camera, so override and don't
        /// call to the base implementation to disable that behavior.
        /// </summary>
        protected virtual void OnRightButtonDown()
        {
            _dragging = true;
        }

        /// <summary>
        /// Called when the right button was released. In the abstract class this
        /// method finishes dragging the world/camera, so override and don't
        /// call to the base implementation to disable that behavior.
        /// </summary>
        protected virtual void OnRightButtonUp()
        {
            _dragging = false;
        }

        /// <summary>
        /// Called when the scene should be set up (bodies and fixtures created).
        /// </summary>
        protected virtual void Create()
        {
        }

        /// <summary>
        /// Called inbetween simulation updates.
        /// </summary>
        protected virtual void Step()
        {
        }

        #endregion
    }
}
