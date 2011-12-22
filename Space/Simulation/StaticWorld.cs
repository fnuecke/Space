using Space.Data;
using Engine.Util;
using System;
using System.Collections.Generic;

namespace Space.Simulation
{
    /// <summary>
    /// This class contains static world information, i.e. information that
    /// won't change under any circumstances during a single game.
    /// </summary>
    class StaticWorld
    {

        List<System> Systems;
        WorldConstaints Constraints;
        byte Size;
        /// <summary>
        /// Creates a new world based on the given seed.
        /// </summary>
        /// <param name="size">the maximum extents of the world, in solar systems (width and height).</param>
        /// <param name="seed">the world seed to use to populate the world.</param>
        public StaticWorld(byte size, ulong seed, WorldConstaints constraints)
        {
            Systems= new List<System>();
            Size = size;
            Constraints = constraints;
            MersenneTwister twister = new MersenneTwister(seed);
            for (byte i = 0; i < size; i++)
            {
                for (byte j = 0; j < size; j++)
                {
                    int random = twister.Next(1000);
            
                    Systems.Add(new System(random,++seed));
                }
            }

            Console.WriteLine("");
        }
        /// <summary>
        /// Creates a System on the Given Position
        /// System information is taken from the list created on startup
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public System CreateSystem(byte x, byte y)
        {
            System system = Systems[Size * y + x];
            if (system.Random < 500)
                return new SolarSystem(system.Random,system.Seed, Constraints);
            else if (system.Random < 800)
            {
                return new AsteroidBelt(system.Random, system.Seed, Constraints);
            }
            else
            {
                return new SpecialSystem(system.Random, system.Seed, Constraints);
            }
        }
    }
}
