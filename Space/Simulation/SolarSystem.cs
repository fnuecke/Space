using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Data;
using Engine.Util;

namespace Space.Simulation
{
    class SolarSystem : System
    {
        MersenneTwister Twister;
        public SolarSystem(int rand,ulong seed, WorldConstaints constraints)
            : base(rand, seed)
        {
            
            Twister = new MersenneTwister(seed);
            int random = Twister.Next(20);
            if (random < 5)
            {
                Console.WriteLine("Creating Solar System High Dens");
            }
            else if (random < 15)
            {
                Console.WriteLine("Creating Solar System Medium Dens");
            }
            else
            {
                Console.WriteLine("Creating Solar System Low Dens");
            }
        }
    }
}
