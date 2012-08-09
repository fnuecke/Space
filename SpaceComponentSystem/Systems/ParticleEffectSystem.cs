using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury;
using ProjectMercury.Renderers;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Controls the particle components in a game, passing them some
    /// information about how to render themselves.
    /// </summary>
    public abstract class ParticleEffectSystem : AbstractComponentSystem<ParticleEffects>, IDrawingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        public bool IsEnabled { get; set; }

        #endregion

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
        /// Gets the current speed of the simulation.
        /// </summary>
        private readonly Func<float> _speed;

        /// <summary>
        /// The speed the simulation runs at.
        /// </summary>
        private readonly float _simulationFps;

        /// <summary>
        /// The relative speed of simulation to render updates per second.
        /// </summary>
        private readonly float _relativeFps;

        /// <summary>
        /// Cached known particle effects.
        /// </summary>
        private readonly Dictionary<string, ParticleEffect> _effects = new Dictionary<string, ParticleEffect>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticleEffectSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics.</param>
        /// <param name="speed">A function getting the speed of the simulation.</param>
        /// <param name="renderFps">The frames per second we render.</param>
        /// <param name="simulationFps">The frames per second the simulation is updated.</param>
        protected ParticleEffectSystem(ContentManager content, IGraphicsDeviceService graphics, Func<float> speed, float renderFps, float simulationFps)
        {
            _content = content;
            _renderer = new SpriteBatchRenderer {GraphicsDeviceService = graphics};
            _renderer.LoadContent(content);
            _speed = speed;
            _simulationFps = simulationFps;
            _relativeFps = simulationFps / renderFps;

            IsEnabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Flags our system as the presenting instance and renders all effects.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Trigger all known permanent effects.
            foreach (var component in Components)
            {
                foreach (var effect in component.Effects)
                {
                    var offset = effect.Item2;
                    Play(effect.Item1, component.Entity, ref offset);
                }
            }

            // Update all known effects.
            foreach (var effect in _effects.Values)
            {
                effect.Update(elapsedMilliseconds / 1000f);
            }

            // Render all known effects.
            var transform = GetTransform();
            foreach (var effect in _effects.Values)
            {
                _renderer.RenderEffect(effect, ref transform);
            }
        }

        /// <summary>
        /// Returns the <em>transformation</em> for rendered content.
        /// </summary>
        /// <returns>
        /// The translation.
        /// </returns>
        protected virtual FarTransform GetTransform()
        {
            return FarTransform.Identity;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Plays the specified effect.
        /// </summary>
        /// <param name="effectName">The effect.</param>
        /// <param name="position">The position.</param>
        /// <param name="impulse">The initial (additional) impulse of the particle.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="scale">The scale.</param>
        public void Play(string effectName, ref FarPosition position, ref Vector2 impulse, float rotation = 0.0f, float scale = 1.0f)
        {
            var transform = GetTransform();
            Vector2 translation;
            FarPosition.Transform(ref position, ref transform, out translation);
            var bounds = _renderer.GraphicsDeviceService.GraphicsDevice.Viewport.Bounds;
            bounds.Inflate((int)(256 * scale), (int)(256 * scale));
            if (!bounds.Contains((int)translation.X, (int)translation.Y))
            {
                return;
            }

            // Let there be graphics!
            var effect = GetEffect(effectName);
            effect.Trigger(ref position, ref impulse, rotation, scale);
        }

        /// <summary>
        /// Plays an effect with the specified name as if it were emitted by
        /// the specified entity, at an offset to the entity's center.
        /// </summary>
        /// <param name="effect">The name of the effect to play.</param>
        /// <param name="entity">The entity that emits the effect.</param>
        /// <param name="offset">The offset of the effect to the center of the entity.</param>
        /// <param name="scale">The scaling of the effect.</param>
        /// <remarks>
        /// The entity must have a <c>Transform</c> component.
        /// </remarks>
        public void Play(string effect, int entity, ref Vector2 offset, float scale = 1.0f)
        {
            // Get the interpolation system to get an interpolated position for the effect generator.
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            // Get interpolated position and rotation.
            FarPosition position;
            interpolation.GetInterpolatedPosition(entity, out position);
            float rotation;
            interpolation.GetInterpolatedRotation(entity, out rotation);

            // Apply the generator offset and rotate to fit into the particle system logic.
            position += offset;
            rotation += MathHelper.Pi;

            // See if we have a velocity to adjust the impulse.
            var velocity = (Velocity)Manager.GetComponent(entity, Velocity.TypeId);
            var impulse = Vector2.Zero;
            if (velocity != null)
            {
                impulse = velocity.Value;

                var speed = _speed();

                // We need to simulate the first update in advance, otherwise the emitter
                // position appears to "move" depending on object velocity.
                position -= impulse * speed * _relativeFps;
                impulse *= speed * _simulationFps;
            }

            Play(effect, ref position, ref impulse, rotation, scale);
        }

        /// <summary>
        /// Plays an effect with the specified name as if it were emitted by
        /// the specified entity.
        /// </summary>
        /// <param name="effect">The name of the effect to play.</param>
        /// <param name="entity">The entity that emits the effect.</param>
        /// <param name="scale">The scaling for the effect.</param>
        /// <remarks>
        /// The entity must have a <c>Transform</c> component.
        /// </remarks>
        public void Play(string effect, int entity, float scale = 1.0f)
        {
            var offset = Vector2.Zero;
            Play(effect, entity, ref offset, scale);
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

        #region Serialization

        /// <summary>
        /// We're purely visual, so don't hash anything.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        public override void Hash(Hasher hasher)
        {
        }

        #endregion

        #region Copying

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override AbstractSystem NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override void CopyInto(AbstractSystem into)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
