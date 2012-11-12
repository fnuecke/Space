using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Random;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Base class for systems implementing damage status effect application or other 'on hit' effects.
    /// </summary>
    public abstract class AbstractDamageApplyingSystem : AbstractSystem, IMessagingSystem
    {
        #region Fields

        /// <summary>
        /// Randomizer used for determining whether certain effects should be applied
        /// (e.g. dots, blocking, ...).
        /// </summary>
        protected MersenneTwister Random = new MersenneTwister(0);

        #endregion

        #region Logic

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            var cm = message as DamageReceived?;
            if (cm == null)
            {
                return;
            }

            var m = cm.Value;
            ApplyDamage(m.Owner, m.Attributes, m.Damagee);
        }

        /// <summary>
        /// Applies the damage for this system.
        /// </summary>
        /// <param name="owner">The entity that caused the damage.</param>
        /// <param name="attributes">The attributes of the entity doing the damage.</param>
        /// <param name="damagee">The entity being damage.</param>
        protected abstract void ApplyDamage(int owner, Attributes<AttributeType> attributes, int damagee);

        #endregion

        #region Copying

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>
        /// The copy.
        /// </returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (AbstractDamageApplyingSystem)base.NewInstance();

            copy.Random = new MersenneTwister(0);

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (AbstractDamageApplyingSystem)into;

            Random.CopyInto(copy.Random);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Engine.Serialization.Packet Packetize(Engine.Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(Random);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            packet.ReadPacketizableInto(ref Random);
        }

        #endregion
    }
}
