using System.Text;
using Engine.Network;
using Engine.Session;

namespace Space.Game
{
    class Server
    {

        private IProtocol protocol;
        private IServerSession session;

        public Server(int maxPlayers)
        {
            protocol = new UdpProtocol(8442, Encoding.ASCII.GetBytes("5p4c3"));
            session = SessionFactory.StartServer(protocol, maxPlayers);

            
        }

    }
}
