using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    class AttackBehaviour : Behaviour
    {
        public int TargetEntity;

        public Vector2 StartPosition;

        public bool TargetDead;

        public AttackBehaviour() { }

        public AttackBehaviour(AiComponent aiComponent, int targetEntity)
            : base(aiComponent)
        {
            TargetEntity = targetEntity;
            
        }

        #region Logic

        public override void Update()
        {
            var targetEntity = AiComponent.Entity.Manager.GetEntity(TargetEntity);

            if (targetEntity == null)
            {

                return;

            }
            var transform = targetEntity.GetComponent<Transform>();

            var info = AiComponent.Entity.GetComponent<ShipInfo>();
            var input = AiComponent.Entity.GetComponent<ShipControl>();
            input.SetStabilizing(false);
            var position = info.Position;

            direction = transform.Translation - position;
            var distance = direction.Length();
            direction.Normalize();

            input.SetTargetRotation((float)Math.Atan2(direction.Y, direction.X));

            //shoot only when in range...
            input.SetShooting(distance < 1000);

            var escapeDir = CalculateEscapeDirection();
            direction += 3 * escapeDir;

            //Rotate torwards our destination

            //not fullspeed if there is noting to fear about


            if (escapeDir == Vector2.Zero &&  info.RelativeEnergy < 0.2)
            {
                input.SetAcceleration(Vector2.Zero);
            }
            else//accelerate towrads Destiny
            {
                input.SetAcceleration(direction);
            }
            
        }

        #endregion

        #region Packet

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(TargetEntity)
                .Write(StartPosition);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            TargetEntity = packet.ReadInt32();
            StartPosition = packet.ReadVector2();
        }

        #endregion
    }
}
