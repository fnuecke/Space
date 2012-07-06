using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AI.Behaviors
{
    /// <summary>
    /// Makes an AI move to a specified location without letting itself be
    /// interrupted.
    /// </summary>
    internal class MoveBehavior : Behavior
    {
        #region Constants

        /// <summary>
        /// Consider our target reached when we're in an epsilon range with
        /// this radius of the target position.
        /// </summary>
        private const float ReachedEpsilon = 100;

        #endregion

        #region Fields

        /// <summary>
        /// The position to move to.
        /// </summary>
        public Vector2 Target;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai.</param>
        public MoveBehavior(ArtificialIntelligence ai)
            : base(ai, 180)
        {
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
            var position = AI.Manager.GetComponent<Transform>(AI.Entity).Translation;
            if ((Target - position).LengthSquared() < ReachedEpsilon * ReachedEpsilon)
            {
                // We have reached our target, pop self.
                AI.PopBehavior();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Figure out where we want to go.
        /// </summary>
        /// <returns>
        /// The coordinate we want to fly to.
        /// </returns>
        protected override Vector2 GetTargetPosition()
        {
            return Target;
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

            Target = packet.ReadVector2();
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public override Behavior CopyInto(Behavior into)
        {
            var copy = (MoveBehavior)base.CopyInto(into);

            copy.Target = Target;

            return copy;
        }

        #endregion
    }
}
