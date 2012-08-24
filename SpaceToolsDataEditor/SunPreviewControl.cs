using System;
using System.Windows.Forms;
using Engine.Graphics;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Factories;
using Space.Graphics;

namespace Space.Tools.DataEditor
{
    class SunPreviewControl : GraphicsDeviceControl
    {
        public SunFactory Sun
        {
            set
            {
                if (_timer == null)
                {
                    _timer = new Timer
                    {
                        Interval = 1000 / 30,
                        Enabled = false
                    };
                    _timer.Tick += TimerOnTick;
                }
                _factory = value;
                if (value == null)
                {
                    _timer.Enabled = false;
                }
                else if (_sun != null)
                {
                    _sun.SetSize(_factory.Radius.Low * 2 * 0.95f);
                    LoadContent();
                }
            }
        }

        private PlainContentManager _content;

        private SunFactory _factory;

        private Sun _sun;

        private Ellipse _circle;

        private Timer _timer;

        private SpriteBatch _batch;

        private Vector2 _surfaceRotation;
        private Vector2 _primaryTurbulenceRotation;
        private Vector2 _secondaryTurbulenceRotation;

        private RenderTarget2D _target;

        public SunPreviewControl()
        {
            var random = new MersenneTwister();

            _surfaceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
            _surfaceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
            _surfaceRotation.Normalize();

            _primaryTurbulenceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
            _primaryTurbulenceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
            _primaryTurbulenceRotation.Normalize();

            _secondaryTurbulenceRotation.X = (float)(random.NextDouble() - 0.5) * 2;
            _secondaryTurbulenceRotation.Y = (float)(random.NextDouble() - 0.5) * 2;
            _secondaryTurbulenceRotation.Normalize();
        }

        protected override void Initialize()
        {
            if (_content == null)
            {
                _content = new PlainContentManager(Services);
                _batch = new SpriteBatch(GraphicsDevice);
                RecreateRenderTarget();
            }
            if (_sun == null)
            {
                try
                {
                    _sun = new Sun(_content, GraphicsDevice);
                    _sun.SurfaceRotation = _surfaceRotation;
                    _sun.PrimaryTurbulenceRotation = _primaryTurbulenceRotation;
                    _sun.SecondaryTurbulenceRotation = _secondaryTurbulenceRotation;
                    if (_factory != null)
                    {
                        _sun.SetSize(_factory.Radius.High * 2 * 0.95f);
                        LoadContent();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed initializing sun: " + ex);
                }
            }
            if (_circle == null)
            {
                try
                {
                    _circle = new Ellipse(_content, GraphicsDevice)
                    {
                        Thickness = 2,
                        Color = Color.SlateGray * 0.5f
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed initializing ellipse: " + ex);
                }
            }
            GraphicsDevice.DeviceReset += GraphicsDeviceOnDeviceReset;
        }

        private void GraphicsDeviceOnDeviceReset(object sender, EventArgs eventArgs)
        {
            if (_sun != null)
            {
                _sun.InvalidateVertices();
            }
            RecreateRenderTarget();
        }

        private void RecreateRenderTarget()
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

        private void LoadContent()
        {
            if (_circle != null)
            {
                _circle.SetSize(_factory.Radius.High * 2);
            }
            try
            {
                _sun.LoadContent(_batch, _content);
                _timer.Enabled = true;
            }
            catch
            {
            }
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            Refresh();
        }

        protected override void Draw()
        {
            GraphicsDevice.SetRenderTarget(_target);
            GraphicsDevice.Clear(Color.FromNonPremultiplied(64, 64, 64, 255));
            if (_sun == null || _factory == null)
            {
                return;
            }
            _sun.Time = (float)(DateTime.UtcNow - DateTime.Today).TotalMilliseconds / 1000;
            _sun.SetCenter(Width / 2f, Height / 2f);
            try
            {
                _sun.Draw();
            }
            catch
            {
            }
            GraphicsDevice.SetRenderTarget(null);
            _batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _batch.Draw(_target, GraphicsDevice.PresentationParameters.Bounds, Color.White);
            _batch.End();
            if (_circle != null)
            {
                _circle.SetCenter(Width / 2f, Height / 2f);
                _circle.Draw();
            }
        }
    }
}
