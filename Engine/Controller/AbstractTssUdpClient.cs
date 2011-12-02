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
    /// Base class for TSS based multiplayer clients using a UDP connection.
    /// This takes care of synchronizing the gamestates between server and
    /// client, and getting the runspeed synchronized as well.
    /// </summary>
    public abstract class AbstractTssUdpClient<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : AbstractUdpClient<TCommandType, TPlayerData, TPacketizerContext>
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
        /// The game state representing the current game world.
        /// </summary>
        protected TSS<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> simulation;

        /// <summary>
        /// The remainder of time we did not update last frame, which we'll add to the
        /// elapsed time in the next frame update.
        /// </summary>
        private double lastUpdateRemainder;

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
        /// Initiliaze session and base classes.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractTssUdpClient(Game game, ushort port, string header)
            : base(game, port, header)
        {
            simulation = new TSS<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>(new uint[] { 50, 100 });
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            simulation.ThresholdExceeded += HandleThresholdExceeded;

            base.Initialize();
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            simulation.ThresholdExceeded -= HandleThresholdExceeded;

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

            if (Session.ConnectionState == ClientState.Connected && !simulation.WaitingForSynchronization)
            {
                // Drive game logic.
                if (Game.IsFixedTimeStep)
                {
                    simulation.Update();
                }
                else
                {
                    // Compensate for dynamic timestep.
                    double elapsed = gameTime.ElapsedGameTime.TotalMilliseconds + lastUpdateRemainder;
                    double target = Game.TargetElapsedTime.TotalMilliseconds;
                    while (elapsed > target)
                    {
                        elapsed -= target;
                        simulation.Update();
                    }
                    lastUpdateRemainder = elapsed;
                }

                // Hash test.
                Hasher hasher = new Hasher();
                simulation.TrailingState.Hash(hasher);
                if (simulation.TrailingFrame == hashFrame)
                {
                    if (hasher.Value != hashValue)
                    {
                        console.WriteLine("Client: hash mismatch " + hashValue + "!= " + hasher.Value + ", re-sync");
                        simulation.Invalidate();
                    }
                }

                // Send sync command every now and then, to keep game clock synched.
                if (new TimeSpan(DateTime.Now.Ticks - lastSyncTime).TotalMilliseconds > SyncInterval)
                {
                    lastSyncTime = DateTime.Now.Ticks;
                    Packet syncRequest = new Packet(5);
                    syncRequest.Write((byte)TssUdpControllerMessage.Synchronize);
                    syncRequest.Write(simulation.CurrentFrame);
                    Session.Send(syncRequest, 0);
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Called when our simulation cannot accomodate an update or rollback,
        /// meaning we have to get a server snapshot.
        /// </summary>
        private void HandleThresholdExceeded(object sender, EventArgs e)
        {
            // So we request it.
            console.WriteLine("Client: insufficient history, re-sync");
            Packet gameStateRequest = new Packet(1);
            gameStateRequest.Write((byte)TssUdpControllerMessage.GameStateRequest);
            Session.Send(gameStateRequest, 200);
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Prepends all normal command messages with the corresponding flag.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="packet">the final packet to send.</param>
        /// <returns></returns>
        protected override Packet WrapDataForSend(Commands.ICommand<TCommandType, TPlayerData, TPacketizerContext> command, Packet packet)
        {
            packet.Write((byte)TssUdpControllerMessage.Command);
            return base.WrapDataForSend(command, packet);
        }

        /// <summary>
        /// Takes care of client side TSS synchronization logic.
        /// </summary>
        protected override bool UnwrapDataForReceive(PlayerDataEventArgs<TPlayerData, TPacketizerContext> args, out ICommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
            var type = (TssUdpControllerMessage)args.Data.ReadByte();
            command = null;
            switch (type)
            {
                case TssUdpControllerMessage.Command:
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
                        long latency = (simulation.CurrentFrame - args.Data.ReadInt64()) / 2;
                        long clientServerDelta = (args.Data.ReadInt64() - simulation.CurrentFrame);
                        long frameDelta = clientServerDelta + latency / 2;
                        if (System.Math.Abs(frameDelta) > 2)
                        {
                            console.WriteLine("Client: correcting for " + frameDelta + " frames.");
                            simulation.RunToFrame(simulation.CurrentFrame + frameDelta);
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
                        Console.WriteLine("Client: game state");
                        simulation.Depacketize(args.Data, packetizer.Context);
                        return true;
                    }
                    break;
                case TssUdpControllerMessage.AddGameObject:
                    // Only accept these when they come from the server.
                    if (args.IsFromServer)
                    {
                        Console.WriteLine("Client: add object");
                        long frame = args.Data.ReadInt64();
                        TSteppable steppable = packetizer.Depacketize<TSteppable>(args.Data);
                        simulation.Add(steppable, frame);
                        return true;
                    }
                    break;
                case TssUdpControllerMessage.RemoveGameObject:
                    // Only accept these when they come from the server.
                    if (args.IsFromServer)
                    {
                        Console.WriteLine("Client: remove object");
                        long frame = args.Data.ReadInt64();
                        long steppableUid = args.Data.ReadInt64();
                        simulation.Remove(steppableUid, frame);
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
    }
}
