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

        /// <summary>
        /// The randomizer we use to pick where to go next.
        /// </summary>
        private readonly MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Constructor
        
        public RoamBehavior(ArtificialIntelligence ai)
            : base(ai, 0)
        {
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
            target.X = _random.NextInt32(Area.Left, Area.Right);
            target.Y = _random.NextInt32(Area.Top, Area.Bottom);

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
                .Write(Area)
                .Write(_random);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Area = packet.ReadRectangle();
            packet.ReadPacketizableInto(_random);
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
            _random.CopyInto(copy._random);
        }

        #endregion
    }
}
