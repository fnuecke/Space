using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Engine.FarMath;
using Engine.Graphics;
using Engine.Math;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury;
using ProjectMercury.Renderers;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    class ProjectilePreviewControl : GraphicsDeviceControl
    {
        public ProjectileFactory[] Projectiles
        {
            set
            {
                if (_timer == null)
                {
                    _timer = new Timer
                    {
                        Interval = 1000 / 60,
                        Enabled = false
                    };
                    _timer.Tick += TimerOnTick;
                }
                _factories = value;
                _projectiles.Clear();
                _triggerWait = 0;
                _timer.Enabled = false;
                LoadContent();
            }
        }

        public FloatInterval TriggerSpeed { get; set; }

        private SpriteBatchRenderer _renderer;

        private PlainContentManager _content;

        private ProjectileFactory[] _factories;

        private readonly List<Projectile> _projectiles = new List<Projectile>();

        private FilledEllipse _circle;

        private SpriteBatch _batch;

        private Timer _timer;

        private float _triggerWait;

        private readonly IUniformRandom _random = new MersenneTwister();

        private Texture2D[] _models;

        private ParticleEffect[] _effects;

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
            if (_circle == null)
            {
                try
                {
                    _circle = new FilledEllipse(_content, GraphicsDevice)
                    {
                        Gradient = 2,
                        Color = Color.SlateBlue * 0.5f
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed initializing ellipse: " + ex);
                }
            }
            LoadContent();
        }

        private void LoadContent()
        {
            if (_factories != null && _content != null)
            {
                _models = new Texture2D[_factories.Length];
                _effects = new ParticleEffect[_factories.Length];
                try
                {
                    for (var i = 0; i < _factories.Length; i++)
                    {
                        _models[i] = string.IsNullOrWhiteSpace(_factories[i].Model) ? null : _content.Load<Texture2D>(_factories[i].Model);
                        _effects[i] = string.IsNullOrWhiteSpace(_factories[i].Effect) ? null : _content.Load<ParticleEffect>(_factories[i].Effect).DeepCopy();
                    }
                    _renderer.LoadContent(_content);
                    _timer.Enabled = true;
                }
                catch
                {
                }
            }
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            foreach (var projectile in _projectiles)
            {
                projectile.Velocity += projectile.Acceleration;
                if (projectile.Friction > 0)
                {
                    projectile.Velocity *= projectile.Friction;
                }
                projectile.Position += projectile.Velocity;
                projectile.TimeToLive -= _timer.Interval;
                if (projectile.Effect != null)
                {
                    projectile.Effect.Update(_timer.Interval / 1000f);
                }
            }
            _projectiles.RemoveAll(x => x.TimeToLive <= 0);

            _triggerWait -= _timer.Interval;
            if (_triggerWait <= 0)
            {
                for (var i = 0; i < _factories.Length; ++i)
                {
                    var projectile = new Projectile
                    {
                        Position = Vector2.Zero,
                        Acceleration = _factories[i].SampleAccelerationForce(0, _random),
                        Velocity =
                            _factories[i].SampleInitialDirectedVelocity(0, _random),
                        Friction = (1 - _factories[i].Friction),
                        Texture = _models[i],
                        Effect = _effects[i] == null ? null : _effects[i].DeepCopy(),
                        TimeToLive = _factories[i].TimeToLive * 1000,
                        EffectOffset = (FarPosition)_factories[i].EffectOffset,
                        CollisionRadius = _factories[i].CollisionRadius
                    };
                    if (projectile.Acceleration != Vector2.Zero)
                    {
                        projectile.Rotation = (float)Math.Atan2(projectile.Acceleration.Y, projectile.Acceleration.X);
                    }
                    else if (projectile.Velocity != Vector2.Zero)
                    {
                        projectile.Rotation = (float)Math.Atan2(projectile.Velocity.Y, projectile.Velocity.X);
                    }
                    if (projectile.Effect != null)
                    {
                        projectile.Effect.Initialise();
                        projectile.Effect.LoadContent(_content);
                    }
                    _projectiles.Add(projectile);
                }

                if (TriggerSpeed != null)
                {
                    _triggerWait = (TriggerSpeed.Low + (TriggerSpeed.High - TriggerSpeed.Low) * (float)_random.NextDouble()) * 1000;
                }
                else
                {
                    _triggerWait = 1000f;
                }
            }

            Refresh();
        }

        protected override void Draw()
        {
            GraphicsDevice.Clear(Color.FromNonPremultiplied(64, 64, 64, 255));
            if (_factories == null)
            {
                return;
            }
            if (_batch == null)
            {
                _batch = new SpriteBatch(GraphicsDevice);
            }
            _batch.Begin();
            foreach (var projectile in _projectiles)
            {
                try
                {
                    var rotation = projectile.Rotation;

                    var position = new Vector2(Width / 2f + projectile.Position.X, Height / 2f + projectile.Position.Y);
                    if (_circle != null)
                    {
                        _circle.SetCenter(position.X, position.Y);
                        _circle.SetSize(projectile.CollisionRadius * 2);
                        _circle.Draw();
                    }

                    if (projectile.Effect != null)
                    {
                        projectile.Effect.Trigger(projectile.EffectOffset);
                        FarTransform t;
                        t.Translation = FarPosition.Zero;
                        t.Matrix = Matrix.CreateRotationZ(rotation) *
                                   Matrix.CreateTranslation(position.X, position.Y, 0);
                        _renderer.RenderEffect(projectile.Effect, ref t);
                    }

                    if (projectile.Texture != null)
                    {
                        Vector2 origin;
                        origin.X = projectile.Texture.Width / 2f;
                        origin.Y = projectile.Texture.Height / 2f;
                        _batch.Draw(projectile.Texture, position, null, Color.White, rotation, origin, 1f,
                                    SpriteEffects.None, 0);
                    }
                }
                catch
                {
                }
            }
            _batch.End();
        }

        private sealed class Projectile
        {
            public Vector2 Position;

            public Vector2 Velocity;

            public Vector2 Acceleration;

            public float Friction;

            public float Rotation;

            public Texture2D Texture;

            public ParticleEffect Effect;

            public float TimeToLive;

            public FarPosition EffectOffset;

            public float CollisionRadius;
        }
    }
}
