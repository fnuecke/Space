using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Components.AI.Behaviors
{
    /// <summary>
    /// Makes AI ships roam a specified area.
    /// </summary>
    internal sealed class RoamBehavior : Behavior
    {
        #region Fields
        
        /// <summary>
        /// The region we're roaming in.
        /// </summary>
        public Rectangle Area;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RoamBehavior"/> class.
        /// </summary>
        /// <param name="ai">The ai component this behavior belongs to.</param>
        /// <param name="random">The randomizer to use for decision making.</param>
        public RoamBehavior(ArtificialIntelligence ai, IUniformRandom random)
            : base(ai, random, 0)
        {
        }

        /// <summary>
        /// Reset this behavior so it can be reused later on.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Area = Rectangle.Empty;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Pick a new target in our region and issue a patrol command towards
        /// it.
        /// </summary>
        /// <returns>
        /// The behavior change.
        /// </returns>
        protected override bool UpdateInternal()
        {
            // We got here, so we have to pick a new destination.
            Vector2 target;
            target.X = Random.NextInt32(Area.Left, Area.Right);
            target.Y = Random.NextInt32(Area.Top, Area.Bottom);

            // And move towards it.
            AI.AttackMove(ref target);

            // Nothing to do. Ever.
            return false;
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
                .Write(Area);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Area = packet.ReadRectangle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Area);
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

            var copy = (RoamBehavior)into;

            copy.Area = Area;
        }

        #endregion
    }
}
