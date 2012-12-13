using Engine.ComponentSystem.Common.Components;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;

namespace Space.ComponentSystem.Components.Behaviors
{
    /// <summary>
    /// Makes an AI guard a specific entity by continuously attack-moving
    /// to the entity's position.
    /// </summary>
    internal sealed class GuardBehavior : Behavior
    {
        #region Fields

        /// <summary>
        /// The entity to guard.
        /// </summary>
        public int Target;

        #endregion
        
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public GuardBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 2)
        {
        }

        /// <summary>
        /// Reset this behavior so it can be reused later on.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Target = 0;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Check if we reached our target.
        /// </summary>
        /// <returns>
        /// Whether to do the rest of the update.
        /// </returns>
        protected override bool UpdateInternal()
        {
            // Stop if our target has died.
            if (!AI.Manager.HasEntity(Target))
            {
                AI.PopBehavior();
                return false;
            }

            // Check for nearby enemies.
            var enemy = GetClosestEnemy(DefaultAggroRange, HealthFilter);
            if (enemy != 0)
            {
                // It's an enemy. Attack it.
                AI.Attack(enemy);
                return false;
            }

            // Nothing is distracting us, keep going as usual.
            return true;
        }

        /// <summary>
        /// Figure out where we want to go.
        /// </summary>
        /// <returns>
        /// The coordinate we want to fly to.
        /// </returns>
        protected override FarPosition GetTargetPosition()
        {
            FarPosition target;
            if (Target != 0)
            {
                target = ((Transform)(AI.Manager.GetComponent(Target, Transform.TypeId))).Translation;
                target += ((Velocity)(AI.Manager.GetComponent(Target, Velocity.TypeId))).Value;
            }
            else
            {
                target = ((Transform)(AI.Manager.GetComponent(AI.Entity, Transform.TypeId))).Translation;
            }
            return target;
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
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Target);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Target = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Target);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public override void CopyInto(Behavior into)
        {
            base.CopyInto(into);

            var copy = (GuardBehavior)into;

            copy.Target = Target;
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
            return base.ToString() + ", Target=" + Target;
        }

        #endregion
    }
}
