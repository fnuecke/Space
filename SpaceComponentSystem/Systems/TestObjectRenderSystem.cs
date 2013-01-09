using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Graphics;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Renders suns.</summary>
    public sealed class TestObjectRenderSystem : AbstractComponentSystem<SunRenderer>, IDrawingSystem
    {
        #region Fields

        /// <summary>The sun renderer we use.</summary>
        private static TestObject _testObject;

        /// <summary>The content manager used to load textures.</summary>
        private readonly ContentManager _content;

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        public bool Enabled { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="SunRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device service.</param>
        public TestObjectRenderSystem(ContentManager content, IGraphicsDeviceService graphics)
        {
            _content = content;
            if (_testObject == null)
            {
                _testObject = new TestObject(content, graphics);
                _testObject.LoadContent();
            }

            Enabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        ///     Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Set/get loop invariants.
            var transform = camera.Transform;
            _testObject.Time = frame / Settings.TicksPerSecond;

            // Render everything in sight.
            foreach (var entity in camera.VisibleEntities)
            {
                var component = (TestObjectRenderer) Manager.GetComponent(entity, TestObjectRenderer.TypeId);

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    RenderObject(component, ref transform);
                }
            }
        }

        /// <summary>Renders the specified sun.</summary>
        /// <param name="component">The component.</param>
        /// <param name="transform">The transform.</param>
        private void RenderObject(TestObjectRenderer component, ref FarTransform transform)
        {
            // Get absolute position of sun.
            var position = ((Transform) Manager.GetComponent(component.Entity, Transform.TypeId)).Translation;

            // Apply transformation.
            _testObject.Center = (Vector2) (position + transform.Translation);
            _testObject.SetTransform(transform.Matrix);
            _testObject.Color = component.Tint;

            // Set remaining parameters for draw.
            _testObject.SetSize(component.Radius * 2);

            // Load the texture if we don't have it yet.
            if (_testObject.SurfaceTexture == null)
            {
                _testObject.SurfaceTexture = _content.Load<Texture2D>("Textures/Asteroids/ast");
            }

            // And draw it.
            _testObject.Draw();
        }

        #endregion
    }
}