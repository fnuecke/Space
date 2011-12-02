using System;
using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for TSS based multiplayer servers using a UDP connection.
    /// This takes care of synchronizing the gamestates between server and
    /// client, and getting the runspeed synchronized as well.
    /// </summary>
    /// <typeparam name="TState">the type of game state used to represent a simulation.
    /// This is the simulation run as a substate of the TSS.</typeparam>
    /// <typeparam name="TSteppable">the type of object we put into our simulation.</typeparam>
    /// <typeparam name="TCommandType">the type of commands we send around.</typeparam>
    /// <typeparam name="TPlayerData">the tpye of the player data structure.</typeparam>
    /// <typeparam name="TPacketizerContext">the type of the packetizer context.</typeparam>
    public abstract class AbstractTssUdpServer<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        : AbstractTssUdpController<IServerSession<TPlayerData, TPacketizerContext>, TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Constants

        /// <summary>
        /// The interval in milliseconds after which to send a hashcheck to the clients.
        /// </summary>
        private const int HashInterval = 5000;

        #endregion

        #region Fields

        /// <summary>
        /// Counter used to distribute ids.
        /// </summary>
        private long lastUid;

        /// <summary>
        /// Last time we sent a hash check to our clients.
        /// </summary>
        private long lastHashTime;

        #endregion

        #region Constructor

        /// <summary>
        /// Base constructor, creates simulation. You'll need to initialize it
        /// by calling its <c>Initialize()</c> method yourself.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="maxPlayers">the maximum number of players in the game.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        protected AbstractTssUdpServer(Game game, int maxPlayers, ushort port, string header)
            : base(game, port, header, new uint[] { 50 })
        {
            Session = SessionFactory.StartServer<TPlayerData, TPacketizerContext>(game, protocol, maxPlayers);
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            if (Session != null)
            {
                Session.GameInfoRequested += HandleGameInfoRequested;
                Session.JoinRequested += HandleJoinRequested;
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
                Session.GameInfoRequested -= HandleGameInfoRequested;
                Session.JoinRequested -= HandleJoinRequested;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drives the game loop, right after driving the network protocol
        /// in the base class.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Send hash check every now and then, to check for desyncs.
            // Do all hashing AFTER network handling, but BEFORE logic
            // handling (stepping), to make sure all commands that can
            // possibly contribute to a state already have.
            if (new TimeSpan(DateTime.Now.Ticks - lastHashTime).TotalMilliseconds > HashInterval)
            {
                lastHashTime = DateTime.Now.Ticks;

                Hasher hasher = new Hasher();
                simulation.TrailingState.Hash(hasher);

                Packet hashCheck = new Packet(5);
                hashCheck.Write((byte)TssUdpControllerMessage.HashCheck);
                hashCheck.Write(simulation.TrailingFrame);
                hashCheck.Write(hasher.Value);
                Session.SendAll(hashCheck, 0);
            }

            // Drive game logic.
            UpdateSimulation(gameTime);
        }

        #endregion

        #region Events

        /// <summary>
        /// Some remote machine sent a request for open games. Use this callback
        /// to send back some custom data.
        /// </summary>
        /// <param name="sender">the underlying session.</param>
        /// <param name="e">information of the type <c>RequestEventArgs</c>.</param>
        protected abstract void HandleGameInfoRequested(object sender, EventArgs e);

        /// <summary>
        /// A player asked to join our game. He passed the session checks (game full,
        /// data sent was valid). In this callback it is possible to override this,
        /// and forbid the joining, or send him some custom data with the response.
        /// </summary>
        /// <param name="sender">the underlying session.</param>
        /// <param name="e">information of the type <c>JoinRequestEventArgs</c>.</param>
        protected abstract void HandleJoinRequested(object sender, EventArgs e);

        #endregion

        #region Modify simulation

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived (if sent).</param>
        public override void Apply(IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> command, uint pollRate = 0)
        {
            if (command.Frame > simulation.TrailingFrame)
            {
                // All commands we apply are authoritative.
                command.IsAuthoritative = true;
                base.Apply(command);
                // As a server we resend all commands.
                SendAll(command, pollRate);
            }
#if DEBUG
            else
            {
                console.WriteLine("Got a command we couldn't use, " + command.Frame + "<" + simulation.TrailingFrame);
            }
#endif
        }

        /// <summary>
        /// Add a steppable to the simulation. Will be inserted at the
        /// current leading frame. The steppable will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="steppable">the steppable to add.</param>
        /// <param name="frame">the frame in which to add the steppable.</param>
        /// <returns>the id the steppable was assigned.</returns>
        public override long AddSteppable(TSteppable steppable, long frame)
        {
            // Give the steppable a unique id. Skip the zero to avoid
            // referencing that object with uninitialized 'pointers'.
            steppable.UID = ++lastUid;

            // Add the steppable to the simulation.
            base.AddSteppable(steppable, frame);

            // Notify all players in the game about this.
            Packet addedInfo = new Packet();
            addedInfo.Write((byte)TssUdpControllerMessage.AddGameObject);
            addedInfo.Write(frame);
            packetizer.Packetize(steppable, addedInfo);
            Session.SendAll(addedInfo, 100);

            return steppable.UID;
        }

        /// <summary>
        /// Removes a steppable with the given id from the simulation.
        /// The steppable will be removed at the given frame.
        /// </summary>
        /// <param name="steppableId">the id of the steppable to remove.</param>
        /// <param name="frame">the frame in which to remove the steppable.</param>
        public override void RemoveSteppable(long steppableUid, long frame)
        {
            // Remove the steppable from the simulation.
            base.RemoveSteppable(steppableUid, frame);

            // Notify all players in the game about this.
            Packet removedInfo = new Packet(9);
            removedInfo.Write((byte)TssUdpControllerMessage.RemoveGameObject);
            removedInfo.Write(frame);
            removedInfo.Write(steppableUid);
            Session.SendAll(removedInfo, 100);
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Takes care of server side TSS synchronization logic.
        /// </summary>
        protected override bool UnwrapDataForReceive(PlayerDataEventArgs<TPlayerData, TPacketizerContext> args, out IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
            command = null;
            var type = (TssUdpControllerMessage)args.Data.ReadByte();
            switch (type)
            {
                case TssUdpControllerMessage.Command:
                    // Normal command, forward it.
                    return base.UnwrapDataForReceive(args, out command);

                case TssUdpControllerMessage.Synchronize:
                    // Client resyncing.
                    {
                        long clientFrame = args.Data.ReadInt64();
                        Packet synchronizeResponse = new Packet(9);
                        synchronizeResponse.Write((byte)TssUdpControllerMessage.Synchronize);
                        synchronizeResponse.Write(clientFrame);
                        synchronizeResponse.Write(simulation.CurrentFrame);
                        Session.Send(args.Player.Number, synchronizeResponse, 0);
                    }
                    return true;

                case TssUdpControllerMessage.GameStateRequest:
                    // Client needs game state.
                    {
                        Packet gamestateResponse = new Packet();
                        gamestateResponse.Write((byte)TssUdpControllerMessage.GameStateResponse);
                        simulation.Packetize(gamestateResponse);
                        Session.Send(args.Player.Number, gamestateResponse, 500);
                    }
                    return true;

                // Everything else is unhandled on the server.
                default:
                    break;
            }
            return false;
        }

        #endregion
    }
}
