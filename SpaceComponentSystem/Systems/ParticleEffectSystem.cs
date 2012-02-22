using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury;
using ProjectMercury.Renderers;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Controls the particle components in a game, passing them some
    /// information about how to render themselves.
    /// </summary>
    public class ParticleEffectSystem : AbstractComponentSystem<Components.ParticleEffects>
    {
        #region Fields

        /// <summary>
        /// Content manager used for loading particle effect descriptors and
        /// textures.
        /// </summary>
        private readonly ContentManager _content;

        /// <summary>
        /// The renderer used to draw effects.
        /// </summary>
        private readonly SpriteBatchRenderer _renderer;

        /// <summary>
        /// Cached known particle effects.
        /// </summary>
        private readonly Dictionary<string, ParticleEffect> _effects = new Dictionary<string, ParticleEffect>();

        /// <summary>
        /// Whether this is the sound system thats doing the actual "rendering"
        /// for the simulation the component system belongs to, i.e. whether
        /// Draw is called for this instance. Only that system may actually
        /// play sounds.
        /// </summary>
        private bool _isDrawingInstance;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticleEffectSystem"/> class.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="graphics">The graphics.</param>
        public ParticleEffectSystem(Game game, IGraphicsDeviceService graphics)
        {
            _content = game.Content;
            _renderer = new SpriteBatchRenderer();
            _renderer.GraphicsDeviceService = graphics;
            _renderer.LoadContent(game.Content);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates all particle effects.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(GameTime gameTime, long frame)
        {
            base.Update(gameTime, frame);

            // Only update in the main instance, otherwise effects play too fast.
            if (!_isDrawingInstance)
            {
                return;
            }

            // Update all known effects.
            foreach (var effect in _effects.Values)
            {
                effect.Update((float)gameTime.ElapsedGameTime.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Flags our system as the presenting instance and renders all effects.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame that should be rendered.</param>
        public override void Draw(GameTime gameTime, long frame)
        {
            base.Draw(gameTime, frame);

            _isDrawingInstance = true;

            // Render all known effects.
            var translation = GetTranslation();
            var transform = Matrix.CreateTranslation(translation.X, translation.Y, 0);
            foreach (var effect in _effects.Values)
            {
                _renderer.RenderEffect(effect, ref transform);
            }
        }

        /// <summary>
        /// Emits particles for enabled particle effects.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="frame"></param>
        /// <param name="component"></param>
        protected override void UpdateComponent(GameTime gameTime, long frame, Components.ParticleEffects component)
        {
            foreach (var effect in component.Effects)
            {
                var offset = effect.Item2;
                Play(effect.Item1, component.Entity, ref offset);
            }
        }

        /// <summary>
        /// Gets the translation to use for rendering effects.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        protected virtual Vector2 GetTranslation()
        {
            return Vector2.Zero;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Plays the specified effect.
        /// </summary>
        /// <param name="effectName">The effect.</param>
        /// <param name="position">The position.</param>
        public void Play(string effectName, ref Vector2 position)
        {
            // Do not play sounds if this isn't the main sound system thats
            // used for the presentation.
            if (!_isDrawingInstance)
            {
                return;
            }

            // Get current translation, only trigger effects that are in
            // visible range.
            var translation = GetTranslation();
            var bounds = _renderer.GraphicsDeviceService.GraphicsDevice.Viewport.Bounds;
            bounds.Inflate(256, 256);
            if (!bounds.Contains((int)translation.X, (int)translation.Y))
            {
                return;
            }

            // Let there be sound!
            var effect = GetEffect(effectName);
            
            effect.Trigger(position);
        }

        /// <summary>
        /// Plays a sound cue with the specified name as if it were emitted by
        /// the specified entity.
        /// </summary>
        /// <param name="effect">The name of the effect to play.</param>
        /// <param name="entity">The entity that emits the effect.</param>
        /// <param name="offset">The offset of the effect to the center of the entity.</param>
        /// <remarks>
        /// The entity must have a <c>Transform</c> component.
        /// </remarks>
        public void Play(string effect, int entity, ref Vector2 offset)
        {
            var transform = Manager.GetComponent<Transform>(entity);
            var position = transform.Translation + offset;
            var rotation = transform.Rotation + MathHelper.Pi;

            Play(effect, ref position);
        }

        /// <summary>
        /// Plays a sound cue with the specified name as if it were emitted by
        /// the specified entity.
        /// </summary>
        /// <param name="effect">The name of the effect to play.</param>
        /// <param name="entity">The entity that emits the effect.</param>
        /// <remarks>
        /// The entity must have a <c>Transform</c> component.
        /// </remarks>
        public void Play(string effect, int entity)
        {
            Vector2 offset = Vector2.Zero;
            Play(effect, entity, ref offset);
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Gets the effect with the specified name, loads it if it isn't
        /// already known.
        /// </summary>
        /// <param name="effectName">The effect to get.</param>
        /// <returns>
        /// The effect.
        /// </returns>
        private ParticleEffect GetEffect(string effectName)
        {
            // Note: No need to lock the dictionary, because this only gets
            // called from the rendering instance.
            if (!_effects.ContainsKey(effectName))
            {
                _effects.Add(effectName, _content.Load<ParticleEffect>(effectName));
            }
            return _effects[effectName];
        }

        #endregion

        #region Copying

        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (ParticleEffectSystem)base.DeepCopy(into);

            // Mark as secondary system.
            copy._isDrawingInstance = false;

            return copy;
        }

        #endregion
    }
}
