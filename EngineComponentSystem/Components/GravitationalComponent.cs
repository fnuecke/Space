using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Systems;
using Engine.Math;
using Engine.Serialization;
using Engine.ComponentSystem.Parameterizations;

namespace Engine.ComponentSystem.Components
{
    public class GravitationalComponent : AbstractComponent
    {

        #region Fields
        public enum GravitationTypes
        {
            Atractor,

            Atractee,

            Both


        }

        public GravitationTypes GravitationType;

        public Fixed Mass;
        #endregion

        #region Logic


        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }
        /// <summary>
        /// Updates an objects position based on this velocity.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            if (GravitationType != GravitationTypes.Atractee)
            {
                var entityTransform = Entity.GetComponent<Transform>();
                if (entityTransform != null)
                {
                    foreach (var neigbour in Entity.Manager.SystemManager.GetSystem<IndexSystem>().GetNeighbors(Entity, 50))
                    {
                        var gravitation = neigbour.GetComponent<GravitationalComponent>();
                        if (gravitation != null)
                        {
                            switch (gravitation.GravitationType)
                            {
                                case GravitationTypes.Atractee:
                                case GravitationTypes.Both:
                                    var velocity = neigbour.GetComponent<Velocity>();
                                    var transform = neigbour.GetComponent<Transform>();
                                    if (velocity != null && transform != null)
                                    {
                                        var phi = System.Math.Atan2((double)transform.Translation.Y - (double)entityTransform.Translation.Y,
                                            (double)transform.Translation.X - (double)entityTransform.Translation.X);

                                        var distance = System.Math.Sqrt(System.Math.Pow((double)transform.Translation.Y - (double)entityTransform.Translation.Y, 2) +
                                                            System.Math.Pow((double)transform.Translation.X - (double)entityTransform.Translation.X, 2));
                                        var velo = FPoint.Create(velocity.Value.X - (Fixed)(System.Math.Cos(phi) * (double)Mass / System.Math.Pow(distance, 2)),
                                            velocity.Value.Y - (Fixed)(System.Math.Sin(phi) * (double)Mass / System.Math.Pow(distance, 2)));
                                        //Console.WriteLine(velo);
                                        velocity.Value = velo;
                                    }
                                    break;
                                default://do nothing
                                    break;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)GravitationType)
                .Write(Mass);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            GravitationType = (GravitationTypes)packet.ReadByte();
            Mass = packet.ReadFixed();
        }

        

        #endregion

    }
}
