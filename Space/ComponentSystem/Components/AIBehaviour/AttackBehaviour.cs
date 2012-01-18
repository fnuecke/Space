﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AIBehaviour
{
    class AttackBehaviour : Behaviour
    {
        public int TargetEntity;
        public AttackBehaviour(AIComponent aiComponent,int targetEntity)
            :base(aiComponent)
        {
            TargetEntity = targetEntity;
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

            direction = position - transform.Translation;
            

            
            input.SetTargetRotation((float)Math.Atan2(direction.Y, direction.X));
            input.SetShooting(true);
            var escapeDir = CalculateEscapeDirection();
            direction += 2 * escapeDir;

            //Rotate torwards our destination
            
            //not fullspeed if there is noting to fear about
            
            
            input.SetAcceleration(direction);
            
        }
    }
}
