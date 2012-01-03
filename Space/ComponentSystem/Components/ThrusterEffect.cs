using Engine.ComponentSystem.Components;
using Engine.Serialization;
using ProjectMercury.Emitters;

namespace Space.ComponentSystem.Components
{
    public sealed class ThrusterEffect : Effect
    {
        #region Properties

        public float Rotation { get; set; }

        #endregion

        #region Constructor

        public ThrusterEffect(string effectName)
            : base(effectName)
        {
        }

        public ThrusterEffect()
        {
        }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
            if (Enabled && _effect != null)
            {
                var transform = Entity.GetComponent<Transform>();
                var velocity = Entity.GetComponent<Velocity>();
                if (transform != null)
                {
                    Rotation = transform.Rotation + (float)System.Math.PI;
                    foreach (var emitter in _effect)
                    {
                        var cone = emitter as ConeEmitter;
                        if (cone != null)
                        {
                            cone.Direction = Rotation;
                        }
                        if (velocity != null)
                        {
                            emitter.ReleaseImpulse = velocity.Value * 59;
                            emitter.TriggerOffset = -velocity.Value;
                        }
                    }
                }
            }

            base.Update(parameterization);
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Rotation);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Rotation = packet.ReadSingle();
        }

        #endregion
    }
}
