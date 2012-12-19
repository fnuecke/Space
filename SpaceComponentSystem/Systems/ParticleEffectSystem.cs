using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Space.ComponentSystem.Components;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Controls the particle components in a game, passing them some
    /// information about how to render themselves.
    /// </summary>
    public abstract class ParticleEffectSystem : AbstractComponentSystem<ParticleEffects>, IDrawingSystem, IMessagingSystem
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
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The renderer used to draw effects.
        /// </summary>
        private SpriteBatchRenderer _renderer;

        /// <summary>
        /// Gets the current speed of the simulation.
        /// </summary>
        private readonly Func<float> _simulationFps;

        /// <summary>
        /// Cached known particle effects.
        /// </summary>
        private readonly Dictionary<string, ParticleEffect> _effects = new Dictionary<string, ParticleEffect>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticleEffectSystem"/> class.
        /// </summary>
        /// <param name="simulationFps">A function getting the current simulation framerate.</param>
        protected ParticleEffectSystem(Func<float> simulationFps)
        {
            _simulationFps = simulationFps;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    if (_renderer == null)
                    {
                        _renderer = new SpriteBatchRenderer {GraphicsDeviceService = cm.Value.Graphics};
                    }
                    _renderer.LoadContent(cm.Value.Content);
                    foreach (var component in Components)
                    {
                        foreach (var effect in component.Effects)
                        {
                            if (effect.Effect == null)
                            {
                                var graphicsSystem = ((GraphicsDeviceSystem)Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                                effect.Effect = graphicsSystem.Content.Load<ParticleEffect>(effect.AssetName).DeepCopy();
                                effect.Effect.LoadContent(cm.Value.Content);
                                effect.Effect.Initialise();
                            }
                            else
                            {
                                effect.Effect.LoadContent(cm.Value.Content);
                            }
                        }
                    }
                    foreach (var effect in _effects)
                    {
                        effect.Value.LoadContent(cm.Value.Content);
                    }
                }
            }
            // TODO do we have to dispose and recreate the renderer?
        }

        /// <summary>
        /// Flags our system as the presenting instance and renders all effects.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Get global transform.
            var transform = GetTransform();

            // Get delta to keep update speed constant regardless of framerate.
            var delta = elapsedMilliseconds / (1000 / (_simulationFps() / Settings.TicksPerSecond));

            // Get the interpolation system to get an interpolated position for the effect generator.
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            // Handle particle effects attached to entities.
            foreach (var component in Components)
            {
                // Handle each effect per component.
                foreach (var effect in component.Effects)
                {
                    // Get info for triggering and rendering.
                    FarPosition position;
                    interpolation.GetInterpolatedPosition(component.Entity, out position);

                    // Load / initialize particle effects if they aren't yet.
                    if (effect.Effect == null)
                    {
                        var graphicsSystem = ((GraphicsDeviceSystem)Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                        effect.Effect = graphicsSystem.Content.Load<ParticleEffect>(effect.AssetName).DeepCopy();
                        effect.Effect.LoadContent(graphicsSystem.Content);
                        effect.Effect.Initialise();
                    }

                    // Only do the triggering work if the effect is actually enabled.
                    // ALWAYS RENDER, to allow already triggered effects to play out (and not
                    // instantly disappear).
                    if (effect.Enabled && effect.Scale * effect.Intensity > 0.1f)
                    {
                        // Check if it's in bounds, i.e. whether we have to trigger it at all.
                        FarPosition translation;
                        FarPosition.Transform(ref position, ref transform, out translation);
                        var bounds = _renderer.GraphicsDeviceService.GraphicsDevice.Viewport.Bounds;
                        bounds.Inflate(256, 256);
                        if (bounds.Contains((int)translation.X, (int)translation.Y))
                        {
                            // Get rotation of the object.
                            float rotation;
                            interpolation.GetInterpolatedRotation(component.Entity, out rotation);

                            // Move the offset according to rotation.
                            var cosRadians = (float)Math.Cos(rotation);
                            var sinRadians = (float)Math.Sin(rotation);

                            FarPosition offset;
                            offset.X = effect.Offset.X * cosRadians - effect.Offset.Y * sinRadians;
                            offset.Y = effect.Offset.X * sinRadians + effect.Offset.Y * cosRadians;

                            // Adjust emitting rotation.
                            rotation += effect.Direction;

                            // Trigger.
                            effect.Effect.Trigger(offset, rotation, effect.Scale * effect.Intensity);
                        }
                    }

                    // Render at owning entity's position.
                    var localTransform = transform;
                    localTransform.Translation += position;
                    _renderer.RenderEffect(effect.Effect, ref localTransform);

                    // Update after rendering.
                    effect.Effect.Update(delta);
                }
            }

            // Render and update all known unbound effects (not attached to an entity).
            foreach (var effect in _effects.Values)
            {
                _renderer.RenderEffect(effect, ref transform);
                effect.Update(delta);
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
        /// <param name="effect">The effect.</param>
        /// <param name="position">The position.</param>
        /// <param name="impulse">The initial (additional) impulse of the particle.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="scale">The scale.</param>
        public void Play(ParticleEffect effect, ref FarPosition position, ref Vector2 impulse, float rotation = 0.0f, float scale = 1.0f)
        {
            // Get position of the effect relative to view port.
            var transform = GetTransform();
            FarPosition translation;
            FarPosition.Transform(ref position, ref transform, out translation);

            // Check if it's in bounds, i.e. whether we have to render it at all.
            var bounds = _renderer.GraphicsDeviceService.GraphicsDevice.Viewport.Bounds;
            bounds.Inflate((int)(256 * scale), (int)(256 * scale));
            if (!bounds.Contains((int)translation.X, (int)translation.Y))
            {
                return;
            }

            // Let there be graphics!
            lock (this)
            {
                effect.Trigger(ref position, ref impulse, rotation, scale);
            }
        }

        /// <summary>
        /// Plays an effect with the specified name as if it were emitted by
        /// the specified entity, at an offset to the entity's center.
        /// </summary>
        /// <param name="effect">The effect to trigger.</param>
        /// <param name="entity">The entity that emits the effect.</param>
        /// <param name="offset">The offset of the effect to the center of the entity.</param>
        /// <param name="scale">The scaling of the effect.</param>
        /// <remarks>
        /// The entity must have a <c>Transform</c> component.
        /// </remarks>
        public void Play(ParticleEffect effect, int entity, ref Vector2 offset, float scale = 1.0f)
        {
            // Get the interpolation system to get an interpolated position for the effect generator.
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            // Get interpolated position and rotation.
            FarPosition position;
            interpolation.GetInterpolatedPosition(entity, out position);
            float rotation;
            interpolation.GetInterpolatedRotation(entity, out rotation);

            // Apply the generator offset.
            position += offset;

            // See if we have a velocity to adjust the impulse.
            var velocity = (Velocity)Manager.GetComponent(entity, Velocity.TypeId);
            var impulse = Vector2.Zero;
            if (velocity != null)
            {
                impulse = velocity.Value;

                // Scale the impulse to "per second" speed.
                impulse *= _simulationFps();
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
            Play(GetEffect(effect), entity, ref offset, scale);
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
            // See if the effect is already known.
            if (!_effects.ContainsKey(effectName))
            {
                // It isn't. Lock the dictionary (might be called from some reactionary
                // systems, e.g. death triggering expliosions which run in parallel).
                lock (_effects)
                {
                    // Check again (some other thread might have already added it
                    // while we locked).
                    if (!_effects.ContainsKey(effectName))
                    {
                        // Nope, really don't have it yet, load and init.
                        var graphicsSystem = ((GraphicsDeviceSystem)Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                        var effect = graphicsSystem.Content.Load<ParticleEffect>(effectName).DeepCopy();
                        effect.LoadContent(graphicsSystem.Content);
                        effect.Initialise();
                        _effects.Add(effectName, effect);
                    }
                }
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
    }
}
