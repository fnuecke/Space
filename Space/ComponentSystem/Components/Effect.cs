using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
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
        #region Properties

        /// <summary>
        /// The asset name of the particle effect to trigger.
        /// </summary>
        public string EffectName { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The actual instance of the effect we're using.
        /// </summary>
        protected ParticleEffect _effect;

        #endregion

        #region Constructor

        public Effect(string effectName)
        {
            this.EffectName = effectName;
        }

        public Effect()
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
#if DEBUG
            base.Update(parameterization);
#endif
            var args = (ParticleParameterization)parameterization;

            // What kind of update are we running?
            if (args.UpdateType == ComponentSystemUpdateType.Logic)
            {
                // Logic, we need a transform to do the positioning.
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    // If we have an effect make sure its loaded and trigger it.
                    if (_effect == null && !string.IsNullOrWhiteSpace(EffectName))
                    {
                        // Always create a deep copy, because this will always
                        // return the same instance.
                        _effect = args.Content.Load<ParticleEffect>(EffectName).DeepCopy();
                        _effect.Initialise();
                        _effect.LoadContent(args.Content);
                    }
                    if (_effect != null)
                    {
                        // Only trigger new particles while we're enabled.
                        if (Enabled)
                        {
                            _effect.Trigger(transform.Translation);
                        }

                        // Always update, to allow existing particles to disappear.
                        _effect.Update(1f / 60f);
                    }
                }
            }
            else if (args.UpdateType == ComponentSystemUpdateType.Display)
            {
                // Render if we have our effect.
                if (_effect != null)
                {
                    args.Renderer.RenderEffect(_effect, ref args.Matrix);
                }
            }
        }

        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(ParticleParameterization);
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(EffectName);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            EffectName = packet.ReadString();
        }

        public override object Clone()
        {
            var copy = (Effect)base.Clone();

            if (_effect != null)
            {
                copy._effect = _effect.DeepCopy();
            }

            return copy;
        }

        #endregion
    }
}
