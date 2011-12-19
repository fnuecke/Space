using System;
using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Parameterizations;
using SpaceData;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single weapon slot on an entity, which can hold a weapon
    /// at a specific offset to the entity's origin, and in a specific angle.
    /// 
    /// <para>
    /// Optional: <c>Transform</c>, <c>Velocity</c>.
    /// </para>
    /// </summary>
    public class WeaponSlot : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// A list of weapons currently active.
        /// </summary>
        public WeaponData Weapon { get; set; }

        /// <summary>
        /// Remaining cooldown time for weapons, in game updates.
        /// </summary>
        public int Cooldown { get; set; }

        /// <summary>
        /// Mounting position relative to the entities center.
        /// </summary>
        public FPoint MountPoint { get; set; }

        /// <summary>
        /// The orientation of the mount point, i.e. the direction we fire in,
        /// as a normalized vector.
        /// </summary>
        public FPoint MountDirection { get; set; }

        #endregion

        #region Logic

        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var p = (WeaponParameterization)parameterization;

            // Reduce cooldowns.
            if (Cooldown > 0)
            {
                --Cooldown;
            }

            // Trigger weapons that are not on cooldown if we're shooting.
            if (Weapon != null && Entity.GetComponent<WeaponControl>().IsShooting)
            {
                if (Cooldown == 0)
                {
                    Cooldown = Weapon.Cooldown;
                    p.Weapon = Weapon;
                    p.Position = MountPoint;
                    p.Velocity = MountDirection * Weapon.ProjectileSpeed;

                    // Try to get initial position and velocity based on components.
                    var transfrom = Entity.GetComponent<Transform>();
                    var velocity = Entity.GetComponent<Velocity>();

                    if (transfrom != null)
                    {
                        p.Position += transfrom.Translation;
                    }
                    if (velocity != null)
                    {
                        p.Velocity += velocity.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Accepts <c>WeaponParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(WeaponParameterization));
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet
                .Write(Weapon == null ? new Packet() : (new Packet().Write(Weapon)))
                .Write(Cooldown)
                .Write(MountPoint)
                .Write(MountDirection);
        }

        public override void Depacketize(Packet packet)
        {
            var weaponData = packet.ReadPacket();
            if (weaponData.Length > 0)
            {
                Weapon = new WeaponData();
                Weapon.Depacketize(weaponData);
            }
            else
            {
                Weapon = null;
            }
            Cooldown = packet.ReadInt32();
            MountPoint = packet.ReadFPoint();
            MountDirection = packet.ReadFPoint();
        }

        public override void Hash(Hasher hasher)
        {
            // TODO make weapondata hashable?
            hasher.Put(BitConverter.GetBytes(Cooldown));
            hasher.Put(BitConverter.GetBytes(MountPoint.X.RawValue));
            hasher.Put(BitConverter.GetBytes(MountPoint.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(MountDirection.X.RawValue));
            hasher.Put(BitConverter.GetBytes(MountDirection.Y.RawValue));
        }

        #endregion
    }
}
