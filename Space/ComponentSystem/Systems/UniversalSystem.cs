using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    public class UniversalSystem : AbstractComponentSystem<NullParameterization, NullParameterization>
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #region Properties

        public ulong WorldSeed { get; set; }

        #endregion

        #region Fields

        private WorldConstraints _constaints;

        private Dictionary<ulong, List<int>> _entities = new Dictionary<ulong, List<int>>();

        #endregion

        #region Constructor

        public UniversalSystem(WorldConstraints constaits)
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

            var center = new Vector2(cellSize * x + (cellSize >> 1), cellSize * y + (cellSize >> 1));

            IEntity sun = EntityFactory.CreateAstronomicBody("Textures/sun", center, AstronomicBodyType.Sun, 10000);
            list.Add(Manager.EntityManager.AddEntity(sun));
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            for (int i = 1; i < random.Next(1, 8); i++)
            {
                var planet = EntityFactory.CreateAstronomicBody("Textures/planet_rock", sun,
                   (float)(i * i * _constaints.PlanetOrbitMean / 2 + random.NextDouble() * _constaints.PlanetRadiusStdDev * 2 - _constaints.PlanetRadiusStdDev),
                   (float)(i * i * _constaints.PlanetOrbitMean / 2 + random.NextDouble() * _constaints.PlanetRadiusStdDev * 2 - _constaints.PlanetRadiusStdDev),
                    angle, (int)Math.Sqrt(Math.Pow(i * i * (double)_constaints.PlanetOrbitMean / 2, 3)), AstronomicBodyType.Planet, 1000);
                var renderer = planet.GetComponent<PlanetRenderer>();
                renderer.Tint = Color.DarkOliveGreen;
                renderer.AtmosphereTint = Color.LightSkyBlue;
                renderer.Scale = 0.5f;
                var spin = planet.GetComponent<Spin>();
                spin.Value = (float)random.NextDouble() * 0.003f - 0.0015f;
                list.Add(Manager.EntityManager.AddEntity(planet));

                for (int j = 1; j < random.Next(1, 4); j++)
                {
                    var moon = EntityFactory.CreateAstronomicBody("Textures/planet_rock", planet,
                   (float)(j * j * _constaints.MoonOrbitMean / 2 + random.NextDouble() * _constaints.MoonOrbitStdDevFraction * 2 - _constaints.MoonOrbitStdDevFraction),
                   (float)(j * j * _constaints.MoonOrbitMean / 2 + random.NextDouble() * _constaints.MoonOrbitStdDevFraction * 2 - _constaints.MoonOrbitStdDevFraction),
                    angle, (int)Math.Sqrt(Math.Pow(j * j * (double)_constaints.MoonOrbitMean / 2, 3)), AstronomicBodyType.Moon, 100);
                    renderer = moon.GetComponent<PlanetRenderer>();
                    renderer.Scale = 0.25f;
                    renderer.AtmosphereTint = Color.Black;
                    spin = moon.GetComponent<Spin>();
                    spin.Value = (float)random.NextDouble() * 0.002f - 0.001f;
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

            var center = new Vector2((cellSize >> 1), (cellSize >> 1));
            Console.WriteLine(center);
            IEntity entity = EntityFactory.CreateAstronomicBody("Textures/sun", center, AstronomicBodyType.Sun, 10000);
            list.Add(Manager.EntityManager.AddEntity(entity));

            entity = EntityFactory.CreateAstronomicBody("Textures/planet_rock", entity, 5000, 4000, 1, 3560, AstronomicBodyType.Planet, 1000);
            var renderer = entity.GetComponent<PlanetRenderer>();
            renderer.Tint = Color.DarkOliveGreen;
            renderer.AtmosphereTint = Color.LightSkyBlue;
            renderer.Scale = 0.5f;
            var spin = entity.GetComponent<Spin>();
            spin.Value = (float)random.NextDouble() * 0.003f - 0.0015f;
            list.Add(Manager.EntityManager.AddEntity(entity));


            entity = EntityFactory.CreateAstronomicBody("Textures/planet_rock", entity, 200, 180, 100, 300, AstronomicBodyType.Moon, 100);
            renderer = entity.GetComponent<PlanetRenderer>();
            renderer.Scale = 0.25f;
            renderer.AtmosphereTint = Color.Black;
            spin = entity.GetComponent<Spin>();
            spin.Value = (float)random.NextDouble() * 0.002f - 0.001f;
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
