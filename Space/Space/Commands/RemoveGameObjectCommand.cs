using Engine.Commands;
using Engine.Serialization;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by server to tell players about a player despawn.
    /// </summary>
    class RemoveGameObjectCommand : Command<GameCommandType, PlayerInfo, PacketizerContext>
    {
        /// <summary>
        /// The frame in which the object was added.
        /// </summary>
        public long Frame { get; private set; }

        /// <summary>
        /// The ID of the object to remove.
        /// </summary>
        public long GameObjectUID { get; private set; }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public RemoveGameObjectCommand()
            : base(GameCommandType.RemoveGameObject)
        {
        }

        /// <summary>
        /// Construct new player removal command, removing a ship of the given player
        /// at the given time.
        /// </summary>
        /// <param name="objectId">the id of the object to remove.</param>
        /// <param name="frame">the frame in which to spawn the ship.</param>
        public RemoveGameObjectCommand(long objectId, long frame)
            : base(GameCommandType.RemoveGameObject)
        {
            this.Frame = frame;
            this.GameObjectUID = objectId;
        }

        public override void Packetize(Packet packet)
        {
            packet.Write(Frame);
            packet.Write(GameObjectUID);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, PacketizerContext context)
        {
            Frame = packet.ReadInt64();
            GameObjectUID = packet.ReadInt64();

            base.Depacketize(packet, context);
        }
    }
}
