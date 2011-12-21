using Space.Data;

namespace Space.Simulation
{
    /// <summary>
    /// This class contains static world information, i.e. information that
    /// won't change under any circumstances during a single game.
    /// </summary>
    class StaticWorld
    {
        /// <summary>
        /// Creates a new world based on the given seed.
        /// </summary>
        /// <param name="size">the maximum extents of the world, in solar systems (width and height).</param>
        /// <param name="seed">the world seed to use to populate the world.</param>
        public StaticWorld(byte size, long seed, WorldConstaints constraints)
        {

        }
    }
}
