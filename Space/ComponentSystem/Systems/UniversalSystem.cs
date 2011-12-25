using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    public class UniversalSystem : AbstractComponentSystem<NullParameterization>
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
            ShouldSynchronize = true;
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
                    if (info.X == 0 && info.Y == 0)
                    {
                        _entities.Add(info.Id, CreateStartSystem());
                    }
                    else
                    {
                        _entities.Add(info.Id, CreateSunSystem(info.X, info.Y, new MersenneTwister(info.Id ^ WorldSeed)));
                    }
                }
                else
                {
                    if (_entities.ContainsKey(info.Id))
                    {
                        foreach (int id in _entities[info.Id])
                        {
                            Manager.EntityManager.RemoveEntity(id);
                        }

                        _entities.Remove(info.Id);
                    }
                }
            }
        }

        #endregion

        #region Cloning

        public override Packet Packetize(Packet packet)
        {
            packet.Write(WorldSeed);

            packet.Write(_entities.Count);
            foreach (var item in _entities)
            {
                packet.Write(item.Key);
                packet.Write(item.Value.Count);
                foreach (var entityId in item.Value)
                {
                    packet.Write(entityId);
                }
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            WorldSeed = packet.ReadUInt64();

            _entities.Clear();
            int numCells = packet.ReadInt32();
            for (int i = 0; i < numCells; i++)
            {
                var key = packet.ReadUInt64();
                var list = new List<int>();
                int numEntities = packet.ReadInt32();
                for (int j = 0; j < numEntities; j++)
                {
                    list.Add(packet.ReadInt32());
                }
                _entities.Add(key, list);
            }
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(WorldSeed));
            foreach (var entities in _entities.Values)
            {
                foreach (var entity in entities)
                {
                    hasher.Put(BitConverter.GetBytes(entity));
                }
            }
        }

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

            FPoint center = FPoint.Create(Fixed.Create(cellSize * x) + (cellSize >> 1), Fixed.Create(cellSize * y) + (cellSize >> 1));

            IEntity sun = EntityFactory.CreateStar("Textures/sun", center,AstronomicBodyType.Sun);
            list.Add(Manager.EntityManager.AddEntity(sun));

            for (int i = 1; i < random.Next(1, 12); i++)
            {
                var planet = EntityFactory.CreateStar("Textures/sun", sun,
                    (Fixed)random.Next(i * 100, i * 130), (Fixed)random.Next(i * 100, i * 130),
                    (Fixed)random.Next(200, 500) * i, random.Next(200, 355),AstronomicBodyType.Planet);
                list.Add(Manager.EntityManager.AddEntity(planet));

                for (int j = 1; j < random.Next(1, 4); j++)
                {
                    var moon = EntityFactory.CreateStar("Textures/sun", planet,
                        (Fixed)random.Next(j * 10, j * 30), (Fixed)random.Next(j * 10, j * 13),
                        (Fixed)random.Next(200, 500) * j, random.Next(200, 355),AstronomicBodyType.Moon);
                    list.Add(Manager.EntityManager.AddEntity(moon));
                }
            }

            return list;
        }

        private List<int> CreateStartSystem()
        {
            var random = new MersenneTwister(WorldSeed);

            List<int> list = new List<int>();

            var cellSize = Manager.GetSystem<CellSystem>().CellSize;

            FPoint center = FPoint.Create((Fixed)(cellSize >> 1), (Fixed)(cellSize >> 1));
            Console.WriteLine(center);
            IEntity entity = EntityFactory.CreateStar("Textures/sun", center,AstronomicBodyType.Sun);
            list.Add(Manager.EntityManager.AddEntity(entity));

            entity = EntityFactory.CreateStar("Textures/sun", entity, (Fixed)500, (Fixed)200, (Fixed)1, 240,AstronomicBodyType.Planet);
            list.Add(Manager.EntityManager.AddEntity(entity));

            
            entity = EntityFactory.CreateStar("Textures/sun", entity, (Fixed)200, (Fixed)100, (Fixed)100, 60,AstronomicBodyType.Moon);
            list.Add(Manager.EntityManager.AddEntity(entity));

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
        public List<int> GetSystemList(ulong id)
        {
            return _entities[id];
        }
        #endregion
    }
}
