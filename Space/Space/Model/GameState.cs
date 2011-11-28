using System.Collections.Generic;
using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Space.Commands;

namespace Space.Model
{
    class GameState : PhysicsEnabledState<GameState, IGameObject, GameCommandType>
    {
        protected override GameState ThisState { get { return this; } }

        private Game game;

        private ISession<PlayerInfo> session;

        private Dictionary<int, long> playerShips = new Dictionary<int, long>();

        public GameState()
        {
        }

        public GameState(Game game, ISession<PlayerInfo> session)
        {
            this.game = game;
            this.session = session;
        }

        public Ship GetPlayerShip(int player)
        {
            if (playerShips.ContainsKey(player))
            {
                return (Ship)Get(playerShips[player]);
            }
            return null;
        }

        protected override void HandleCommand(ISimulationCommand<GameCommandType> command)
        {
            switch (command.Type)
            {
                case GameCommandType.PlayerInput:
                    {
                        var inputCommand = (PlayerInputCommand)command;
                        Ship ship = GetPlayerShip(command.Player);
                        if (ship == null)
                        {
                            // No ship for this player.
                            return;
                        }
                        switch (inputCommand.Input)
                        {
                            case PlayerInputCommand.PlayerInput.Accelerate:
                                ship.Accelerate(inputCommand.Direction);
                                break;
                            case PlayerInputCommand.PlayerInput.StopMovement:
                                ship.StopMovement();
                                break;
                            case PlayerInputCommand.PlayerInput.TurnLeft:
                                ship.TurnLeft();
                                break;
                            case PlayerInputCommand.PlayerInput.TurnRight:
                                ship.TurnRight();
                                break;
                            case PlayerInputCommand.PlayerInput.StopRotation:
                                ship.StopRotating();
                                break;
                        }
                    }
                    break;
                case GameCommandType.AddPlayerShip:
                    {
                        var addCommand = (AddPlayerCommand)command;
                        var player = session.GetPlayer(addCommand.PlayerNumber);
                        var ship = ((GameObjectFactory)game.Services.GetService(typeof(IGameObjectFactory))).CreateShip(player.Data.ShipName, player.Number);
                        if (player.Data.ShipUID > 0)
                        {
                            ship.UID = player.Data.ShipUID;
                        }
                        else
                        {
                            player.Data.ShipUID = ship.UID;
                        }
                        playerShips[command.Player] = ship.UID;
                        Add(ship);
                    }
                    break;
                case GameCommandType.RemovePlayerShip:
                    break;
                default:
                    break;
            }
        }

        public override object Clone()
        {
            var clone = new GameState(game, session);
            foreach (var key in playerShips.Keys)
            {
                clone.playerShips[key] = playerShips[key];
            }

            return CloneTo(clone);
        }

        public override void Packetize(Packet packet)
        {
            packet.Write(playerShips.Count);
            foreach (var kv in playerShips)
            {
                packet.Write(kv.Key);
                packet.Write(kv.Value);
            }

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            playerShips.Clear();
            int numPlayerShips = packet.ReadInt32();
            for (int i = 0; i < numPlayerShips; ++i)
            {
                int key = packet.ReadInt32();
                long value = packet.ReadInt64();
                playerShips[key] = value;
            }

            base.Depacketize(packet);
        }
    }
}
