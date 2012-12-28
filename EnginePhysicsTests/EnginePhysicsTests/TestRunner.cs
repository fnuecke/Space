using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.Physics.Systems;
using EnginePhysicsTests.Tests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EnginePhysicsTests
{
    /// <summary>
    /// Test runner framework, knows tests and switches between them.
    /// </summary>
    internal sealed class TestRunner : Game
    {
        #region Fields

        /// <summary>
        /// The list of known tests between we can cycle.
        /// </summary>
        private static readonly AbstractTest[] Tests = new AbstractTest[]
        {
            new Pyramid(),
            new VaryingRestitution(),
            new VaryingFriction(),
            new AddPair(),
            new CompoundShapes(),
        };

        /// <summary>
        /// The graphics device manager we use.
        /// </summary>
        private readonly GraphicsDeviceManager _graphics;

        /// <summary>
        /// The last keyboard state, used to check whether we need to cycle.
        /// </summary>
        private KeyboardState _lastState;

        /// <summary>
        /// The id of the current test.
        /// </summary>
        private int _currentTest;

        /// <summary>
        /// Whether the simulation is currently allowed to run.
        /// </summary>
        private bool _running = false;

        /// <summary>
        /// The manager in which the current test runs.
        /// </summary>
        private Manager _manager;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunner"/> class.
        /// </summary>
        public TestRunner()
        {
            IsMouseVisible = true;
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1024,
                PreferredBackBufferHeight = 768,
                SynchronizeWithVerticalRetrace = true,
                PreferMultiSampling = true
            };
            Content.RootDirectory = "data";
        }

        /// <summary>
        /// Initialize to the first test when the graphics device has been set up.
        /// </summary>
        protected override void LoadContent()
        {
            LoadTest(0);
        }

        /// <summary>
        /// Updates the simulation running the current test and checks
        /// whether we should switch to another test.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Switch test?
            if (KeyPressed(Keys.Right))
            {
                LoadTest(_currentTest + 1);
            }
            else if (KeyPressed(Keys.Left))
            {
                LoadTest(_currentTest - 1);
            }

            // Toggle pause?
            if (KeyPressed(Keys.P))
            {
                _running = !_running;
            }

            // Allow simulating?
            if ((_running || KeyPressed(Keys.Space)) && _manager != null)
            {
                _manager.Update(0);
            }

            // Always run tests, to allow changing camera.
            Tests[_currentTest].Update();

            _lastState = Keyboard.GetState();
        }

        /// <summary>
        /// Renders the current test scene.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (_manager != null)
            {
                _manager.Draw(0, 0);
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// Checks whether the specified key was pressed this update.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns></returns>
        private bool KeyPressed(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key) && !_lastState.IsKeyDown(key);
        }

        /// <summary>
        /// Loads the test at the specified index. Returns silently if there are
        /// no tests known. Will wrap the number to the valid range.
        /// </summary>
        /// <param name="number">The number.</param>
        private void LoadTest(int number)
        {
            // Gracefully ignore if there are no tests.
            if (Tests.Length == 0)
            {
                return;
            }

            // Recreate our manager and systems.
            _manager = new Manager();
            _manager.AddSystem(new PhysicsSystem(1 / 60f, new Vector2(0, -9.81f)));
            _manager.AddSystem(new GraphicsDeviceSystem(Content, _graphics) {Enabled = true});
            _manager.AddSystem(new DebugPhysicsRenderSystem {Enabled = true});

            // Wrap the number to the valid range.
            _currentTest = (number + Tests.Length) % Tests.Length;

            // Initialize the new test.
            Tests[_currentTest].Initialize(_manager);
        }
    }
}
