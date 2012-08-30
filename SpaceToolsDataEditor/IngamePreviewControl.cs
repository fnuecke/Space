﻿using System;
using System.Linq;
using System.Windows.Forms;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Systems;
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

        private DebugCollisionBoundsRenderSystem _collisionBounds;

        private DebugSlotRenderSystem _slots;

        private Grid _grid;

        private PlanetMaxBoundsRenderer _maxBounds;

        private int _sunId;

        private long _frame = 0;

        public IngamePreviewControl()
        {
            _contextMenu.Items.Add(new ToolStripMenuItem("Collision bounds", null, null, "collbounds") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Planet/sun maximum radius", null, null, "maxbounds") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Equipment slots", null, null, "slots") { CheckOnClick = true, CheckState = CheckState.Checked });
            _contextMenu.Items.Add(new ToolStripMenuItem("Thruster effects", null, null, "thrusterfx") { CheckOnClick = true, CheckState = CheckState.Checked });
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
                _batch = new SpriteBatch(GraphicsDevice);
            }
            if (_grid == null)
            {
                _grid = new Grid(_content, GraphicsDevice);
            }

            FactoryLibrary.LoadContent(_content);

            _manager.AddSystems(new AbstractSystem[]
            {
                    new AvatarSystem(),
                    new ShipInfoSystem(),
                    new CharacterSystem<AttributeType>(),
                    new FrictionSystem(),
                    new AccelerationSystem(),
                    new VelocitySystem(),
                    new SpinSystem(),
                    new TranslationSystem(),
                    new IndexSystem(16, 64),
                    new ExpirationSystem(),
                    new InventorySystem(),
                    new ItemSlotSystem(),
                    new ItemEffectSystem(),
                    new RegeneratingValueSystem(),
                    new CameraCenteredInterpolationSystem(GraphicsDevice, () => 20f),
                    new CameraSystem(GraphicsDevice, null, null),

                    new PlanetMaxBoundsRenderer(_content, GraphicsDevice),
                    new DebugCollisionBoundsRenderSystem(_content, GraphicsDevice),

                    new PlanetRenderSystem(_content, GraphicsDevice),
                    new SunRenderSystem(_content, GraphicsDevice, _batch),

                    new CameraCenteredTextureRenderSystem(_content, _batch),
                    new CameraCenteredParticleEffectSystem(_content, (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService)), () => 20f),
                    
                    new DebugSlotRenderSystem(_content, GraphicsDevice), 
                    
            });
            // Fix position to avoid querying unavailable services.
            ((CameraSystem)_manager.GetSystem(CameraSystem.TypeId)).CameraPositon = FarPosition.Zero;

            // Enable debug render systems.
            _collisionBounds = (DebugCollisionBoundsRenderSystem)_manager.GetSystem(DebugCollisionBoundsRenderSystem.TypeId);
            _slots = (DebugSlotRenderSystem)_manager.GetSystem(DebugSlotRenderSystem.TypeId);
            _maxBounds = (PlanetMaxBoundsRenderer)Manager.GetSystem(PlanetMaxBoundsRenderer.TypeId);

            Clear();

            _updateTimer.Enabled = true;
            _drawTimer.Enabled = true;

            GraphicsDeviceOnDeviceReset(null, null);
            GraphicsDevice.DeviceReset += GraphicsDeviceOnDeviceReset;
        }

        private void GraphicsDeviceOnDeviceReset(object sender, EventArgs eventArgs)
        {
            if (_target != null)
            {
                _target.Dispose();
            }

            // Get settings. We use the whole screen to draw.
            var pp = GraphicsDevice.PresentationParameters;

            var width = pp.BackBufferWidth;
            var height = pp.BackBufferHeight;

            // Create a target for rendering the main sun texture.
            _target = new RenderTarget2D(GraphicsDevice, width, height, false, pp.BackBufferFormat, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        protected override void Draw()
        {
            GraphicsDevice.SetRenderTarget(_target);
            GraphicsDevice.Clear(Color.FromNonPremultiplied(64, 64, 64, 255));
            try
            {
                foreach (var component in _manager.Components)
                {
                    if (component is ParticleEffects)
                    {
                        ((ParticleEffects)component).SetGroupEnabled(ParticleEffects.EffectGroup.Thruster, ((ToolStripMenuItem)_contextMenu.Items["thrusterfx"]).Checked);
                    }
                }

                if (_collisionBounds != null)
                {
                    _collisionBounds.IsEnabled = ((ToolStripMenuItem)_contextMenu.Items["collbounds"]).Checked;
                }

                if (_slots != null)
                {
                    _slots.IsEnabled = ((ToolStripMenuItem)_contextMenu.Items["slots"]).Checked;
                }

                if (_maxBounds != null)
                {
                    _maxBounds.IsEnabled = ((ToolStripMenuItem)_contextMenu.Items["maxbounds"]).Checked;
                }

                if (_grid != null && ((ToolStripMenuItem)_contextMenu.Items["grid"]).Checked)
                {
                    _grid.SetSize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                    _grid.SetCenter(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
                    _grid.Draw();
                }

                if (_sunId > 0)
                {
                    var mousePos = PointToClient(MousePosition);
                    FarPosition sunPos;
                    sunPos.X = mousePos.X - Width / 2f;
                    sunPos.Y = mousePos.Y - Height / 2f;
                    var t = (Transform)Manager.GetComponent(_sunId, Transform.TypeId);
                    t.SetTranslation(sunPos);
                    t.ApplyTranslation();
                }

                _manager.Draw(_frame, _drawTimer.Interval);
            }
            catch (Exception ex)
            {
                Clear();
            }
            GraphicsDevice.SetRenderTarget(null);
            _batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _batch.Draw(_target, GraphicsDevice.PresentationParameters.Bounds, Color.White);
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

            public bool IsEnabled { get; set; }

            public PlanetMaxBoundsRenderer(ContentManager content, GraphicsDevice graphics)
            {
                _ellipse = new Ellipse(content, graphics)
                {
                    Color = Color.LightCyan * 0.75f,
                    Thickness = 5f,
                    BlendState = BlendState.Additive
                };
            }

            public void Draw(long frame, float elapsedMilliseconds)
            {
                var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);
                foreach (var component in Components)
                {
                    _ellipse.Transform = camera.Transform.Matrix;
                    _ellipse.SetSize(component.MaxRadius * 2);
                    _ellipse.Draw();
                }
            }
        }
    }
}