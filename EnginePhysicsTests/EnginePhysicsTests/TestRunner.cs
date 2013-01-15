using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.Math;
using Engine.Physics.Joints;
using Engine.Physics.Systems;
using Engine.Physics.Tests.Tests;
using Engine.Serialization;
using Engine.Util;
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
    /// <summary>Test runner framework, knows tests and switches between them.</summary>
    internal sealed class TestRunner : Game
    {
        #region Constants

        /// <summary>The list of known tests between we can cycle.</summary>
        private static readonly AbstractTest[] Tests = new AbstractTest[]
        {
            new Tumbler(),
            new Pyramid(),
            new VaryingRestitution(),
            new VaryingFriction(),
            new AddPair(),
            new CompoundShapes(),
            new ContinuousTest(),
            new Bullet(),
            new SphereStack(),
            new EdgeBenchmark(),
            new EdgeBenchmarkWithCircles(),
            new CharacterCollision(),
            new VerticalStack(),
            new TheoJansen(),
            new Gears(),
            new Car()
        };

        /// <summary>The updates per second to perform on the simulation.</summary>
        private const float UpdatesPerSecond = 60;

        #endregion

        #region Fields

        /// <summary>A string buffer used to accumulate text messages to print each frame.</summary>
        private static readonly StringBuilder StringBuffer = new StringBuilder();

        /// <summary>The graphics device manager we use.</summary>
        private readonly GraphicsDeviceManager _graphics;

        /// <summary>The manager in which the current test runs.</summary>
        private readonly Manager _manager = new Manager();

        /// <summary>Profiling data accumulated over time.</summary>
        private readonly Profile _profile = new Profile();

        /// <summary>Used to render messages.</summary>
        private SpriteBatch _spriteBatch;

        /// <summary>The font to render messages with.</summary>
        private SpriteFont _font;

        /// <summary>The last keyboard state, used to check for changes.</summary>
        private KeyboardState _lastKeyboardState;

        /// <summary>The last mouse state, to check for changes.</summary>
        private MouseState _lastMouseState;

        /// <summary>The id of the current test.</summary>
        private int _currentTest;

        /// <summary>Whether the simulation is currently allowed to run.</summary>
        private bool _running = true;

        /// <summary>Override for running state to force stepping a single update.</summary>
        private bool _runOnce;

        /// <summary>The physics system we use.</summary>
        private PhysicsSystem _physics;

        /// <summary>The debug render system we use.</summary>
        private DebugPhysicsRenderSystem _renderer;

        /// <summary>Whether to render the help text.</summary>
        private bool _showHelp;

        /// <summary>Whether to show profiling information.</summary>
        private bool _showProfile;

        /// <summary>The accumulated elapsed time since the last simulation update.</summary>
        private float _elapsedTimeAccumulator;

        /// <summary>The id of the mouse joint used for dragging bodies around.</summary>
        private int _mouseJoint = -1;

        /// <summary>A serialized snapshot of the scene.</summary>
        private Packet _snapshot;

        /// <summary>The hash of the simulation when the snapshot was created.</summary>
        private uint _snapshotHash;

        /// <summary>The zipped size of the snapshot.</summary>
        private int _snapshotCompressedSize;

        #endregion

        /// <summary>Draws the string in the next draw call.</summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments to put into the format string.</param>
        public static void DrawString(string format, params object[] args)
        {
            StringBuffer.AppendFormat(format, args);
            StringBuffer.Append("\n");
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestRunner"/> class.
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

        /// <summary>Initialize to the first test when the graphics device has been set up.</summary>
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(_graphics.GraphicsDevice);
            _font = new ResourceContentManager(Services, GameResource.ResourceManager).Load<SpriteFont>("ConsoleFont");

            _manager.AddSystem(new IndexSystem(16, 1));
            _manager.AddSystem(new GraphicsDeviceSystem(Content, _graphics) {Enabled = true});
            _manager.AddSystem(_physics = new PhysicsSystem(1 / UpdatesPerSecond, new Vector2(0, -10f)));
            _manager.AddSystem(_renderer = new DebugPhysicsRenderSystem {Enabled = true, Scale = 0.1f, Offset = new WorldPoint(0, -12)});

            _renderer.RenderFixtures = true;
            _renderer.RenderJoints = true;

            LoadTest(0);
        }

        /// <summary>Updates the simulation running the current test and checks whether we should switch to another test.</summary>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Only handle input when window has focus.
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            if (IsActive)
            {
                // Check for key presses and releases.
                foreach (Keys key in Enum.GetValues(typeof (Keys)))
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
                    OnMouseMove(
                        mousePosition,
                        mousePosition -
                        new Vector2(_lastMouseState.X, GraphicsDevice.Viewport.Height - _lastMouseState.Y));
                }
            }
            _lastKeyboardState = Keyboard.GetState();
            _lastMouseState = mouseState;

            StringBuffer.Clear();

            if (_manager != null)
            {
                DrawString(Tests[_currentTest].GetType().Name);
            }

            if (_snapshot != null)
            {
                DrawString(
                    "Got a save state: [{0:X}] @ {1:0.00}KB ({3:0.00}% compressed @ {2:0.00}KB)",
                    _snapshotHash,
                    (_snapshot.Length / 1024f),
                    (_snapshotCompressedSize / 1024f),
                    100f * _snapshotCompressedSize / _snapshot.Length);
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
F2           - Toggles profiler information and stats (current: {0}).
F3           - Toggle joint rendering. (current: {1})
F4           - Toggle contact points and normal rendering. (current: {2})
F5           - Toggle contact point normal impulse rendering. (current: {3})
F7           - Toggle center of mass rendering. (current: {4})
F8           - Toggle bounding box rendering. (current: {5})
S            - Toggle whether sleeping is allowed. (current: {6})

Left Arrow   - Previous test.
Right Arrow  - Next test.
Space        - Pause or unpause simulation.
Tab or Enter - Advance simulation one frame.
R            - Reload current test (keeping pause state).
K            - Create snapshot (for testing serialization).
L            - Load snapshot created with K.
C            - Test copying (creates simulation copy and uses it).",
                    _showProfile,
                    _renderer.RenderJoints,
                    _renderer.RenderContactPoints,
                    _renderer.RenderContactPointNormalImpulse,
                    _renderer.RenderCenterOfMass,
                    _renderer.RenderFixtureBounds,
                    _physics.AllowSleep);
            }
            else
            {
                DrawString("\nPress F1 for help.");
            }

            if (_showProfile && _physics != null)
            {
                DrawString(
                    @"
Bodies/Fixtures/Contacts/Joints/Tree depth: {25}/{26}/{27}/{28}/{29}
HPT: {24,5}       Last [Average] (Maximum)
Step          {0,7:0.00} [{1,7:0.00}] ({2,7:0.00})
Collide       {3,7:0.00} [{4,7:0.00}] ({5,7:0.00})
Solve         {6,7:0.00} [{7,7:0.00}] ({8,7:0.00})
SolveInit     {9,7:0.00} [{10,7:0.00}] ({11,7:0.00})
SolveVelocity {12,7:0.00} [{13,7:0.00}] ({14,7:0.00})
SolvePosition {15,7:0.00} [{16,7:0.00}] ({17,7:0.00})
Broadphase    {18,7:0.00} [{19,7:0.00}] ({20,7:0.00})
SolveTOI      {21,7:0.00} [{22,7:0.00}] ({23,7:0.00})",
                    _profile.Step.Last,
                    _profile.Step.Mean(),
                    _profile.Step.Max,
                    _profile.Collide.Last,
                    _profile.Collide.Mean(),
                    _profile.Collide.Max,
                    _profile.Solve.Last,
                    _profile.Solve.Mean(),
                    _profile.Solve.Max,
                    _profile.SolveInit.Last,
                    _profile.SolveInit.Mean(),
                    _profile.SolveInit.Max,
                    _profile.SolveVelocity.Last,
                    _profile.SolveVelocity.Mean(),
                    _profile.SolveVelocity.Max,
                    _profile.SolvePosition.Last,
                    _profile.SolvePosition.Mean(),
                    _profile.SolvePosition.Max,
                    _profile.Broadphase.Last,
                    _profile.Broadphase.Mean(),
                    _profile.Broadphase.Max,
                    _profile.SolveTOI.Last,
                    _profile.SolveTOI.Mean(),
                    _profile.SolveTOI.Max,
                    Stopwatch.IsHighResolution,
                    _physics.BodyCount,
                    "N/A" /* _physics.FixtureCount */,
                    _physics.ContactCount,
                    _physics.JointCount,
                    "N/A" /* _physics.IndexDepth */);
            }

            // Newline before any text current test may want to display.
            DrawString("");

            // Allow simulating?
            if ((_running || _runOnce) && _manager != null)
            {
                _elapsedTimeAccumulator += (float) gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_elapsedTimeAccumulator >= 1000f / UpdatesPerSecond)
                {
                    _manager.Update(0);
                    _runOnce = false;
                    _elapsedTimeAccumulator = 0;

                    // Update profiling data.
                    if (_physics != null)
                    {
                        var profile = _physics.Profile;
                        _profile.Step.Put(profile.Step);
                        _profile.Collide.Put(profile.Collide);
                        _profile.Solve.Put(profile.Solve);
                        _profile.SolveInit.Put(profile.SolveInit);
                        _profile.SolveVelocity.Put(profile.SolveVelocity);
                        _profile.SolvePosition.Put(profile.SolvePosition);
                        _profile.Broadphase.Put(profile.Broadphase);
                        _profile.SolveTOI.Put(profile.SolveTOI);
                    }
                }
                // Always update the tests, regardless of tick rate, for
                // proper text rendering.
                Tests[_currentTest].Update();
            }
        }

        /// <summary>Renders the current test scene.</summary>
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
        ///     Loads the test at the specified index. Returns silently if there are no tests known. Will wrap the number to
        ///     the valid range.
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

            // Clear our system, drop snapshot, clear references.
            _manager.Clear();
            _physics.Gravity = new Vector2(0, -10);
            if (reset)
            {
                _renderer.Offset = new WorldPoint(0, -12);
                _renderer.Scale = 0.1f;
                _snapshot = null;
            }
            _mouseJoint = -1;
            _profile.Reset();

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

        /// <summary>Called when the specified key was pressed.</summary>
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

                case Keys.K:
                    // Create a snapshot via serialization.
                    if (_manager != null)
                    {
                        // Kill the mouse joint because deserializing it would
                        // cause a joint that we do not control anymore.
                        if (_mouseJoint >= 0)
                        {
                            _manager.RemoveJoint(_mouseJoint);
                            _mouseJoint = -1;
                        }

                        using (var w = new StreamWriter("before.txt"))
                        {
                            w.Dump(_manager);
                        }

                        _snapshot = new Packet();
                        _snapshot.Write(_manager);
                        var hasher = new Hasher();
                        hasher.Write(_manager);
                        _snapshotHash = hasher.Value;
                        _snapshotCompressedSize =
                            SimpleCompression.Compress(_snapshot.GetBuffer(), _snapshot.Length).Length;
                    }
                    break;
                case Keys.L:
                    // Load a previously created snapshot.
                    if (_snapshot != null)
                    {
                        // Reset test and stuff to avoid invalid references.
                        _mouseJoint = -1;
                        _snapshot.Reset();
                        _snapshot.ReadPacketizableInto(_manager);

                        var hasher = new Hasher();
                        hasher.Write(_manager);
                        Debug.Assert(_snapshotHash == hasher.Value);
                    }
                    break;

                case Keys.C:
                    // Test copy implementation.
                    if (_manager != null)
                    {
                        var copy = new Manager();
                        copy.AddSystem(new PhysicsSystem(1 / UpdatesPerSecond, new Vector2(0, -10f)));
                        copy.AddSystem(new GraphicsDeviceSystem(Content, _graphics) {Enabled = true});
                        copy.AddSystem(
                            new DebugPhysicsRenderSystem {Enabled = true, Scale = 0.1f, Offset = new WorldPoint(0, -12)});

                        _manager.CopyInto(copy);
                        _manager.Clear();
                        copy.CopyInto(_manager);
                    }
                    break;

                case Keys.S:
                    // Toggle whether sleeping is allowed.
                    _physics.AllowSleep = !_physics.AllowSleep;
                    break;

                case Keys.F1:
                    // Toggle help display.
                    _showHelp = !_showHelp;
                    break;
                case Keys.F2:
                    // Toggle profiler information.
                    _showProfile = !_showProfile;
                    break;
                case Keys.F3:
                    // Toggle joints.
                    _renderer.RenderJoints = !_renderer.RenderJoints;
                    break;
                case Keys.F4:
                    // Toggle contact point and normals.
                    _renderer.RenderContactPoints = !_renderer.RenderContactPoints;
                    _renderer.RenderContactNormals = !_renderer.RenderContactNormals;
                    break;
                case Keys.F5:
                    // Toggle contact point normal impulse.
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

        /// <summary>Called when the specified key was released.</summary>
        /// <param name="key">The key that was released.</param>
        private void OnKeyUp(Keys key)
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnKeyUp(key);
            }
        }

        /// <summary>Called when the left button was pressed.</summary>
        private void OnLeftButtonDown()
        {
            if (_manager != null)
            {
                if (_mouseJoint >= 0)
                {
                    _manager.RemoveJoint(_mouseJoint);
                    _mouseJoint = -1;
                }
                var mouseWorldPoint = _renderer.ScreenToSimulation(
                    new Vector2(
                        Mouse.GetState().X,
                        Mouse.GetState().Y));
                var fixture = _physics.GetFixtureAt(mouseWorldPoint);
                if (fixture != null)
                {
                    _mouseJoint =
                        _manager.AddMouseJoint(fixture.Body, mouseWorldPoint, fixture.Body.Mass * 1000).Id;
                }
                Tests[_currentTest].OnLeftButtonDown();
            }
        }

        /// <summary>Called when the left button was released.</summary>
        private void OnLeftButtonUp()
        {
            if (_manager != null)
            {
                if (_mouseJoint >= 0)
                {
                    _manager.RemoveJoint(_mouseJoint);
                    _mouseJoint = -1;
                }
                Tests[_currentTest].OnLeftButtonUp();
            }
        }

        /// <summary>Called when the right button was pressed.</summary>
        private void OnRightButtonDown()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnRightButtonDown();
            }
        }

        /// <summary>Called when the right button was released.</summary>
        private void OnRightButtonUp()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnRightButtonUp();
            }
        }

        /// <summary>Called when the right button was pressed.</summary>
        private void OnWheelDown()
        {
            if (_manager != null)
            {
                Tests[_currentTest].OnWheelDown();
            }
        }

        /// <summary>Called when the right button was released.</summary>
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
                // Check if dragging a body. If so update the target.
                if (_mouseJoint >= 0)
                {
                    var mouseWorldPoint = _renderer.ScreenToSimulation(
                        new Vector2(
                            Mouse.GetState().X,
                            Mouse.GetState().Y));
                    ((MouseJoint) _manager.GetJointById(_mouseJoint)).Target = mouseWorldPoint;
                }

                Tests[_currentTest].OnMouseMove(mousePosition, delta);
            }
        }

        /// <summary>Used to accumulate profiling data over time.</summary>
        private sealed class Profile
        {
            private const int SampleCount = 240;

            public readonly DoubleSampling Step = new DoubleSampling(SampleCount);

            public readonly DoubleSampling Collide = new DoubleSampling(SampleCount);

            public readonly DoubleSampling Solve = new DoubleSampling(SampleCount);

            public readonly DoubleSampling SolveInit = new DoubleSampling(SampleCount);

            public readonly DoubleSampling SolveVelocity = new DoubleSampling(SampleCount);

            public readonly DoubleSampling SolvePosition = new DoubleSampling(SampleCount);

            public readonly DoubleSampling Broadphase = new DoubleSampling(SampleCount);

            public readonly DoubleSampling SolveTOI = new DoubleSampling(SampleCount);

            /// <summary>Resets all samplings.</summary>
            public void Reset()
            {
                Step.Reset();
                Collide.Reset();
                Solve.Reset();
                SolveInit.Reset();
                SolveVelocity.Reset();
                SolvePosition.Reset();
                Broadphase.Reset();
                SolveTOI.Reset();
            }
        }
    }
}