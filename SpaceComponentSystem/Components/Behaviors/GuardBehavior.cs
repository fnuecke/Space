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
            // We need a relatively high poll rate to avoid jittery formation flight.
            : base(ai, random, 0.3f)
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
            // Stop if we're guarding ourself or our target has died.
            if (Target == AI.Entity || !AI.Manager.HasEntity(Target))
            {
                AI.PopBehavior();
                return false;
            }

            // Check for nearby enemies.
            var enemy = GetClosestEnemy(AI.Configuration.AggroRange, OwnerWithHealthFilter);
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
        /// Called when an entity becomes an invalid target (removed from the
        /// system or died). This is intended to allow behaviors to stop in
        /// case their related entity is removed (e.g. target when attacking).
        /// </summary>
        /// <param name="entity">The entity that was removed.</param>
        internal override void OnEntityInvalidated(int entity)
        {
            if (entity == Target)
            {
                // If we're in a squad, guard the leader, otherwise give up.
                var squad = (Squad)AI.Manager.GetComponent(AI.Entity, Squad.TypeId);
                if (squad != null)
                {
                    Target = squad.Leader;
                }
                else
                {
                    Target = 0;
                }
            }
        }

        #endregion

        #region Behavior type specifics

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
                var squad = (Squad)AI.Manager.GetComponent(AI.Entity, Squad.TypeId);
                if (squad != null && squad.Contains(Target))
                {
                    // We're in a squad and protecting a member. Leave everything to the
                    // autopilot (vegetative nervous system) but keep going.
                    target = ((Transform)(AI.Manager.GetComponent(AI.Entity, Transform.TypeId))).Translation;
                    var leaderVelocity = (Velocity)AI.Manager.GetComponent(squad.Leader, Velocity.TypeId);
                    if (leaderVelocity != null)
                    {
                        target += leaderVelocity.Value;
                    }
                }
                else
                {
                    // Not in a squad or targeting something that's not in the squad...
                    target = ((Transform)(AI.Manager.GetComponent(Target, Transform.TypeId))).Translation;
                    var targetVelocity = (Velocity)AI.Manager.GetComponent(Target, Velocity.TypeId);
                    if (targetVelocity != null)
                    {
                        target += targetVelocity.Value;
                    }
                }
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
