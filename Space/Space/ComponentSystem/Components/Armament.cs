using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Space.ComponentSystem.Parameterizations;
using SpaceData;

namespace Space.ComponentSystem.Components
{
    public class Armament : AbstractComponent
    {
        /// <summary>
        /// Whether ima currently firin mah lazer or not.
        /// </summary>
        public bool IsShooting { get; set; }

        public List<WeaponData> Weapons { get; private set; }

        public Armament()
        {
            this.Weapons = new List<WeaponData>();
        }

        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check in debug mode, as this should not happen anyway.
            if (!SupportsParameterization(parameterization))
            {
                throw new System.ArgumentException("parameterization");
            }
#endif
            var p = (InputParameterization)parameterization;

            if (IsShooting)
            {
                // TODO check cooldowns of weapons, fire those that aren't on cooldown, add info on generated shot to list in parameterization.
            }
        }

        public override bool SupportsParameterization(object parameterization)
        {
            return parameterization is ArmamentParameterization;
        }

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
    }
}
