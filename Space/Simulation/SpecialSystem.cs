using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Data;
using Engine.Util;

namespace Space.Simulation
{

    class SpecialSystem : System
    {
        MersenneTwister Twister;
        public SpecialSystem(int rand,ulong seed, WorldConstaints constraints)
            : base(rand, seed)
        {
            Twister = new MersenneTwister(seed);
            int random = Twister.Next(10);
            if (random == 0)
            {
                Console.WriteLine("Creating Black Hole");
            }
            if (random < 3)
            {
                Console.WriteLine("Creating Worm Hole");
            }
            else if (random < 13)
            {
                Console.WriteLine("Creating Neutron Star");
            }
            else if (random <= 20)
            {
                Console.WriteLine("Creating White Dwarf");
            }
        }
    }
}
