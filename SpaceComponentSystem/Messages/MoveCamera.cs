using System.Collections.Generic;
using Engine.FarMath;

namespace Space.ComponentSystem.Messages
{
    public struct MoveCamera
    {
        public int Player;

        public bool Return;

        public long ReturnSpeed;

        public List<Positions> Position;

        public struct Positions
        {
            ///<summary>
            /// The Destination to move to can be the same as in the previous Position if Camera shall stand on one point
            /// </summary>
            public FarPosition Destination;
            /// <summary>
            /// The speed How fast the Transition shall take place, in Frames
            /// </summary>
            public long Speed;
            /// <summary>
            /// The zoom when Destination is reached
            /// </summary>
            public float Zoom;
        }
    }
}
