using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
