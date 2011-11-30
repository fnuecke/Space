using Engine.Commands;
using Engine.Serialization;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by server to tell players about a new player spawn.
    /// </summary>
    class AddGameObjectCommand : Command<GameCommandType, PlayerInfo, PacketizerContext>
    {
        /// <summary>
        /// The frame in which the object was added.
        /// </summary>
        public long Frame { get; private set; }

        /// <summary>
        /// The serialized form of the object to add.
        /// </summary>
        public Packet GameObject { get; private set; }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public AddGameObjectCommand()
            : base(GameCommandType.AddGameObject)
        {
        }

        /// <summary>
        /// Construct new player add command, spawning a ship for the given player
        /// at the given time.
        /// </summary>
        /// <param name="gameObject">the serialized object to add to the simulation.</param>
        /// <param name="frame">the frame in which to spawn the ship.</param>
        public AddGameObjectCommand(Packet gameObject, long frame)
            : base(GameCommandType.AddGameObject)
        {
            this.Frame = frame;
            this.GameObject = gameObject;
        }

        public override void Packetize(Packet packet)
        {
            packet.Write(Frame);
            packet.Write(GameObject);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, Model.PacketizerContext context)
        {
            Frame = packet.ReadInt64();
            GameObject = packet.ReadPacket();

            base.Depacketize(packet, context);
        }
    }
}
