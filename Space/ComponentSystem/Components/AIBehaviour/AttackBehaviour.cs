using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    class AttackBehaviour : Behaviour
    {
        public int TargetEntity;
        public Vector2 StartPosition;
        public bool TargetDead;
        public AttackBehaviour(){}
        public AttackBehaviour(AiComponent aiComponent,int targetEntity)
            :base(aiComponent)
        {
            TargetEntity = targetEntity;
            aiComponent.Entity.Manager.Removed += HandleEntityRemoved;
        }


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
            

            var position = info.Position;

            direction =  transform.Translation - position ;
            var distance = direction.Length();
            direction.Normalize();
            
            input.SetTargetRotation((float)Math.Atan2(direction.Y, direction.X));
            
            //shoot only when in range...
            input.SetShooting(distance<1000);
            
            var escapeDir = CalculateEscapeDirection();
            direction += 2 * escapeDir;

            //Rotate torwards our destination
            
            //not fullspeed if there is noting to fear about

            

            if (escapeDir == Vector2.Zero &&  info.Energy < info.MaxEnergy * 0.2)
            {
                input.SetAcceleration(Vector2.Zero);
            }
            else//accelerate towrads Destiny
            {
                input.SetAcceleration(direction);
            }
        }

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(StartPosition);

        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            StartPosition = packet.ReadVector2();
        }

         private void HandleEntityRemoved(object sender, EntityEventArgs e)
         {
             if (e.EntityUid == TargetEntity)
                 TargetDead = true;
         }
    }
}
