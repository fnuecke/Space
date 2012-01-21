using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.ComponentSystem.Systems.Messages;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    
    class ShipsSpawnSystem : AbstractComponentSystem<NullParameterization, NullParameterization>
    {
        #region Fields

        private List<int> _entities = new List<int>();

        private ContentManager _content;

        #endregion

        #region Constructor

        public ShipsSpawnSystem(ContentManager content)
        {
            _content = content;
            ShouldSynchronize = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update all components in this system.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Update(long frame)
        {
            var cellSystem = Manager.GetSystem<CellSystem>();
            var entitiesCopy = new List<int>(_entities);
            foreach (var entityId in entitiesCopy)
            {
                var entity = Manager.EntityManager.GetEntity(entityId);
                var transform = entity.GetComponent<Transform>();
                if (!cellSystem.IsCellActive(CellSystem.GetCellIdFromCoordinates(ref transform.Translation)))
                {
                    _entities.Remove(entityId);
                    Manager.EntityManager.RemoveEntity(entityId);
                }
            }
        }

        public override void HandleSystemMessage<T>(ref T message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)(ValueType)message;
                if (info.State)
                {
                    const int cellSize = CellSystem.CellSize;
                    var center = new Vector2(cellSize*info.X + (cellSize >> 1), cellSize*info.Y + (cellSize >> 1));
                    var cellInfo = Manager.GetSystem<UniverseSystem>().CellInfo[info.Id];

                    for (var i = -2; i < 2; i++)
                    {
                        for (var j = -2; j < 2; j++)
                        {
                            var spawnPoint = new Vector2(center.X + i * (float)cellSize / 5, center.Y - j * (float)cellSize / 5);
                            var order =
                                new AiComponent.AiCommand(spawnPoint
                                ,
                                cellSize, AiComponent.Order.Guard);
                        
                        _entities.Add(Manager.EntityManager.AddEntity(
                            EntityFactory.CreateAIShip(_content.Load<ShipData[]>("Data/ships")[1], cellInfo.Faction, spawnPoint, order
                            )));
                    }
                    }
                }
            }
            else if (message is EntityRemoved)
            {
                var info = (EntityRemoved)(ValueType)message;
                _entities.Remove(info.EntityUid);
            }
        }

        #endregion

        #region Cloning

        public override Packet Packetize(Packet packet)
        {
            packet.Write(_entities.Count);
            foreach (var item in _entities)
            {
                packet.Write(item);
                

            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            _entities.Clear();
            int numCells = packet.ReadInt32();
            for (int i = 0; i < numCells; i++)
            {
                var key = packet.ReadInt32();
                
                _entities.Add(key);
            }
        }

        public override void Hash(Hasher hasher)
        {
            foreach (var entities in _entities)
            {
                hasher.Put(BitConverter.GetBytes(entities));
                
                
                
            }
        }

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (ShipsSpawnSystem)base.DeepCopy(into);

            if (copy == into)
            {
                
                copy._entities.Clear();
            }
            else
            {
                copy._entities = new List<int>();
            }

            foreach (var item in _entities)
            {
                copy._entities.Add(item);
            }

            return copy;
        }

        #endregion
    }
}
