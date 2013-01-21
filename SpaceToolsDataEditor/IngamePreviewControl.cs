using System;
using System.Linq;
using System.Windows.Forms;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Systems;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.Tools.DataEditor
{
    sealed class IngamePreviewControl : GraphicsDeviceControl
    {
        public IManager Manager { get { return _manager; } }

        public int SunId { get { return _sunId; } }
        
        private readonly Timer _updateTimer = new Timer
        {
            Interval = 1000 / 20,
            Enabled = false
        };

        private readonly Timer _drawTimer = new Timer
        {
            Interval = 1000 / 60,
            Enabled = false
        };

        private readonly ContextMenuStrip _contextMenu = new ContextMenuStrip();

        private ContentManager _content;

        private SpriteBatch _batch;

        private RenderTarget2D _target;

        private readonly Manager _manager = new Manager();

        private DebugSlotRenderSystem _slots;

        private Grid _grid;

        private PlanetMaxBoundsRenderer _maxBounds;

        private int _sunId;

        private long _frame;

        private ShieldRenderSystem _shields;

        public IngamePreviewControl()
        {
            _contextMenu.Items.Add(new ToolStripMenuItem("Collision bounds", null, null, "collbounds") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Planet/sun maximum radius", null, null, "maxbounds") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Equipment slots", null, null, "slots") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Thruster effects", null, null, "thrusterfx") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Shield effects", null, null, "shieldfx") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Show grid", null, null, "grid") { CheckOnClick = true, CheckState = CheckState.Checked });

            ContextMenuStrip = _contextMenu;
            _updateTimer.Tick += (sender, args) => _manager.Update(_frame++);
            _drawTimer.Tick += (sender, args) => Invalidate();
        }

        public void Clear()
        {
            _manager.Clear();
            if (_manager.Systems.Any())
            {
                // Add a "sun" (where light comes from for renderers).
                _sunId = _manager.AddEntity();
                _manager.AddComponent<Transform>(_sunId);
            }
        }

        protected override void Initialize()
        {
            if (_content == null)
            {
                _content = new PlainContentManager(Services);
            }
            if (_batch == null)
            {
                _batch = new SpriteBatch(GraphicsDeviceManager.GraphicsDevice);
            }
            if (_grid == null)
            {
                _grid = new Grid(_content, GraphicsDeviceManager);
                _grid.LoadContent();
            }

            FactoryLibrary.LoadContent(_content);

            _manager.AddSystems(new AbstractSystem[]
            {
                    new AvatarSystem(),
                    new ShipInfoSystem(),
                    new CharacterSystem<AttributeType>(),
                    new FrictionSystem(),
                    new VelocitySystem(),
                    new TranslationSystem(),
                    new IndexSystem(16, 1),
                    new ExpirationSystem(),
                    new InventorySystem(),
                    new ItemSlotSystem(),
                    new ItemEffectSystem(),
                    new RegeneratingValueSystem(),

                    new GraphicsDeviceSystem(_content, GraphicsDeviceManager) {Enabled = true},

                    new CameraCenteredInterpolationSystem(() => 20f) {Enabled = true},
                    new LocalPlayerSystem(null),
                    new CameraSystem(GraphicsDeviceManager.GraphicsDevice, null) {Enabled = true},

                    new PlanetMaxBoundsRenderer(_content, GraphicsDeviceManager) {Enabled = true},

                    new PlanetRenderSystem {Enabled = true},
                    new SunRenderSystem {Enabled = true},

                    new CameraCenteredTextureRenderSystem {Enabled = true},
                    new CameraCenteredParticleEffectSystem(() => 20f) {Enabled = true},
                    new ShieldRenderSystem {Enabled = true},
                    
                    new DebugSlotRenderSystem {Enabled = true}
                    
            });
            // Fix position to avoid querying unavailable services.
            ((CameraSystem)_manager.GetSystem(CameraSystem.TypeId)).CameraPosition = FarPosition.Zero;

            // Enable debug render systems.
            _slots = (DebugSlotRenderSystem)_manager.GetSystem(DebugSlotRenderSystem.TypeId);
            _maxBounds = (PlanetMaxBoundsRenderer)Manager.GetSystem(PlanetMaxBoundsRenderer.TypeId);
            _shields = (ShieldRenderSystem)Manager.GetSystem(ShieldRenderSystem.TypeId);

            Clear();

            _updateTimer.Enabled = true;
            _drawTimer.Enabled = true;

            GraphicsDeviceOnDeviceReset(null, null);
            GraphicsDeviceManager.GraphicsDevice.DeviceReset += GraphicsDeviceOnDeviceReset;
        }

        private void GraphicsDeviceOnDeviceReset(object sender, EventArgs eventArgs)
        {
            if (_target != null)
            {
                _target.Dispose();
            }

            // Get settings. We use the whole screen to draw.
            var pp = GraphicsDeviceManager.GraphicsDevice.PresentationParameters;

            var width = pp.BackBufferWidth;
            var height = pp.BackBufferHeight;

            // Create a target for rendering the main sun texture.
            _target = new RenderTarget2D(GraphicsDeviceManager.GraphicsDevice, width, height, false, pp.BackBufferFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        protected override void Draw()
        {
            // Only do the actual rendering if we have our environment.
            if (_target == null)
            {
                // Otherwise just clear the screen.
                GraphicsDeviceManager.GraphicsDevice.Clear(Color.FromNonPremultiplied(64, 64, 64, 255));
                return;
            }

            // OK, set our custom render target and clear it.
            GraphicsDeviceManager.GraphicsDevice.SetRenderTarget(_target);
            GraphicsDeviceManager.GraphicsDevice.Clear(Color.FromNonPremultiplied(64, 64, 64, 255));
            try
            {
                // Toggle systems depending on the check state in the context menu.
                foreach (var component in _manager.Components)
                {
                    if (component is ParticleEffects)
                    {
                        ((ParticleEffects)component).SetGroupEnabled(ParticleEffects.EffectGroup.Thruster, ((ToolStripMenuItem)_contextMenu.Items["thrusterfx"]).Checked);
                    }
                }
                if (_slots != null)
                {
                    _slots.Enabled = ((ToolStripMenuItem)_contextMenu.Items["slots"]).Checked;
                }
                if (_maxBounds != null)
                {
                    _maxBounds.Enabled = ((ToolStripMenuItem)_contextMenu.Items["maxbounds"]).Checked;
                }
                if (_shields != null)
                {
                    _shields.Enabled = ((ToolStripMenuItem)_contextMenu.Items["shieldfx"]).Checked;
                }

                // Render the grid in the background.
                if (_grid != null && ((ToolStripMenuItem)_contextMenu.Items["grid"]).Checked)
                {
                    _grid.SetSize(GraphicsDeviceManager.GraphicsDevice.Viewport.Width, GraphicsDeviceManager.GraphicsDevice.Viewport.Height);
                    _grid.SetCenter(GraphicsDeviceManager.GraphicsDevice.Viewport.Width / 2f, GraphicsDeviceManager.GraphicsDevice.Viewport.Height / 2f);
                    _grid.Draw();
                }

                // Render our light source, if it's set. This is for lighting of objects that
                // support it (only planets for now).
                if (_sunId > 0)
                {
                    var mousePos = PointToClient(MousePosition);
                    FarPosition sunPos;
                    sunPos.X = mousePos.X - Width / 2f;
                    sunPos.Y = mousePos.Y - Height / 2f;
                    var t = (ITransform)Manager.GetComponent(_sunId, TransformTypeId);
                    t.Position = sunPos;
                }

                // Draw our mini simulation.
                _manager.Draw(_frame, _drawTimer.Interval);
            }
            catch (Exception ex)
            {
                // Something went wrong, so we'll just clear the manager, hoping that'll at least
                // keep the error from happening over and over again.
                Clear();

                Console.WriteLine(ex);
            }

            // Switch back to main backbuffer and copy over our render target.
            GraphicsDeviceManager.GraphicsDevice.SetRenderTarget(null);
            _batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _batch.Draw(_target, GraphicsDeviceManager.GraphicsDevice.PresentationParameters.Bounds, Color.White);
            _batch.End();
        }

        public sealed class PlanetMaxBounds : Engine.ComponentSystem.Components.Component
        {
            #region Type ID

            /// <summary>
            /// The unique type ID for this object, by which it is referred to in the manager.
            /// </summary>
            public static readonly int TypeId = CreateTypeId();

            /// <summary>
            /// The type id unique to the entity/component system in the current program.
            /// </summary>
            public override int GetTypeId()
            {
                return TypeId;
            }

            #endregion

            public float MaxRadius;
        }

        private sealed class PlanetMaxBoundsRenderer : AbstractComponentSystem<PlanetMaxBounds>, IDrawingSystem
        {
            #region Type ID

            /// <summary>
            /// The unique type ID for this system, by which it is referred to in the manager.
            /// </summary>
            public static readonly int TypeId = CreateTypeId();

            #endregion

            private readonly Ellipse _ellipse;

            public bool Enabled { get; set; }

            public PlanetMaxBoundsRenderer(ContentManager content, IGraphicsDeviceService graphics)
            {
                _ellipse = new Ellipse(content, graphics)
                {
                    Color = Color.LightCyan * 0.75f,
                    Thickness = 5f,
                    BlendState = BlendState.Additive
                };
                _ellipse.LoadContent();
            }

            public void Draw(long frame, float elapsedMilliseconds)
            {
                var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);
                foreach (var component in Components)
                {
                    _ellipse.Transform = camera.Transform;
                    _ellipse.SetSize(component.MaxRadius * 2);
                    _ellipse.Draw();
                }
            }
        }
    }
}
