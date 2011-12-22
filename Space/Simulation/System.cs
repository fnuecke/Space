using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Space.Simulation
{
    class System
    {
        public ulong Seed;
        public int Random;
        public System(int random,ulong seed)
        {
            Random = random;
            Seed = seed;
        }
    }
}
