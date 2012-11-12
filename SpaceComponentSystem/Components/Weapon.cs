using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.ComponentSystem.Factories;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single weapon item.
    /// </summary>
    public sealed class Weapon : SpaceItem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The sound this weapon emits when firing.
        /// </summary>
        public string Sound;

        /// <summary>
        /// Attributes that are local to this weapon and only used for computing
        /// this weapon's damage, cooldown, energy consumption etc.
        /// </summary>
        public readonly Dictionary<AttributeType, float> Attributes = new Dictionary<AttributeType, float>();

        /// <summary>
        /// The projectiles this weapon fires.
        /// </summary>
        public ProjectileFactory[] Projectiles;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherWeapon = (Weapon)other;
            Sound = otherWeapon.Sound;
            foreach (var attribute in otherWeapon.Attributes)
            {
                Attributes.Add(attribute.Key, attribute.Value);
            }
            Projectiles = otherWeapon.Projectiles;

            return this;
        }

        /// <summary>
        /// Creates a new weapon with the specified parameters.
        /// </summary>
        /// <param name="sound">The sound to play when the weapon is fired.</param>
        /// <param name="attributes">The attributes for this specific weapon.</param>
        /// <param name="projectiles">The info on projectiles being shot.</param>
        /// <returns></returns>
        public Weapon Initialize(string sound, Dictionary<AttributeType, float> attributes, ProjectileFactory[] projectiles)
        {
            Sound = sound;
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    Attributes.Add(attribute.Key, attribute.Value);
                }
            }
            Projectiles = projectiles;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Sound = null;
            Attributes.Clear();
            Projectiles = null;
        }

        #endregion

        #region Serialization / Hashing / Cloning

        /// <summary>
        /// Packetizes the specified packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns></returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(Sound);

            packet.Write(Attributes.Count);
            foreach (var attribute in Attributes)
            {
                packet.Write((byte)attribute.Key);
                packet.Write(attribute.Value);
            }

            packet.Write(Projectiles);

            return packet;
        }

        /// <summary>
        /// Depacketizes the specified packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Sound = packet.ReadString();
            var numAttributes = packet.ReadInt32();
            for (var i = 0; i < numAttributes; i++)
            {
                var type = (AttributeType)packet.ReadByte();
                var value = packet.ReadSingle();
                Attributes.Add(type, value);
            }
            Projectiles = packet.ReadPacketizables<ProjectileFactory>();
        }

        /// <summary>
        /// Hashes the specified hasher.
        /// </summary>
        /// <param name="hasher">The hasher.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Sound);
            hasher.Put(Attributes.Count);
            hasher.Put(Projectiles);
        }

        #endregion
    }
}
