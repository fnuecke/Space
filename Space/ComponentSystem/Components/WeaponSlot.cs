using System;
using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Components.Messages;
using Space.ComponentSystem.Parameterizations;
using Space.Data;

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
        public WeaponData Weapon
        {
            get
            {
                return _weapon;
            }
            set
            {
                if (_sound > 0)
                {
                    Entity.RemoveComponent(_sound);
                    _sound = -1;
                }
                _weapon = value;
                if (_weapon != null)
                {
                    var sound = new Sound();
                    sound.SoundCue = _weapon.Sound;
                    sound.TriggeringMessages.Add(typeof(WeaponFired));
                    Entity.AddComponent(sound);
                    _sound = sound.UID;
                }
            }
        }

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

        #region Fields

        /// <summary>
        /// The id of the sound component we use to play our weapon sound.
        /// </summary>
        private int _sound;

        /// <summary>
        /// The actual weapon data.
        /// </summary>
        private WeaponData _weapon;

        #endregion

        #region Constructor

        public WeaponSlot()
        {
            MountDirection = FPoint.Create((Fixed)1, (Fixed)0);
            _sound = -1;
        }

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
                        p.Direction = FPoint.Rotate(FPoint.Create((Fixed)1, (Fixed)0), transfrom.Rotation);
                        p.Velocity = FPoint.Rotate(p.Velocity, transfrom.Rotation);
                    }
                    if (velocity != null)
                    {
                        p.Velocity += velocity.Value;
                    }

                    Entity.SendMessage(WeaponFired.Create());
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
                .Write(Cooldown)
                .Write(MountPoint)
                .Write(MountDirection)
                .Write(_sound)
                .Write(_weapon);
        }

        public override void Depacketize(Packet packet)
        {
            Cooldown = packet.ReadInt32();
            MountPoint = packet.ReadFPoint();
            MountDirection = packet.ReadFPoint();
            _sound = packet.ReadInt32();
            _weapon = packet.ReadPacketizable(new WeaponData());
        }

        public override void Hash(Hasher hasher)
        {
            // TODO make weapondata hashable?
            hasher.Put(BitConverter.GetBytes(Cooldown));
            hasher.Put(BitConverter.GetBytes(MountPoint.X.RawValue));
            hasher.Put(BitConverter.GetBytes(MountPoint.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(MountDirection.X.RawValue));
            hasher.Put(BitConverter.GetBytes(MountDirection.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(_sound));
        }

        #endregion
    }
}
