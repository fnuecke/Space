using System;
using Engine.Commands;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for TSS based multiplayer clients using a UDP connection.
    /// This takes care of synchronizing the gamestates between server and
    /// client, and getting the run speed synchronized as well.
    /// </summary>
    /// <typeparam name="TState">the type of game state used to represent a simulation.
    /// This is the simulation run as a sub-state of the TSS.</typeparam>
    /// <typeparam name="TSteppable">the type of object we put into our simulation.</typeparam>
    /// <typeparam name="TCommandType">the type of commands we send around.</typeparam>
    /// <typeparam name="TPlayerData">the tpye of the player data structure.</typeparam>
    /// <typeparam name="TPacketizerContext">the type of the packetizer context.</typeparam>
    public abstract class AbstractTssClient<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        : AbstractTssController<IClientSession<TPlayerData, TPacketizerContext>, TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Constants

        /// <summary>
        /// The interval in milliseconds after which to send a synchronization request to the
        /// server. The lower the value the better the synchronization, but, obviously, also
        /// more network traffic.
        /// </summary>
        private const int SyncInterval = 1000;

        #endregion

        #region Fields

        /// <summary>
        /// Last time we sent a sync command to the server.
        /// </summary>
        private long lastSyncTime = 0;

        /// <summary>
        /// The last frame we know the server's state hash of.
        /// </summary>
        private long hashFrame = -1;

        /// <summary>
        /// The hash value of the server's state.
        /// </summary>
        private int hashValue;

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize session and base classes.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractTssClient(Game game, IClientSession<TPlayerData, TPacketizerContext> session)
            : base(game, session, new uint[] { 50, 100 })
        {
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            if (Session != null)
            {
                Session.GameInfoReceived += HandleGameInfoReceived;
                Session.JoinResponse += HandleJoinResponse;
            }

            if (Simulation != null)
            {
                Simulation.Invalidated += HandleSimulationInvalidated;
            }

            base.Initialize();
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (Session != null)
            {
                Session.GameInfoReceived -= HandleGameInfoReceived;
                Session.JoinResponse -= HandleJoinResponse;

                Session.Dispose();
            }

            if (Simulation != null)
            {
                Simulation.Invalidated -= HandleSimulationInvalidated;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drives the game loop, right after driving the network protocol
        /// in the base class. Also part of synchronizing run speeds on
        /// server and client by sending sync requests in certain intervals.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Session.ConnectionState == ClientState.Connected && !Simulation.WaitingForSynchronization)
            {
                // Hash test.
                // Do all hashing AFTER network handling, but BEFORE logic
                // handling (stepping), to make sure all commands that can
                // possibly contribute to a state already have.
                if (Simulation.TrailingFrame == hashFrame)
                {
                    Hasher hasher = new Hasher();
                    Simulation.TrailingState.Hash(hasher);
                    if (hasher.Value != hashValue)
                    {
                        Console.WriteLine("Client: hash mismatch " + hashValue + "!= " + hasher.Value);
                        Simulation.Invalidate();
                    }
                }

                // Drive game logic.
                UpdateSimulation(gameTime);

                // Send sync command every now and then, to keep game clock synchronized.
                if (new TimeSpan(DateTime.Now.Ticks - lastSyncTime).TotalMilliseconds > SyncInterval)
                {
                    lastSyncTime = DateTime.Now.Ticks;
                    Packet syncRequest = new Packet(5);
                    syncRequest.Write((byte)TssUdpControllerMessage.Synchronize);
                    syncRequest.Write(Simulation.CurrentFrame);
                    Session.SendToHost(syncRequest, PacketPriority.None);
                }
            }
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        protected override void Apply(IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> command, PacketPriority priority)
        {
            base.Apply(command, priority);
            // As a client we only send commands that are our own AND have not been sent
            // back to us by the server, acknowledging our actions. I.e. only send our
            // own, tentative commands.
            if (!command.IsAuthoritative && command.Player.Equals(Session.LocalPlayer))
            {
                SendToEveryone(command, priority);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// We received information about a running game from some host.
        /// </summary>
        /// <param name="sender">the underlying session.</param>
        /// <param name="e">information of the type <c>GameInfoReceivedEventArgs</c>.</param>
        protected abstract void HandleGameInfoReceived(object sender, EventArgs e);

        /// <summary>
        /// A server sent us a response to our request to join his game.
        /// </summary>
        /// <param name="sender">the underlying session.</param>
        /// <param name="e">information of the type <c>JoinResponseEventArgs</c>.</param>
        protected abstract void HandleJoinResponse(object sender, EventArgs e);

        #endregion

        #region Protocol layer

        /// <summary>
        /// Takes care of client side TSS synchronization logic.
        /// </summary>
        protected override bool UnwrapDataForReceive(PlayerDataEventArgs<TPlayerData, TPacketizerContext> args, out IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
            var type = (TssUdpControllerMessage)args.Data.ReadByte();
            command = null;
            switch (type)
            {
                case TssUdpControllerMessage.Command:
                    // Normal command, forward it.
                    return base.UnwrapDataForReceive(args, out command);

                case TssUdpControllerMessage.Synchronize:
                    // Answer to a synchronization request.
                    // Only accept these when they come from the server.
                    if (args.IsFromServer)
                    {
                        // This calculation follows algorithm described here:
                        // http://www.mine-control.com/zack/timesync/timesync.html
                        // Which is actually pretty logical, except for the '+ latency / 2'
                        // in the frameDelta part. But it works, so w/e.
                        // We skip steps 5-7 because they're for TCP retransmits causing
                        // peaks, but we're using UDP, so there.
                        long latency = (Simulation.CurrentFrame - args.Data.ReadInt64()) / 2;
                        long clientServerDelta = (args.Data.ReadInt64() - Simulation.CurrentFrame);
                        long frameDelta = clientServerDelta + latency / 2;
                        if (System.Math.Abs(frameDelta) > 2)
                        {
#if DEBUG
                            Console.WriteLine("Client: correcting for " + frameDelta + " frames.");
#endif
                            Simulation.RunToFrame(Simulation.CurrentFrame + frameDelta);
                        }
                        return true;
                    }
                    break;

                case TssUdpControllerMessage.GameStateResponse:
                    // Got a simulation snap shot (normally after requesting it due to
                    // our simulation going out of scope for an older event).
                    // Only accept these when they come from the server.
                    if (args.IsFromServer)
                    {
                        Simulation.Depacketize(args.Data, Packetizer.Context);
                        return true;
                    }
                    break;

                case TssUdpControllerMessage.AddGameObject:
                    // Only accept these when they come from the server.
                    if (args.IsFromServer)
                    {
                        long addFrame = args.Data.ReadInt64();
                        TSteppable steppable = Packetizer.Depacketize<TSteppable>(args.Data);
                        Simulation.AddSteppable(steppable, addFrame);
                        return true;
                    }
                    break;

                case TssUdpControllerMessage.RemoveGameObject:
                    // Only accept these when they come from the server.
                    if (args.IsFromServer)
                    {
                        long removeFrame = args.Data.ReadInt64();
                        long steppableUid = args.Data.ReadInt64();
                        Simulation.RemoveSteppable(steppableUid, removeFrame);
                        return true;
                    }
                    break;

                case TssUdpControllerMessage.HashCheck:
                    // Only accept these when they come from the server.
                    if (args.IsFromServer)
                    {
                        hashFrame = args.Data.ReadInt64();
                        hashValue = args.Data.ReadInt32();
                        return true;
                    }
                    break;

                // Everything else is unhandled on the client.
                default:
                    break;
            }
            return false;
        }

        #endregion

        #region Events handled internally

        /// <summary>
        /// Called when our simulation cannot accomodate an update or rollback,
        /// meaning we have to get a server snapshot.
        /// </summary>
        private void HandleSimulationInvalidated(object sender, EventArgs e)
        {
            // So we request it.
#if DEBUG
            Console.WriteLine("Client: simulation invalidated, re-sync");
#endif
            Packet gameStateRequest = new Packet(1);
            gameStateRequest.Write((byte)TssUdpControllerMessage.GameStateRequest);
            Session.SendToHost(gameStateRequest, PacketPriority.Medium);
        }

        #endregion
    }
}
