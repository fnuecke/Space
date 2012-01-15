using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Entities;
using Space.ComponentSystem.Systems.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    class ShipsSpawnSystem : AbstractComponentSystem<NullParameterization, NullParameterization>
    {
        #region Properties
        
        /// <summary>
        /// The component system manager this system is part of.
        /// </summary>
        public override IComponentSystemManager Manager
        {
            get
            {
                return base.Manager;
            }
            set
            {
                if (Manager != null)
                {
                    Manager.EntityManager.Removed -= HandleEntityRemoved;
                }

                base.Manager = value;

                if (Manager != null)
                {
                    Manager.EntityManager.Removed += HandleEntityRemoved;
                }
            }
        }

        #endregion

        #region Fields

        private HashSet<int> _entities = new HashSet<int>();

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
            var activeCells = cellSystem.ActiveCells;
            foreach (var i in _entities)
            {
                var entity = Manager.EntityManager.GetEntity(i);
                var transform = entity.GetComponent<Transform>();
                if (!cellSystem.IsCellActive(CellSystem.GetCellIdFromCoordinates(ref transform.Translation)))
                {
                    _entities.Remove(i);
                    Manager.EntityManager.RemoveEntity(i);
                }
            }
        }

        public override void HandleMessage(ValueType message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)message;
                if (info.State && info.X == 0 && info.Y == 0)
                {
                    _entities.Add(Manager.EntityManager.AddEntity(EntityFactory.CreateAIShip(_content.Load<ShipData[]>("Data/ships")[0], Factions.Player5)));
                }
            }
        }

        private void HandleEntityRemoved(object sender, EntityEventArgs e)
        {
            _entities.Remove(e.EntityUid);
        }

        #endregion

        #region Cloning

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            packet.Write(_entities.Count);
            foreach (var entityUid in _entities)
            {
                packet.Write(entityUid);
            }

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _entities.Clear();
            int numEntities = packet.ReadInt32();
            for (int i = 0; i < numEntities; ++i)
            {
                _entities.Add(packet.ReadInt32());
            }
        }

        public override void Hash(Hasher hasher)
        {
            foreach (var entityUid in _entities)
            {
                hasher.Put(BitConverter.GetBytes(entityUid));
            }
        }

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (ShipsSpawnSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._entities.Clear();
                copy._entities.UnionWith(_entities);
            }
            else
            {
                copy._entities = new HashSet<int>(_entities);
            }

            return copy;
        }

        #endregion
    }
}
