using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using ProjectMercury;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single particle effect, attached to an entity.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public class Effect : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The asset name of the particle effect to trigger.
        /// </summary>
        public string EffectName;

        /// <summary>
        /// Whether we're currently allowed to emit particles or not.
        /// </summary>
        public bool Emitting;

        /// <summary>
        /// The actual instance of the effect we're using. As a "pointer", so
        /// we can set it from any related copy of this effect.
        /// </summary>
        protected ParticleEffect[] _effect = new ParticleEffect[1];

        /// <summary>
        /// Checks if we're the instance that's used to draw. If so, we will
        /// do updates, otherwise we won't.
        /// </summary>
        protected bool _isDrawingInstance;

        protected float Scale = 0;
        /// <summary>
        /// The latest known frame in our simulation. Don't do updates before
        /// this one, to avoid stuttering (TSS rollbacks causing double updates
        /// or the like). As with the effect, as a pointer because only the
        /// leading (drawing) instance should change this, but all instances
        /// that are copies of the same need to know the leading value.
        /// </summary>
        private long[] _lastKnownFrame = new long[1];

        /// <summary>
        /// Used keep multiple threads (TSS) from writing the last known frame
        /// at the same time.
        /// </summary>
        private object _frameLock = new object();

        #endregion

        #region Constructor

        public Effect(string effectName)
        {
            this.EffectName = effectName;
            Emitting = true;
            

        }

        public Effect()
            : this(string.Empty)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates our particle effect, but won't spawn new particles while
        /// this component is disabled.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
            var args = (DefaultLogicParameterization)parameterization;

            // Logic, we need a transform to do the positioning.
            if (_effect[0] != null && _isDrawingInstance)
            {
                lock (_frameLock)
                {
                    if (args.Frame > _lastKnownFrame[0])
                    {
                        _lastKnownFrame[0] = args.Frame;
                    }
                    else
                    {
                        return;
                    }
                }

                // Only trigger new particles while we're enabled.
                if (Emitting)
                {
                    var transform = Entity.GetComponent<Transform>();
                    if (transform != null)
                    {
                        _effect[0].Trigger(transform.Translation);

                    }
                }

                // Always update, to allow existing particles to disappear.
                _effect[0].Update(1f / 60f);
            }
        }

        public override void Draw(object parameterization)
        {
            var args = (ParticleParameterization)parameterization;

            // If we have an effect make sure its loaded and trigger it.
            if (_effect[0] == null && !string.IsNullOrWhiteSpace(EffectName))
            {
                // Always create a deep copy, because this will always
                // return the same instance.
                _effect[0] = args.Game.Content.Load<ParticleEffect>(EffectName).DeepCopy();
                _effect[0].Initialise();
                _effect[0].LoadContent(args.Game.Content);
               
            }

            // Render if we have our effect.
            if (_effect[0] != null)
            {
                if (Scale != 0)
                {
                    foreach (var effect in _effect[0])
                    {
                        effect.ReleaseScale += Scale;
                    }
                }
                // Only render effects whose emitter is near or inside the
                // visible bounds (performance), where possible.
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    
                    
                    var extendedView = args.SpriteBatch.GraphicsDevice.ScissorRectangle;
                    extendedView.Inflate(1024, 1024);
                    Point point;
                    point.X = (int)(transform.Translation.X + args.Transform.Translation.X);
                    point.Y = (int)(transform.Translation.Y + args.Transform.Translation.Y);
                    if (extendedView.Contains(point))
                    {
                        args.Renderer.RenderEffect(_effect[0], ref args.Transform);
                    }
                }
                else
                {
                    args.Renderer.RenderEffect(_effect[0], ref args.Transform);
                }
            }

            // We're the instance on which draw is called.
            _isDrawingInstance = true;
        }

        /// <summary>
        /// Accepts <c>ParticleParameterization</c> and <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        /// <summary>
        /// Accepts <c>ParticleParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsDrawParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(ParticleParameterization);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(EffectName);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            EffectName = packet.ReadString();
            _effect[0] = null;
            _isDrawingInstance = false;
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Effect)base.DeepCopy(into);

            if (copy == into)
            {
                copy.EffectName = EffectName;
                copy.Emitting = Emitting;
                copy._effect = _effect;
                copy._lastKnownFrame = _lastKnownFrame;
                copy._frameLock = _frameLock;
            }
            copy._isDrawingInstance = false;

            return copy;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", EffectName = " + EffectName + ", Emitting = " + Emitting.ToString();
        }

        #endregion
    }
}
