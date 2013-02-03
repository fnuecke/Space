using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using ProjectMercury;
using ProjectMercury.Renderers;
using Space.ComponentSystem.Components;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Controls the particle components in a game, passing them some information about how to render themselves.</summary>
    [Packetizable(false), PresentationOnlyAttribute]
    public abstract class ParticleEffectSystem : AbstractComponentSystem<ParticleEffects>
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        [PublicAPI]
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The renderer used to draw effects.</summary>
        private SpriteBatchRenderer _renderer;

        /// <summary>Gets the current speed of the simulation.</summary>
        private readonly Func<float> _simulationSpeed;

        /// <summary>Cached known particle effects.</summary>
        private readonly Dictionary<string, ParticleEffect> _effects = new Dictionary<string, ParticleEffect>();

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParticleEffectSystem"/> class.
        /// </summary>
        /// <param name="simulationSpeed">A function getting the current simulation framerate.</param>
        protected ParticleEffectSystem(Func<float> simulationSpeed)
        {
            _simulationSpeed = simulationSpeed;
        }

        #endregion

        #region Logic
        
        /// <summary>Flags our system as the presenting instance and renders all effects.</summary>
        [MessageCallback]
        public void OnDraw(Draw message)
        {
            if (!Enabled)
            {
                return;
            }

            // Get global transform.
            var transform = GetTransform();
            var translation = GetTranslation();

            // Get delta to keep update speed constant regardless of framerate.
            var delta = (message.ElapsedMilliseconds / 1000f) * _simulationSpeed();

            // Get the interpolation system to get an interpolated position for the effect generator.
            var interpolation = (InterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);

            // Handle particle effects attached to entities.
            foreach (var component in Components)
            {
                // Skip disabled components.
                if (!component.Enabled)
                {
                    continue;
                }

                // Handle each effect per component.
                foreach (var effect in component.Effects)
                {
                    // Get info for triggering and rendering.
                    FarPosition position;
                    float angle;
                    interpolation.GetInterpolatedTransform(component.Entity, out position, out angle);

                    // Load / initialize particle effects if they aren't yet.
                    if (effect.Effect == null)
                    {
                        var content = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content;
                        effect.Effect = content.Load<ParticleEffect>(effect.AssetName).DeepCopy();
                        effect.Effect.LoadContent(content);
                        effect.Effect.Initialise();
                    }

                    // Only do the triggering work if the effect is actually enabled.
                    // ALWAYS RENDER, to allow already triggered effects to play out (and not
                    // instantly disappear).
                    if (effect.Enabled && effect.Scale * effect.Intensity > 0.1f)
                    {
                        // Check if it's in bounds, i.e. whether we have to trigger it at all.
                        var localTranslation = Vector2.Transform((Vector2) (position + translation), transform);
                        var bounds = _renderer.GraphicsDeviceService.GraphicsDevice.Viewport.Bounds;
                        bounds.Inflate(256, 256);
                        if (bounds.Contains((int) localTranslation.X, (int) localTranslation.Y))
                        {
                            // Move the offset according to rotation.
                            var cosRadians = (float) Math.Cos(angle);
                            var sinRadians = (float) Math.Sin(angle);

                            FarPosition offset;
                            offset.X = effect.Offset.X * cosRadians - effect.Offset.Y * sinRadians;
                            offset.Y = effect.Offset.X * sinRadians + effect.Offset.Y * cosRadians;

                            // Adjust emitting rotation.
                            angle += effect.Direction;

                            // Trigger.
                            effect.Effect.Trigger(offset, angle, effect.Scale * effect.Intensity);
                        }
                    }

                    // Render at owning entity's position.
                    FarTransform localTransform;
                    localTransform.Matrix = transform;
                    localTransform.Translation = FarUnitConversion.ToScreenUnits(translation + position);
                    _renderer.RenderEffect(effect.Effect, ref localTransform);

                    // Update after rendering.
                    effect.Effect.Update(delta);
                }
            }

            // Render and update all known unbound effects (not attached to an entity).
            FarTransform globalTransform;
            globalTransform.Matrix = transform;
            globalTransform.Translation = FarUnitConversion.ToScreenUnits(translation);
            foreach (var effect in _effects.Values)
            {
                _renderer.RenderEffect(effect, ref globalTransform);
                effect.Update(delta);
            }
        }

        /// <summary>
        ///     Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected abstract Matrix GetTransform();

        /// <summary>
        ///     Returns the <em>translation</em> for globally offsetting rendered content.
        /// </summary>
        /// <returns>The translation.</returns>
        protected abstract FarPosition GetTranslation();

        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            var content = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content;
            if (_renderer == null)
            {
                _renderer = new SpriteBatchRenderer {GraphicsDeviceService = message.Graphics};
            }
            _renderer.LoadContent(content);
            foreach (var component in Components)
            {
                foreach (var effect in component.Effects)
                {
                    if (effect.Effect == null)
                    {
                        effect.Effect = content.Load<ParticleEffect>(effect.AssetName).DeepCopy();
                        effect.Effect.LoadContent(content);
                        effect.Effect.Initialise();
                    }
                    else
                    {
                        effect.Effect.LoadContent(content);
                    }
                }
            }
            foreach (var effect in _effects)
            {
                effect.Value.LoadContent(content);
            }
        }

        // TODO do we have to dispose and recreate the renderer, i.e. handle OnGraphicsDeviceDisposing?

        #endregion

        #region Accessors

        /// <summary>Plays the specified effect.</summary>
        /// <param name="effect">The effect.</param>
        /// <param name="position">The position.</param>
        /// <param name="impulse">The initial (additional) impulse of the particle.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="scale">The scale.</param>
        [PublicAPI]
        public void Play(string effect, FarPosition position, Vector2 impulse, float rotation = 0.0f, float scale = 1.0f)
        {
            if (!Enabled)
            {
                return;
            }

            // Check if it's in bounds, i.e. whether we have to render it at all.
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);
            if (!camera.ComputeVisibleBounds().Contains(position))
            {
                return;
            }

            // Let there be graphics!
            position = FarUnitConversion.ToScreenUnits(position);
            lock (this)
            {
                GetEffect(effect).Trigger(ref position, ref impulse, rotation, scale);
            }
        }

        /// <summary>
        ///     Plays an effect with the specified name as if it were emitted by the specified entity, at an offset to the
        ///     entity's center.
        /// </summary>
        /// <param name="effect">The effect to trigger.</param>
        /// <param name="entity">The entity that emits the effect.</param>
        /// <param name="offset">The offset of the effect to the center of the entity.</param>
        /// <param name="scale">The scaling of the effect.</param>
        /// <remarks>
        ///     The entity must have a <c>Transform</c> component.
        /// </remarks>
        [PublicAPI]
        public void Play(string effect, int entity, Vector2 offset, float scale = 1.0f)
        {
            // Get the interpolation system to get an interpolated position for the effect generator.
            var interpolation = (InterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);

            // Get interpolated position and rotation.
            FarPosition position;
            float angle;
            interpolation.GetInterpolatedTransform(entity, out position, out angle);

            // Apply the generator offset.
            position += offset;

            // See if we have a velocity to adjust the impulse.
            var velocity = (Velocity) Manager.GetComponent(entity, Velocity.TypeId);
            var impulse = Vector2.Zero;
            if (velocity != null)
            {
                impulse = velocity.LinearVelocity;

                // Scale the impulse to "per second" speed.
                impulse *= _simulationSpeed() * Settings.TicksPerSecond;
            }

            Play(effect, position, impulse, angle, scale);
        }
        
        /// <summary>Plays an effect with the specified name as if it were emitted by the specified entity.</summary>
        /// <param name="effect">The name of the effect to play.</param>
        /// <param name="entity">The entity that emits the effect.</param>
        /// <param name="scale">The scaling for the effect.</param>
        /// <remarks>
        ///     The entity must have a <c>Transform</c> component.
        /// </remarks>
        [PublicAPI]
        public void Play(string effect, int entity, float scale = 1.0f)
        {
            var offset = Vector2.Zero;
            Play(effect, entity, offset, scale);
        }

        #endregion

        #region Utility methods

        /// <summary>Gets the effect with the specified name, loads it if it isn't already known.</summary>
        /// <param name="effectName">The effect to get.</param>
        /// <returns>The effect.</returns>
        private ParticleEffect GetEffect(string effectName)
        {
            // See if the effect is already known.
            if (!_effects.ContainsKey(effectName))
            {
                // It isn't. Lock the dictionary (might be called from some reactionary
                // systems, e.g. death triggering explosions which run in parallel).
                lock (_effects)
                {
                    // Check again (some other thread might have already added it
                    // while we locked).
                    if (!_effects.ContainsKey(effectName))
                    {
                        // Nope, really don't have it yet, load and init.
                        var content = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content;
                        var effect = content.Load<ParticleEffect>(effectName).DeepCopy();
                        effect.LoadContent(content);
                        effect.Initialise();
                        _effects.Add(effectName, effect);
                    }
                }
            }
            return _effects[effectName];
        }

        #endregion
    }
}