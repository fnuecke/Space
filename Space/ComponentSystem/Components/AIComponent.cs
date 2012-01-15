using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Components.AIBehaviour;

namespace Space.ComponentSystem.Components
{
    
    class AIComponent:AbstractComponent
    {
        private bool starting = false;
        public int counter = 0;
        private Behaviour currentbehaviour;
        public override void Update(object parameterization)
        {

            //currentbehaviour.
            //var input = Entity.GetComponent<ShipControl>();
            //counter++;
            //counter %= 400;
            //if(counter == 0)
            
            //{
            
            //    input.Accelerate(Directions.South);
            //}
            //else if (counter == 100)
            //{
            //    input.StopAccelerate(Directions.South);
            //    input.Accelerate(Directions.West);
            //}
            //else if (counter == 200)
            //{
            //    input.StopAccelerate(Directions.West);
            //    input.Accelerate(Directions.North);
            //}
            //else if (counter == 300)
            //{
            //    input.StopAccelerate(Directions.North);
            //    input.Accelerate(Directions.East);
            //}

            
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>.
        /// </summary>
        /// <param name="parameterizationType">The parameterization to check.</param>
        /// <returns>Whether its supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }
        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                ;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            
        }

        #endregion
    }
}
