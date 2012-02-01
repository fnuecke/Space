using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Constraints;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Manages spawning dynamic objects for cells, such as random ships.
    /// </summary>
    sealed class ShipsSpawnSystem : AbstractSystem
    {
        #region Fields

        private Dictionary<ulong, List<int>> _entities = new Dictionary<ulong, List<int>>();

        private ContentManager _content;

        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Constructor

        public ShipsSpawnSystem(ContentManager content)
        {
            _content = content;
            ShouldSynchronize = true;
        }

        #endregion

        #region Logic



        public override void HandleMessage<T>(ref T message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)(ValueType)message;
                if (info.State)
                {
                    if (info.X == 0 && info.Y == 0)
                    {


                        const int cellSize = CellSystem.CellSize;
                        var center = new Vector2(cellSize * info.X + (cellSize >> 1), cellSize * info.Y + (cellSize >> 1));
                        var cellInfo = Manager.GetSystem<UniverseSystem>().GetCellInfo(info.Id);
                        var list = new List<int>();
                        _entities.Add(info.Id, list);
                        for (var i = -2; i < 2; i++)
                        {
                            for (var j = -2; j < 2; j++)
                            {
                                var spawnPoint = new Vector2(center.X + i * (float)cellSize / 5, center.Y - j * (float)cellSize / 5);
                                var order = new AiComponent.AiCommand(spawnPoint, cellSize, AiComponent.Order.Guard);
                                //spawnPoint = new Vector2(center.X + i * (float)cellSize / 5+10000, center.Y - j * (float)cellSize / 5+10000);

                                var ship = EntityFactory.CreateAIShip(
                                    ConstraintsLibrary.GetConstraints<ShipConstraints>("Level 1 AI Ship"),
                                    cellInfo.Faction, spawnPoint, Manager.EntityManager, _random, order);

                                list.Add(Manager.EntityManager.AddEntity(ship));
                            }
                        }
                    }
                }
                else
                {
                    var Listcopy = new List<int>(_entities[info.Id]);
                    _entities.Remove(info.Id);
                    foreach (var entry in Listcopy)
                    {
                        Manager.EntityManager.RemoveEntity(entry);
                    }
                }
            }
            else if (message is EntityRemoved)
            {
                var info = (EntityRemoved)(ValueType)message;
                var transform = info.Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    var position = transform.Translation;
                    var cellId = CoordinateIds.Combine(
                       (int)position.X >> CellSystem.CellSizeShiftAmount,
                       (int)position.Y >> CellSystem.CellSizeShiftAmount);

                    if (_entities.ContainsKey(cellId))
                        _entities[cellId].Remove(info.Entity.UID);
                }
            }
            else if (message is EntityChangedCell)
            {
                var info = (EntityChangedCell)(ValueType)message;
                var entityID = info.EntityID;
                _entities[info.OldCellID].Remove(info.EntityID);
                if (Manager.GetSystem<CellSystem>().IsCellActive(info.NewCellID))
                {
                    _entities[info.NewCellID].Add(info.EntityID);
                }
                else
                {
                    Manager.EntityManager.RemoveEntity(entityID);
                }

            }
        }

        public void CreateAttackingShip(ref Vector2 startPosition, int targetEntity, Factions faction)
        {
            var aicommand = new AiComponent.AiCommand(targetEntity, 2000, AiComponent.Order.Move);
            var cellID = CoordinateIds.Combine((int)startPosition.X >> CellSystem.CellSizeShiftAmount,
                   (int)startPosition.Y >> CellSystem.CellSizeShiftAmount);

            var ship = EntityFactory.CreateAIShip(
                ConstraintsLibrary.GetConstraints<ShipConstraints>("Level 1 AI Ship"),
                faction, startPosition, Manager.EntityManager, _random, aicommand);

            _entities[cellID].Add(Manager.EntityManager.AddEntity(ship));
        }
        public void CreateAttackingShip(ref Vector2 startPosition, ref Vector2 targetPosition, Factions faction)
        {
            var aicommand = new AiComponent.AiCommand(targetPosition, 2000, AiComponent.Order.Move);
            var cellID = CoordinateIds.Combine((int)startPosition.X >> CellSystem.CellSizeShiftAmount,
                  (int)startPosition.Y >> CellSystem.CellSizeShiftAmount);

            var ship = EntityFactory.CreateAIShip(
                 ConstraintsLibrary.GetConstraints<ShipConstraints>("Level 1 AI Ship"),
                faction, startPosition, Manager.EntityManager, _random, aicommand);

            _entities[cellID].Add(Manager.EntityManager.AddEntity(ship));
        }
        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
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

            packet.Write(_random);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
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

            _random = packet.ReadPacketizableInto(_random);
        }

        public override void Hash(Hasher hasher)
        {
            foreach (var entities in _entities.Values)
            {
                foreach (var entity in entities)
                {
                    hasher.Put(BitConverter.GetBytes(entity));
                }
            }
        }

        #endregion

        #region Copying

        public override ISystem DeepCopy(ISystem into)
        {
            var copy = (ShipsSpawnSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._content = _content;
                copy._entities.Clear();
                copy._random = _random.DeepCopy(_random);
            }
            else
            {
                copy._entities = new Dictionary<ulong, List<int>>();
                copy._random = _random.DeepCopy();
            }

            foreach (var item in _entities)
            {
                copy._entities.Add(item.Key, new List<int>(item.Value));

            }

            return copy;
        }

        #endregion
    }
}
