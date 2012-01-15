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
        #region Fields

        private List<int> _entities;

        private ContentManager _content;

        #endregion

        #region Constructor

        public ShipsSpawnSystem(ContentManager content)
        {
            _entities = new List<int>();
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

        #endregion

        #region Cloning

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
        }

        public override void Hash(Hasher hasher)
        {

        }

        public override IComponentSystem DeepCopy(IComponentSystem into)
        {
            var copy = (ShipsSpawnSystem)base.DeepCopy(into);

            return copy;
        }

        protected override void CopyFields(AbstractComponentSystem<NullParameterization, NullParameterization> into)
        {
            base.CopyFields(into);
        }

        #endregion
    }
}
