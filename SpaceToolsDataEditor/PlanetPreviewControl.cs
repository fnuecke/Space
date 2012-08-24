using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Factories;
using Space.Graphics;

namespace Space.Tools.DataEditor
{
    class PlanetPreviewControl : GraphicsDeviceControl
    {
        public PlanetFactory Planet
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
                else if (_planet != null)
                {
                    LoadContent();
                }
            }
        }

        private PlainContentManager _content;

        private PlanetFactory _factory;

        private Planet _planet;

        private Timer _timer;

        protected override void Initialize()
        {
            if (_content == null)
            {
                _content = new PlainContentManager(Services);
            }
            if (_planet == null)
            {
                try
                {
                    _planet = new Planet(_content, GraphicsDevice);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed initializing planet: " + ex);
                }
                LoadContent();
            }
            GraphicsDevice.DeviceReset += GraphicsDeviceOnDeviceReset;
        }

        private void GraphicsDeviceOnDeviceReset(object sender, EventArgs eventArgs)
        {
            _planet.InvalidateVertices();
        }

        private void LoadContent()
        {
            if (_factory != null)
            {
                _planet.SurfaceRotation = new Vector2(_factory.RotationSpeed.High, _factory.RotationSpeed.Low);
                _planet.SurfaceTint = _factory.SurfaceTint;
                _planet.AtmosphereTint = _factory.AtmosphereTint;
                _planet.SetSize(_factory.Radius.High);
                try
                {
                    _planet.SurfaceTexture = _content.Load<Texture2D>(_factory.Texture);
                    _timer.Enabled = true;
                }
                catch
                {
                }
            }
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            Refresh();
        }

        protected override void Draw()
        {
            GraphicsDevice.Clear(Color.FromNonPremultiplied(64, 64, 64, 255));
            if (_planet == null || _factory == null)
            {
                return;
            }
            _planet.Time = (float)(DateTime.UtcNow - DateTime.Today).TotalMilliseconds / 1000;
            Vector2 toLight;
            var mousePos = PointToClient(MousePosition);
            toLight.X = mousePos.X - Width / 2f;
            toLight.Y = mousePos.Y - Height / 2f;
            toLight.Normalize();
            _planet.LightDirection = toLight;
            _planet.SetCenter(Width / 2f, Height / 2f);
            try
            {
                _planet.Draw();
            }
            catch
            {
            }
        }
    }
}
