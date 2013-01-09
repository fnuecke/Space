using Engine.Serialization;
using Engine.XnaExtensions;

namespace Engine.Physics.Tests
{
#if WINDOWS || XBOX
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            Packetizable.AddValueTypeOverloads(typeof(PacketExtensions));

            using (var game = new TestRunner())
            {
                game.Run();
            }
        }
    }
#endif
}
