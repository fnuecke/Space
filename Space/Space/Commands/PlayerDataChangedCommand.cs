using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Space.Model;

namespace Space.Commands
{
    /// <summary>
    /// Used by server to tell players about a new player spawn.
    /// </summary>
    class PlayerDataChangedCommand : Command<GameCommandType, PlayerInfo, PacketizerContext>
    {
        /// <summary>
        /// The value that changed.
        /// </summary>
        public PlayerInfoField Field { get; private set; }

        /// <summary>
        /// The new, changed value.
        /// </summary>
        public Packet Value { get; private set; }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public PlayerDataChangedCommand()
            : base(GameCommandType.PlayerDataChanged)
        {
        }

        public PlayerDataChangedCommand(Player<PlayerInfo, PacketizerContext> player, PlayerInfoField field, Packet value)
            : base(GameCommandType.PlayerDataChanged, player)
        {
            this.Field = field;
            this.Value = value;
        }

        public override void Packetize(Packet packet)
        {
            packet.Write((byte)Field);
            packet.Write(Value);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, PacketizerContext context)
        {
            Field = (PlayerInfoField)packet.ReadByte();
            Value = packet.ReadPacket();

            base.Depacketize(packet, context);
        }
    }
}
