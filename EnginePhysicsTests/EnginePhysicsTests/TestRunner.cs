using System;
using System.Text;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.Physics.Systems;
using Engine.Physics.Tests.Tests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Tests
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
            new ContinuousTest(),
            new BulletTest(),
            new SphereStack(),
            new EdgeBenchmark(),
            new EdgeBenchmarkWithCircles(),
            new CharacterCollision(),
        };

        /// <summary>
        /// The graphics device manager we use.
        /// </summary>
        private readonly GraphicsDeviceManager _graphics;

        /// <summary>
        /// The manager in which the current test runs.
        /// </summary>
        private readonly Manager _manager = new Manager();

        /// <summary>
        /// A string buffer used to accumulate text messages to print each frame.
        /// </summary>
        private static readonly StringBuilder StringBuffer = new StringBuilder();

        /// <summary>
        /// Used to render messages.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// The font to render messages with.
        /// </summary>
        private SpriteFont _font;

        /// <summary>
        /// The last keyboard state, used to check for changes.
        /// </summary>
        private KeyboardState _lastKeyboardState;

        /// <summary>
        /// The last mouse state, to check for changes.
        /// </summary>
        private MouseState _lastMouseState;

        /// <summary>
        /// The id of the current test.
        /// </summary>
        private int _currentTest;

        /// <summary>
        /// Whether the simulation is currently allowed to run.
        /// </summary>
        private bool _running = true;

        /// <summary>
        /// Override for running state to force stepping a single update.
        /// </summary>
        private bool _runOnce;

        /// <summary>
        /// The physics system we use.
        /// </summary>
        private PhysicsSystem _physics;

        /// <summary>
        /// The debug render system we use.
        /// </summary>
        private DebugPhysicsRenderSystem _renderer;

        /// <summary>
        /// Whether to render the help text.
        /// </summary>
        private bool _showHelp = true;

        #endregion

        /// <summary>
        /// Draws the string in the next draw call.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments to put into the format string.</param>
        public static void DrawString(string format, params object[] args)
        {
            StringBuffer.AppendFormat(format, args);
            StringBuffer.Append("\n");
        }

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
            _spriteBatch = new SpriteBatch(_graphics.GraphicsDevice);
            _font = new ResourceContentManager(Services, GameResource.ResourceManager).Load<SpriteFont>("ConsoleFont");

            _manager.AddSystem(_physics = new PhysicsSystem(1 / 60f, new Vector2(0, -10f)));
            _manager.AddSystem(new GraphicsDeviceSystem(Content, _graphics) {Enabled = true});
            _manager.AddSystem(_renderer = new DebugPhysicsRenderSystem {Enabled = true, Scale = 0.1f, Offset = new WorldPoint(0, -12)});

            _renderer.RenderFixtures = true;

            LoadTest(0);
        }

        /// <summary>
        /// Updates the simulation running the current test and checks
        /// whether we should switch to another test.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Only handle input when window has focus.
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            if (IsActive)
            {
                // Check for key presses and releases.
                foreach (Keys key in Enum.GetValues(typeof(Keys)))
                {
                    if (keyboardState[key] == KeyState.Down && _lastKeyboardState[key] == KeyState.Up)
                    {
                        OnKeyDown(key);
                    }
                    if (keyboardState[key] == KeyState.Up && _lastKeyboardState[key] == KeyState.Down)
                    {
                        OnKeyUp(key);
                    }
                }

                // Check for left clicks.
                if (mouseState.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released)
                {
                    OnLeftButtonDown();
                }
                if (mouseState.LeftButton == ButtonState.Released && _lastMouseState.LeftButton == ButtonState.Pressed)
                {
                    OnLeftButtonUp();
                }

                // Check for right clicks.
                if (mouseState.RightButton == ButtonState.Pressed && _lastMouseState.RightButton == ButtonState.Released)
                {
                    OnRightButtonDown();
                }
                if (mouseState.RightButton == ButtonState.Released && _lastMouseState.RightButton == ButtonState.Pressed)
                {
                    OnRightButtonUp();
                }

                // Check for mouse wheel.
                if (mouseState.ScrollWheelValue > _lastMouseState.ScrollWheelValue)
                {
                    OnWheelUp();
                }
                if (mouseState.ScrollWheelValue < _lastMouseState.ScrollWheelValue)
                {
                    OnWheelDown();
                }

                // Mouse move.
                if (mouseState.X != _lastMouseState.X || mouseState.Y != _lastMouseState.Y)
                {
                    var mousePosition = new Vector2(mouseState.X, GraphicsDevice.Viewport.Height - mouseState.Y);
                    OnMouseMove(mousePosition, mousePosition - new Vector2(_lastMouseState.X, GraphicsDevice.Viewport.Height - _lastMouseState.Y));
                }
            }
            _lastKeyboardState = Keyboard.GetState();
            _lastMouseState = mouseState;

            StringBuffer.Clear();

            if (_manager != null)
            {
                DrawString(Tests[_currentTest].GetType().Name);
            }

            if (!_running)
            {
                DrawString("****PAUSED**** (press Space to toggle)");
            }

            if (_showHelp)
            {
                DrawString(
@"
Hotkeys:
F1           - Toggles this help message.
F2           - Toggles profiler information.
F3           - Toggle joint rendering.
F4           - Toggle contact points and normal rendering.
F5           - Toggle contact point normal impulse rendering.
F7           - Toggle center of mass rendering.
F8           - Toggle bounding box rendering.

Left Arrow   - Previous test.
Right Arrow  - Next test.
Space        - Pause or unpause simulation.
Tab or Enter - Advance simulation one frame.
R            - Reload current test (keeping pause state).");
            }
            else
            {
                DrawString("\nPress F1 for help.");
            }

            // Newline before any text current test may want to display.
            DrawString("");

            // Allow simulating?
            if ((_running || _runOnce) && _manager != null)
            {
                _manager.Update(0);
                Tests[_currentTest].Update();
                _runOnce = false;
            }
        }

        /// <summary>
        /// Renders the current test scene.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            GraphicsDevice.Clear(Color.Black);

            if (_manager != null)
            {
                _manager.Draw(0, 0);
            }

            if (StringBuffer.Length > 0)
            {
                _spriteBatch.Begin();
                _spriteBatch.DrawString(_font, StringBuffer, new Vector2(20, 20), Color.White);
                _spriteBatch.End();
            }
        }

        /// <summary>
        /// Loads the test at the specified index. Returns silently if there are
        /// no tests known. Will wrap the number to the valid range.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="reset">Whether to reset system settings.</param>
        private void LoadTest(int number, bool reset = true)
        {
            // Gracefully ignore if there are no tests.
            if (Tests.Length == 0)
            {
                return;
            }

            // Clear our system.
            _manager.Clear();
            _physics.Gravity = new Vector2(0, -10);
            if (reset)
            {
                _renderer.Offset = new WorldPoint(0, -12);
                _renderer.Scale = 0.1f;
            }

            // Wrap the number to the valid range.
            _currentTest = (number + Tests.Length) % Tests.Length;

            // Initialize the new test.
            Tests[_currentTest].Initialize(_manager);

            // Pause if the test tells us to.
            if (reset)
            {
                _running = !Tests[_currentTest].StartPaused;
            }
        }

        /// <summary>
        /// Called when the specified key was pressed.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        private void OnKeyDown(Keys key)
        {
            switch (key)
            {
                case Keys.Right:
                    // Next test.
                    LoadTest(_currentTest + 1);
                    break;
                case Keys.Left:
                    // Previous test
                    LoadTest(_currentTest - 1);
                    break;

                case Keys.Space:
                    // Toggle pause.
                    _running = !_running;
                    break;

                case Keys.Tab:
                case Keys.Enter:
                    // Manual step.
                    _runOnce = true;
                    break;

                case Keys.R:
                    // Reset current test but keep camera and run settings.
                    LoadTest(_currentTest, false);
                    break;

                case Keys.F1:
                    // Toggle help display.
                    _showHelp = !_showHelp;
                    break;
                case Keys.F2:
                    // Toggle profiler information.
                    break;
                case Keys.F3:
                    // Toggle joints.
                    break;
                case Keys.F4:
                    // Toggle contact point and normals.
                    _renderer.RenderContactPoints = !_renderer.RenderContactPoints;
                    _renderer.RenderContactNormals = !_renderer.RenderContactNormals;
                    break;
                case Keys.F5:
                    // Toggle contact point normal imulse.
                    _renderer.RenderContactPointNormalImpulse = !_renderer.RenderContactPointNormalImpulse;
                    break;
                case Keys.F7:
                    // Toggle center of mass.
                    _renderer.RenderCenterOfMass = !_renderer.RenderCenterOfMass;
                    break;
                case Keys.F8:
                    // Toggle fixture bounding boxes.
                    _renderer.RenderFixtureBounds = !_renderer.RenderFixtureBounds;
                    break;
            }

            if (_manager != null)
            {
                Tests[_currentTest].OnKeyDown(key);
            }
        }

        /// <summary>
        /// Called when the specified key was released.
        /// </summary>
        /// <param name="key">The key that was released.</param>
        private void OnKeyUp(Keys key)
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnKeyUp(key);
            }
        }

        /// <summary>
        /// Called when the left button was pressed.
        /// </summary>
        private void OnLeftButtonDown()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnLeftButtonDown();
            }
        }

        /// <summary>
        /// Called when the left button was released.
        /// </summary>
        private void OnLeftButtonUp()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnLeftButtonUp();
            }
        }

        /// <summary>
        /// Called when the right button was pressed.
        /// </summary>
        private void OnRightButtonDown()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnRightButtonDown();
            }
        }

        /// <summary>
        /// Called when the right button was released.
        /// </summary>
        private void OnRightButtonUp()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnRightButtonUp();
            }
        }

        /// <summary>
        /// Called when the right button was pressed.
        /// </summary>
        private void OnWheelDown()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnWheelDown();
            }
        }

        /// <summary>
        /// Called when the right button was released.
        /// </summary>
        private void OnWheelUp()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnWheelUp();
            }
        }

        private void OnMouseMove(Vector2 mousePosition, Vector2 delta)
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnMouseMove(mousePosition, delta);
            }
        }
    }
}
