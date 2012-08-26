using System;
using System.Windows.Forms;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
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

        private readonly Manager _manager = new Manager();

        public IngamePreviewControl()
        {
            _contextMenu.Items.Add(new ToolStripMenuItem("Thruster effects", null, null, "thrusterfx") {CheckOnClick = true, CheckState = CheckState.Checked});

            ContextMenuStrip = _contextMenu;
            _updateTimer.Tick += (sender, args) => _manager.Update(0);
            _drawTimer.Tick += (sender, args) => Refresh();
        }

        public void Clear()
        {
            _manager.Clear();
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

                    new DebugCollisionBoundsRenderSystem(_content, GraphicsDevice),

                    new CameraCenteredTextureRenderSystem(_content, _batch),
                    new CameraCenteredParticleEffectSystem(_content, (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService)), () => 20f),
                    
                    new DebugSlotRenderSystem(_content, GraphicsDevice), 
                    
            });
            // Fix position to avoid querying unavailable services.
            ((CameraSystem)_manager.GetSystem(CameraSystem.TypeId)).CameraPositon = FarPosition.Zero;

            // Enable debug render systems.
            ((DebugCollisionBoundsRenderSystem)_manager.GetSystem(DebugCollisionBoundsRenderSystem.TypeId)).IsEnabled = true;
            ((DebugSlotRenderSystem)_manager.GetSystem(DebugSlotRenderSystem.TypeId)).IsEnabled = true;

            _updateTimer.Enabled = true;
            _drawTimer.Enabled = true;
        }

        protected override void Draw()
        {
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

                _manager.Draw(0, _drawTimer.Interval);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
