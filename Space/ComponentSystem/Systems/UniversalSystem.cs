using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Systems;
using Space.Data;
using Engine.Util;
using Space.ComponentSystem.Entities;
using Engine.Math;
using Engine.ComponentSystem.Entities;

namespace Space.ComponentSystem.Systems
{
    class UniversalSystem : IComponentSystem
    {
        public Dictionary<long, List<int>> zellEntitys; 
        public IComponentSystemManager Manager
        {
            get;
            set;
        }
        public WorldConstaints Constaints;

        public MersenneTwister Twister;

        public System.Collections.ObjectModel.ReadOnlyCollection<Engine.ComponentSystem.Components.IComponent> Components
        {
            get { throw new NotSupportedException(); }
        }

        public UniversalSystem(WorldConstaints constaits)
        {
            Constaints = constaits;
            zellEntitys = new Dictionary<long, List<int>>();
            
        }

        public void Update(ComponentSystemUpdateType updateType, long frame)
        {
            //not used
        }

        public IComponentSystem AddComponent(Engine.ComponentSystem.Components.IComponent component)
        {
            //
            return null;
        }

        public void RemoveComponent(Engine.ComponentSystem.Components.IComponent component)
        {
           //not used
        }

        public object Clone()
        {
            return this;
        }

        public void HandleMessage(int x, int y, bool alive)
        {
            long shiftx = ((long)x)<< 32;
            long result = shiftx | (long)y;

            if (alive)
            {
                Twister = new MersenneTwister();
                List<int> list;

                if (x == 0 && y == 0)
                    list = CreateStartSystem();
                list = CreateSunSystem(x,y,result);

                zellEntitys.Add(result, list);
                
            }

            else
            {
                foreach (int id in zellEntitys[result])
                {
                    Manager.EntityManager.RemoveEntity(id);
                    
                }
                zellEntitys.Remove(result);
            }
        }

        private List<int> CreateSunSystem(int x,int y,long result)
        {
            FPoint center = FPoint.Create(Fixed.Create(x),Fixed.Create(y));
            List<int> list = new List<int>();
            IEntity entity = EntityFactory.CreateStar("Texture/sun", center);
            Manager.EntityManager.AddEntity(entity);
            list.Add(entity.UID);

            return list;
        }

        private List<int> CreateStartSystem()
        {
            List<int> list = new List<int>();
            
            Twister = new MersenneTwister(Constaints.WorldSeed);
            FPoint center = FPoint.Zero;
            center.X += Twister.Next(2000) - 1000;
            center.Y += Twister.Next(2000) - 1000;
            IEntity entity = EntityFactory.CreateStar("Texture/sun", center);

            return list;
        }
        private List<int> CreateAsteroidBelt()
        {
            List<int> list = new List<int>();

            return list;
        }
        private List<int> CreateNebula()
        {
            var list = new List<int>();

            return list;
        }
        private List<int> CreateSpecialSystem()
        {
            var list = new List<int>();

            return list;
        }



        
    }
}
