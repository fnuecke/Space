using System.Text;
using Engine.Commands;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for UDP driven clients and servers.
    /// </summary>
    public abstract class AbstractUdpController<TSession, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        : AbstractController<TSession, UdpProtocol, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Construction / Destruction

        /// <summary>
        /// Initialize the protocol.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractUdpController(Game game, ushort port, string header)
            : base(game)
        {
            Protocol = new UdpProtocol(port, Encoding.ASCII.GetBytes(header));
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drive the network protocol.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Drive network communication.
            Protocol.Receive();
            Protocol.Flush();

            base.Update(gameTime);
        }

        #endregion
    }
}
