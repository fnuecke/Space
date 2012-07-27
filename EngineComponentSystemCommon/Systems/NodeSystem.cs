using System;
using System.Diagnostics;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// This system tracks entities to ensure they are in the correct node. Note
    /// that it is assumed an entity never moves farther than "one node" in a
    /// single step in local coordinates (otherwise transition to neighboring
    /// nodes fails).
    /// </summary>
    public class NodeSystem : AbstractComponentSystem<Node>
    {
        #region Fields

        /// <summary>
        /// The size of a single node, i.e. the extents of a local coordinate system.
        /// </summary>
        public uint NodeSize { get; private set; }

        /// <summary>
        /// Precomputed for convenience.
        /// </summary>
        private float _nodeSizeHalf;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeSystem"/> class.
        /// </summary>
        /// <param name="nodeSize">Size of a single node. Must be a power of two and not zero.</param>
        public NodeSystem(uint nodeSize)
        {
            Debug.Assert(nodeSize > 0);
            NodeSize = BitwiseMagic.GetNextHighestPowerOfTwo(nodeSize - 1);
            _nodeSizeHalf = NodeSize / 2.0f;
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Checks translation changed of entities, and if they move to a different node
        /// (i.e. they leave the coordinate system of the current one) transfer them to
        /// that node.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            base.Receive(ref message);

            if (message is TranslationChanged)
            {
                var translationChanged = (TranslationChanged)(ValueType)message;

                // Check changes for both x and y axis.
                int dx = 0, dy = 0;

                // See if we went to a neighboring node.
                if (translationChanged.CurrentPosition.X < -_nodeSizeHalf)
                {
                    // Moved to node on the left.
                    dx = -1;
                }
                else if (translationChanged.CurrentPosition.X >= _nodeSizeHalf)
                {
                    // Moved to the node on the right.
                    dx = 1;
                }

                if (translationChanged.CurrentPosition.Y < -_nodeSizeHalf)
                {
                    // Moved to node on the top.
                    dy = -1;
                }
                else if (translationChanged.CurrentPosition.Y >= _nodeSizeHalf)
                {
                    // Moved to the node on the bottom.
                    dy = 1;
                }

                // Transfer if something changed.
                if (dx != 0 || dy != 0)
                {
                    // Getting the component is probably more or at least similarly
                    // expensive as the above comparisons, so fetch it only when we
                    // know we actually need it.
                    var node = ((Node)Manager.GetComponent(translationChanged.Entity, Node.TypeId));
                    if (node != null)
                    {
                        TransferToNode(node, dx, dy);
                    }
                }
            }
        }

        /// <summary>
        /// Transfers an entity from it's current node to another one, based
        /// on the specified delta.
        /// </summary>
        /// <param name="node">The node component to adjust.</param>
        /// <param name="dx">The delta on the x axis.</param>
        /// <param name="dy">The delta on the y axis.</param>
        private void TransferToNode(Node node, int dx, int dy)
        {
            NodeChanged message;
            message.Entity = node.Entity;
            message.PreviousNode = node.Value;

            int x, y;
            BitwiseMagic.Unpack(node.Value, out x, out y);
            x += dx;
            y += dy;
            node.Value = BitwiseMagic.Pack(x, y);

            message.CurrentNode = node.Value;
            Manager.SendMessage(ref message);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(NodeSize);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            NodeSize = packet.ReadUInt32();
            _nodeSizeHalf = NodeSize / 2.0f;
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(NodeSize);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", NodeSize=" + NodeSize;
        }

        #endregion
    }
}
