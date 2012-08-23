using System;
using System.Windows.Forms;
using System.Xml;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
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
                LoadContent();
            }
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            _effect.Trigger(FarPosition.Zero);
            _effect.Update(_timer.Interval / 1000f);
            Refresh();
        }

        private string _assetName;

        private ParticleEffect _effect;

        private SpriteBatchRenderer _renderer;

        private Timer _timer;

        private TextureContentManager _content;

        private SpriteBatch _batch;

        protected override void Initialize()
        {
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
            if (string.IsNullOrWhiteSpace(_assetName))
            {
                return;
            }
            var fileName = ContentProjectManager.GetFileForEffectAsset(_assetName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }
            try
            {
                using (var xmlReader = XmlReader.Create(fileName))
                {
                    var effect = IntermediateSerializer.Deserialize<ParticleEffect>(xmlReader, null);
                    effect.Initialise();
                    if (_content == null)
                    {
                        _content = new TextureContentManager(Services);
                    }
                    effect.LoadContent(_content);
                    _effect = effect;
                }
                if (_timer == null)
                {
                    _timer = new Timer
                    {
                        Interval = 16
                    };
                    _timer.Tick += TimerOnTick;
                }
                _timer.Enabled = true;
            }
            catch
            {
            }
        }

        protected override void Draw()
        {
            if (_effect == null)
            {
                return;
            }
            if (_batch == null)
            {
                _batch = new SpriteBatch(GraphicsDevice);
            }
            FarTransform transform;
            transform.Matrix = Matrix.Identity;
            transform.Translation.X = Width / 2;
            transform.Translation.Y = Height / 2;
            _renderer.RenderEffect(_effect, ref transform);

            _batch.Draw(_effect[0].ParticleTexture, Vector2.Zero, Color.White);
        }
    }
}
