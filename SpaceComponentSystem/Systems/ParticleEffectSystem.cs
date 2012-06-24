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
            var transform = GetTransform();
           
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
        /// Returns the <em>translation</em> for offsetting rendered content.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        protected virtual Vector2 GetTranslation()
        {
            return Vector2.Zero;
        }

        protected virtual Matrix GetTransform()
        {
            return Matrix.Identity;
        }
        #endregion

        #region Accessors

        /// <summary>
        /// Plays the specified effect.
        /// </summary>
        /// <param name="effectName">The effect.</param>
        /// <param name="position">The position.</param>
        /// <param name="scale">The scale.</param>
        public void Play(string effectName, ref Vector2 position, float scale = 1f)
        {
            // Do not play sounds if this isn't the main sound system thats
            // used for the presentation.
            if (!_isDrawingInstance)
            {
                return;
            }

            var translation = position + GetTranslation();
            var bounds = _renderer.GraphicsDeviceService.GraphicsDevice.Viewport.Bounds;
            bounds.Inflate((int)(256 * scale), (int)(256 * scale));
            if (!bounds.Contains((int)translation.X, (int)translation.Y))
            {
                return;
            }

            // Let there be graphics!
            var effect = GetEffect(effectName);
            effect.Trigger(position);
        }

        /// <summary>
        /// Plays an effect with the specified name as if it were emitted by
        /// the specified entity, at an offset to the entity's center.
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

            Play(effect, ref position, rotation);
        }

        /// <summary>
        /// Plays an effect with the specified name as if it were emitted by
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
                var effect = _content.Load<ParticleEffect>(effectName);
                effect.LoadContent(_content);
                effect.Initialise();
                _effects.Add(effectName, effect);
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
