using System.Collections.Generic;
using Engine.Math;
using SpaceData;

namespace Space.ComponentSystem.Parameterizations
{
    public class ArmamentParameterization
    {
        /// <summary>
        /// A list of shots that were fired due to the update call
        /// this parameterization was used in.
        /// </summary>
        public List<ShotInfo> FiredShots { get; set; }

        public class ShotInfo
        {
            /// <summary>
            /// The position at which the shot should be spawned.
            /// </summary>
            public FPoint Position { get; set; }

            /// <summary>
            /// The initial velocity of the shot.
            /// </summary>
            public FPoint Velocity { get; set; }

            /// <summary>
            /// The weapon that fired the shot.
            /// </summary>
            public WeaponData Weapon { get; set; }
        }
    }
}
