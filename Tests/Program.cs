using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.Collections;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("workss");
            
            var list = new List<string>();
            for(var i = 0; i< 100;i++)
            {
                list.Add(""+i);
            }
            var rTree = new RTree<string>();
            Test(rTree,list);
            
            var qTree = new QuadTree<string>(50,128);
            Test(qTree, list);
            Console.ReadKey();
        }

        static void Test(IIndex<string> index,IEnumerable<string> list )
        {
            var random = new MersenneTwister(1);
            var currTime = DateTime.Now;

            foreach (var item in list)
            {
                index.Add(new Vector2(random.NextInt32(1000),random.NextInt32(1000)),item);
            }
            index.Remove("7");
            var returnlist = index.RangeQuery(new Vector2(random.NextInt32(1000), random.NextInt32(1000)),100);
            foreach (var returnedItem in returnlist)
            {
                Console.WriteLine(returnedItem);
            }
            var currTime2 = DateTime.Now;

            Console.WriteLine(" Elapesed Time: "+(currTime2.Ticks-currTime.Ticks));
        }
    }
}
