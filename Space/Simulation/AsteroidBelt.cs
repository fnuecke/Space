using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Data;
using Engine.Util;

namespace Space.Simulation
{
    class AsteroidBelt:System
    {
        MersenneTwister Twister;
        public AsteroidBelt(int rand,ulong seed, WorldConstaints constraints)
            :base(rand,seed)
        {
             Twister = new MersenneTwister(seed);
             int random = Twister.Next(20);
             if (random < 5)
             {
                 Console.WriteLine("Creating Asteroid Belt High Dens");
             }
             else if (random < 15)
             {
                 Console.WriteLine("Creating Asteroid Belt Medium Dens");
             }
             else
             {
                 Console.WriteLine("Creating Asteroid Belt Low Dens");
             }
        }
    }
}
