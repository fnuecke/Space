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
    /// </summary>
    public sealed class Particle : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The asset name of the particle effect to trigger.
        /// </summary>
        public string EffectName { get; set; }

        #endregion

        #region Fields

        private ParticleEffect _effect;

        #endregion

        #region Constructor

        public Particle(string effectName)
        {
            this.EffectName = effectName;
        }

        public Particle()
        {
        }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var args = (ParticleParameterization)parameterization;

            if (args.UpdateType == ComponentSystemUpdateType.Logic)
            {
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    // If we have an effect make sure its loaded and trigger it.
                    if (_effect == null && !string.IsNullOrWhiteSpace(EffectName))
                    {
                        Console.WriteLine("load effect");
                        _effect = args.Content.Load<ParticleEffect>(EffectName).DeepCopy();
                        _effect.Initialise();
                        _effect.LoadContent(args.Content);
                    }
                    if (_effect != null)
                    {
                        _effect.Trigger(transform.Translation);

                        _effect.Update(1f / 60f);
                    }
                }
            }
            else if (args.UpdateType == ComponentSystemUpdateType.Display)
            {
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
            var copy = (Particle)base.Clone();

            if (_effect != null)
            {
                copy._effect = _effect.DeepCopy();
            }

            return copy;
        }

        #endregion
    }
}
