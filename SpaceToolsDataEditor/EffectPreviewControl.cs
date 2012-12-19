using System;
using System.Windows.Forms;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury;
using ProjectMercury.Renderers;

namespace Space.Tools.DataEditor
{
    class EffectPreviewControl : GraphicsDeviceControl
    {
        public string Effect
        {
            get { return _assetName; }
            set
            {
                _assetName = value;
                _effect = null;
                if (_timer != null)
                {
                    _timer.Enabled = false;
                }
                _lastTrigger = DateTime.MinValue;
                LoadContent();
                Invalidate();
            }
        }

        private string _assetName;

        private ParticleEffect _effect;

        private SpriteBatchRenderer _renderer;

        private Timer _timer;

        private PlainContentManager _content;

        private SpriteBatch _batch;

        private DateTime _lastTrigger;

        protected override void Initialize()
        {
            if (_content == null)
            {
                _content = new PlainContentManager(Services);
            }
            if (_renderer == null)
            {
                _renderer = new SpriteBatchRenderer();
            }
            _renderer.GraphicsDeviceService = (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService));
            if (!string.IsNullOrWhiteSpace(_assetName) && _effect == null)
            {
                LoadContent();
            }
        }

        private void LoadContent()
        {
            if (_renderer == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(_assetName))
            {
                return;
            }
            try
            {
                _renderer.LoadContent(_content);
                _effect = _content.Load<ParticleEffect>(_assetName);
                if (_timer == null)
                {
                    _timer = new Timer
                    {
                        Interval = 1000 / 30
                    };
                    _timer.Tick += TimerOnTick;
                }
                _timer.Enabled = true;
            }
            catch
            {
            }
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            bool allowTrigger = (DateTime.UtcNow - _lastTrigger).TotalSeconds >= 1;
            foreach (var emitter in _effect)
            {
                // Force a minimum wait for effects that may trigger multiple
                // times per frame (one-shots used by different entities).
                if (emitter.MinimumTriggerPeriod <= 0)
                {
                    if (allowTrigger)
                    {
                        _lastTrigger = DateTime.UtcNow;
                    }
                    else
                    {
                        continue;
                    }
                }
                emitter.Trigger(FarPosition.Zero);
            }
            _effect.Update(_timer.Interval / 1000f);
            Refresh();
        }

        protected override void Draw()
        {
            GraphicsDeviceManager.GraphicsDevice.Clear(Color.FromNonPremultiplied(64, 64, 64, 255));
            if (_effect == null)
            {
                return;
            }
            if (_batch == null)
            {
                _batch = new SpriteBatch(GraphicsDeviceManager.GraphicsDevice);
            }
            FarTransform transform;
            transform.Matrix = Matrix.Identity;
            transform.Translation.X = Width / 2;
            transform.Translation.Y = Height / 2;
            _renderer.RenderEffect(_effect, ref transform);
        }
    }
}
