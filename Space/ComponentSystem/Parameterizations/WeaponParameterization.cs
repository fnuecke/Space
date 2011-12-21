using Engine.Math;
using Space.Data;

namespace Space.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Parameters for <c>WeaponSlot</c>s.
    /// </summary>
    public class WeaponParameterization
    {
        /// <summary>
        /// The weapon that fired the shot, or null if no shot was fired.
        /// </summary>
        public WeaponData Weapon { get; set; }

        /// <summary>
        /// The position at which the shot should be spawned.
        /// </summary>
        public FPoint Position { get; set; }

        /// <summary>
        /// The initial velocity of the shot.
        /// </summary>
        public FPoint Velocity { get; set; }

        /// <summary>
        /// The direction the shot was fired in.
        /// </summary>
        public FPoint Direction { get; set; }
    }
}
