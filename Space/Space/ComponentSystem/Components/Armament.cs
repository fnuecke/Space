using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Parameterizations;
using SpaceData;

namespace Space.ComponentSystem.Components
{
    public class Armament : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// Whether ima currently firin mah lazer or not.
        /// </summary>
        public bool IsShooting { get; set; }

        /// <summary>
        /// A list of weapons currently active.
        /// </summary>
        public List<WeaponData> Weapons { get; private set; }

        #endregion

        #region Constructor

        public Armament()
        {
            this.Weapons = new List<WeaponData>();
        }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check (see implementation) in debug mode,
            // as it should not happen that this is of an invalid type anyway.
            base.Update(parameterization);
#endif
            var p = (InputParameterization)parameterization;

            if (IsShooting)
            {
                // TODO check cooldowns of weapons, fire those that aren't on cooldown, add info on generated shot to list in parameterization.
            }
        }

        /// <summary>
        /// Accepts <c>ArmamentParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(ArmamentParameterization));
        }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Engine.Serialization.Packet packet)
        {
            packet.Write(IsShooting);
            packet.Write(Weapons.Count);
            foreach (var weapon in Weapons)
            {
                weapon.Packetize(packet);
            }
        }

        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            IsShooting = packet.ReadBoolean();
            int numWeapons = packet.ReadInt32();
            for (int i = 0; i < numWeapons; ++i)
            {
                var data = new WeaponData();
                data.Depacketize(packet);
                Weapons.Add(data);
            }
        }

        public override void Hash(Engine.Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(IsShooting));
            hasher.Put(BitConverter.GetBytes(Weapons.Count)); // TODO make data hashable, hash all weapons
        }

        #endregion
    }
}
