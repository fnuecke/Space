using Engine.ComponentSystem;
using Engine.Physics.Systems;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests
{
    internal abstract class AbstractTest
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether to start the test paused or not.
        /// Computationally expensive tests should return true here, to avoid
        /// locking up the application while cycling through tests.
        /// </summary>
        public virtual bool StartPaused { get { return false;} }

        /// <summary>
        /// Gets the manager representing the system in which this test runs.
        /// </summary>
        protected IManager Manager { get; private set; }

        /// <summary>
        /// Gets a random number generator that may be used in tests.
        /// </summary>
        protected IUniformRandom Random { get { return _random; } }

        /// <summary>
        /// Gets the physics system that runs this test.
        /// </summary>
        protected PhysicsSystem Physics { get; private set; }

        /// <summary>
        /// Gets the renderer used to display the world.
        /// </summary>
        protected DebugPhysicsRenderSystem Renderer { get; private set; }

        /// <summary>
        /// Gets the step count for the current test run.
        /// </summary>
        protected int StepCount { get; private set; }

        /// <summary>
        /// Gets the mouse cursor position in world coordinates.
        /// </summary>
        protected WorldPoint MouseWorldPoint
        {
            get
            {
                return Renderer.ScreenToSimulation(new Vector2(Mouse.GetState().X,
                                                               Mouse.GetState().Y));
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Random number generator for tests.
        /// </summary>
        private readonly MersenneTwister _random = new MersenneTwister(0);

        /// <summary>
        /// Whether we're currently dragging the world around (moving the camera).
        /// </summary>
        private bool _dragging;

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
            Renderer = Manager.GetSystem(ComponentSystem.Manager.GetSystemTypeId<DebugPhysicsRenderSystem>()) as DebugPhysicsRenderSystem;
            _random.Seed(0);
            StepCount = 0;
            Create();
        }

        /// <summary>
        /// Updates the test, each time the simulation was advanced.
        /// </summary>
        public void Update()
        {
            ++StepCount;
            Step();
        }

        #endregion

        #region Implementation Logic

        /// <summary>
        /// Called when the specified key was pressed.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        public virtual void OnKeyDown(Keys key)
        {
        }

        /// <summary>
        /// Called when the specified key was released.
        /// </summary>
        /// <param name="key">The key that was released.</param>
        public virtual void OnKeyUp(Keys key)
        {
        }

        /// <summary>
        /// Called when the left button was pressed. In the abstract class this
        /// method initialized dragging shapes, so override and don't call to
        /// the base implementation to disable that behavior.
        /// </summary>
        public virtual void OnLeftButtonDown()
        {
        }

        /// <summary>
        /// Called when the left button was released. In the abstract class this
        /// method finishes dragging shapes, so override and don't call to the
        /// base implementation to disable that behavior.
        /// </summary>
        public virtual void OnLeftButtonUp()
        {
        }

        /// <summary>
        /// Called when the right button was pressed. In the abstract class this
        /// method initializes dragging the world/camera, so override and don't
        /// call to the base implementation to disable that behavior.
        /// </summary>
        public virtual void OnRightButtonDown()
        {
            _dragging = true;
        }

        /// <summary>
        /// Called when the right button was released. In the abstract class this
        /// method finishes dragging the world/camera, so override and don't
        /// call to the base implementation to disable that behavior.
        /// </summary>
        public virtual void OnRightButtonUp()
        {
            _dragging = false;
        }

        /// <summary>
        /// Called when the mouse wheel was scrolled down. In the abstract class
        /// this method causes zooming out, so override and don't call to the base
        /// implementation to disable that behavior.
        /// </summary>
        public virtual void OnWheelDown()
        {
            Renderer.Scale /= 1.5f;
        }

        /// <summary>
        /// Called when the mouse wheel was scrolled up. In the abstract class
        /// this method causes zooming in, so override and don't call to the base
        /// implementation to disable that behavior.
        /// </summary>
        public virtual void OnWheelUp()
        {
            Renderer.Scale *= 1.5f;
        }

        /// <summary>
        /// Called when the mouse moved. In the abstract class this method causes
        /// camera movement, so override and don't call to the base implementation
        /// to disable that behavior.
        /// </summary>
        /// <param name="mousePosition">The new mouse position.</param>
        /// <param name="delta">The position delta.</param>
        public virtual void OnMouseMove(Vector2 mousePosition, Vector2 delta)
        {
            // Check for scrolling in the world.
            if (_dragging)
            {
                Renderer.Offset += delta / Renderer.Scale;
            }
        }

        /// <summary>
        /// Shortcut for drawing strings via <see cref="TestRunner.DrawString"/>.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments, if any.</param>
        protected static void DrawString(string format, params object[] args)
        {
            TestRunner.DrawString(format, args);
        }

        /// <summary>
        /// Called when the scene should be set up (bodies and fixtures created).
        /// </summary>
        protected abstract void Create();

        /// <summary>
        /// Called inbetween simulation updates.
        /// </summary>
        protected virtual void Step()
        {
        }

        #endregion
    }
}
