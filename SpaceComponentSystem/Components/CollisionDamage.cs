using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// This component can be used to apply damage to another entity upon
    /// collision with the entity this component belongs to.
    /// 
    /// <para>
    /// Used for making bullets damage ships, and causing ships to damage each
    /// other. In the latter case, the damage is applied with a certain
    /// frequency (controlled via the <c>Cooldown</c>).
    /// </para>
    /// </summary>
    public sealed class CollisionDamage : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

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
        /// Determines how many frames (updates) to wait between dealing our
        /// damage. This is per other entity we collide with.
        /// 
        /// <para>
        /// A special case is zero, which means we only do our damage once,
        /// then die.
        /// </para>
        /// </summary>
        public int Cooldown;

        /// <summary>
        /// The amount of damage to deal upon collision.
        /// </summary>
        public float Damage;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherCollisionDamage = (CollisionDamage)other;
            Cooldown = otherCollisionDamage.Cooldown;
            Damage = otherCollisionDamage.Damage;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="cooldown">The cooldown.</param>
        /// <param name="damage">The damage.</param>
        public CollisionDamage Initialize(int cooldown, float damage)
        {
            Cooldown = cooldown;
            Damage = damage;

            return this;
        }

        /// <summary>
        /// Initialize with the specified damage.
        /// </summary>
        /// <param name="damage">The damage.</param>
        public CollisionDamage Initialize(float damage)
        {
            return Initialize(0, damage);
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Cooldown = 0;
            Damage = 0;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(Cooldown);
            packet.Write(Damage);

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Cooldown = packet.ReadInt32();
            Damage = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Cooldown);
            hasher.Put(Damage);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Cooldown=" + Cooldown + ", Damage=" + Damage.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
