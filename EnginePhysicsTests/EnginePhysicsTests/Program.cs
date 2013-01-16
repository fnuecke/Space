using Engine.Serialization;

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
            Packetizable.AddValueTypeOverloads(typeof (Engine.Math.PacketExtensions));
#if FARMATH
            Packetizable.AddValueTypeOverloads(typeof (Engine.FarMath.PacketExtensions));
#endif
            Packetizable.AddValueTypeOverloads(typeof (XnaExtensions.PacketExtensions));

            using (var game = new TestRunner())
            {
                game.Run();
            }
        }
    }
#endif
}
