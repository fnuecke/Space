using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class Command : ICommand
    {

        protected Command(uint type)
        {
            Type = type;
        }

        protected Command(Packet packet)
        {
            Type = packet.ReadUInt32();
        }

        public uint Type { get; private set; }

        // set via protocol
        public bool IsTentative { get; set; }

        // set via protocol
        public int Player { get; set; }

        public virtual void Write(Packet packet)
        {
            packet.Write(Type);
        }

    }
}
