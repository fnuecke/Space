using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.Data;
using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Modules
{
    public class RadarModule : AbstractEntityModule<EntityAttributeType>
    {

        #region Fields

        /// <summary>
        /// The Range this Radar has
        /// </summary>
        public float RadarDistance;


        #endregion


         #region Constructor

        public RadarModule()
        {
            AddAttributeTypeToInvalidate(EntityAttributeType.RadarDistance);
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(RadarDistance);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            RadarDistance = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(RadarDistance));
        }

        #endregion
    }
}
