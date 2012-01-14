using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private List<int> _entitys;
        private ContentManager _content;
        #endregion

        #region Constructor

        public ShipsSpawnSystem(ContentManager content)
        {
            _entitys = new List<int>();
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
            var cellsystem = Manager.GetSystem<CellSystem>();
            var activeCells = cellsystem.ActiveSystems;
            foreach (var i in _entitys)
            {
                var entity = Manager.EntityManager.GetEntity(i);
                var transform = entity.GetComponent<Transform>();
                if(!activeCells.Contains(cellsystem.GetCellFromCoordinates(transform.Translation)))
                {
                    _entitys.Remove(i);
                    Manager.EntityManager.RemoveEntity(i);
                }
            }
        }

        public override void HandleMessage(ValueType message)
        {
            if (!(message is CellStateChanged)) return;
            var info = (CellStateChanged) message;

            if (!info.State) return;
            if (info.X == 0 && info.Y == 0)
            {
                _entitys.Add(Manager.EntityManager.AddEntity(EntityFactory.CreateAIShip(_content.Load<ShipData>("Data/ships"),Factions.Player5)));
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
