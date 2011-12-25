using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Math;
using Engine.Util;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    public class UniversalSystem : AbstractComponentSystem<NullParameterization>
    {
        #region Properties
        
        public ulong WorldSeed { get; set; }

        #endregion

        #region Fields
        
        private WorldConstaints _constaints;

        private Dictionary<ulong, List<int>> _entities = new Dictionary<ulong, List<int>>();

        #endregion

        #region Constructor
        
        public UniversalSystem(WorldConstaints constaits)
        {
            _constaints = constaits;
        }

        #endregion

        #region Logic

        public override void HandleMessage(ValueType message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)message;

                if (info.State)
                {
                    var random = new MersenneTwister();
                    List<int> list;

                    if (info.X == 0 && info.Y == 0)
                    {
                        list = CreateStartSystem();
                    }
                    else
                    {
                        list = CreateSunSystem(info.X, info.Y, new MersenneTwister(info.Id ^ WorldSeed));
                    }

                    _entities.Add(info.Id, list);
                }
                else
                {
                    foreach (int id in _entities[info.Id])
                    {
                        Manager.EntityManager.RemoveEntity(id);
                    }

                    _entities.Remove(info.Id);
                }
            }
        }

        #endregion

        #region Cloning

        public override object Clone()
        {
            var copy = (UniversalSystem)base.Clone();

            copy._entities = new Dictionary<ulong, List<int>>();
            foreach (var item in _entities)
            {
                copy._entities.Add(item.Key, new List<int>(item.Value));
            }

            return copy;
        }

        #endregion

        #region Utility methods

        private List<int> CreateSunSystem(int x, int y, MersenneTwister random)
        {
            List<int> list = new List<int>();

            var cellSize = Manager.GetSystem<CellSystem>().CellSize;

            FPoint center = FPoint.Create(Fixed.Create(cellSize * x), Fixed.Create(cellSize * y));

            center.X += random.Next(2000) - 1000;
            center.Y += random.Next(2000) - 1000;

            IEntity entity = EntityFactory.CreateStar("Textures/sun", center);
            Manager.EntityManager.AddEntity(entity);
            list.Add(entity.UID);
            for (int i = 0; i < random.Next(0, 12); i++)
            {
                Console.WriteLine("Create Sun: " + i);
                //U = sqrt(4pi²a³/G(M+M2))
                entity = EntityFactory.CreateStar("Textures/sun", center, random.Next(i * 100, i * 130), random.Next(i * 100, i * 130), random.Next(200, 500) * i, random.Next(0, 355));
                Manager.EntityManager.AddEntity(entity);
                list.Add(entity.UID);
                for (int j = 0; j < random.Next(0, 3); j++)
                {
                    Console.WriteLine("Create moon: " + j);
                    entity = EntityFactory.CreateStar("Textures/sun", entity, random.Next(j * 10, j * 30), random.Next(i * 10, i * 13), random.Next(200, 500) * j, random.Next(0, 355));
                    Manager.EntityManager.AddEntity(entity);

                    list.Add(entity.UID);
                }
            }

            return list;
        }

        private List<int> CreateStartSystem()
        {
            var random = new MersenneTwister(WorldSeed);

            List<int> list = new List<int>();

            FPoint center = FPoint.Zero;
            center.X += random.Next(2000) - 1000;
            center.Y += random.Next(2000) - 1000;

            IEntity entity = EntityFactory.CreateStar("Textures/sun", center);
            Manager.EntityManager.AddEntity(entity);
            list.Add(entity.UID);
            for (int i = 0; i < random.Next(4, 12);i++ )
            {
                Console.WriteLine("Create Sun: " + i);
                //U = sqrt(4pi²a³/G(M+M2))
                entity = EntityFactory.CreateStar("Textures/sun", center, random.Next(i * 100, i * 130), random.Next(i * 100, i * 130),random.Next(200,500)*i, random.Next(0,355));
                Manager.EntityManager.AddEntity(entity);
                list.Add(entity.UID);
                for (int j = 0; j < random.Next(0, 3); j++)
                {
                    Console.WriteLine("Create moon: "+j);
                    entity = EntityFactory.CreateStar("Textures/sun", entity, random.Next(j * 10, j * 30), random.Next(i * 10, i * 13), random.Next(200, 500) * j, random.Next(0, 355));
                    Manager.EntityManager.AddEntity(entity);

                    list.Add(entity.UID);
                }
            }
            
            

            
           
            return list;
        }

        private List<int> CreateAsteroidBelt()
        {
            List<int> list = new List<int>();

            return list;
        }

        private List<int> CreateNebula()
        {
            List<int> list = new List<int>();

            return list;
        }

        private List<int> CreateSpecialSystem()
        {
            List<int> list = new List<int>();

            return list;
        }

        #endregion
    }
}
