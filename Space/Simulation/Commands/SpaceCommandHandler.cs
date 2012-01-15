using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Simulation.Commands;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Data.Modules;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Used to apply commands to simulations.
    /// </summary>
    static class SpaceCommandHandler
    {
        #region Logger
        
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Logic

        /// <summary>
        /// Takes a command and applies it to the simulation state
        /// represented by the given entity manager.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="manager">The manager to apply it to.</param>
        public static void HandleCommand(Command command, IEntityManager manager)
        {
            switch ((SpaceCommandType)command.Type)
            {
                case SpaceCommandType.PlayerInput:
                    // Player input command, apply it.
                    {
                        var inputCommand = (PlayerInputCommand)command;

                        // Get the player's avatar, and the ship controller.
                        var avatar = manager.SystemManager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);
                        var input = avatar.GetComponent<ShipControl>();

                        // What type of player input should we process?
                        switch (inputCommand.Input)
                        {
                            // Start accelerating in the given direction.
                            case PlayerInputCommand.PlayerInputCommandType.AccelerateUp:
                                input.Accelerate(Directions.North);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.AccelerateRight:
                                input.Accelerate(Directions.East);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.AccelerateDown:
                                input.Accelerate(Directions.South);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.AccelerateLeft:
                                input.Accelerate(Directions.West);
                                break;

                            // Stop accelerating in the given direction.
                            case PlayerInputCommand.PlayerInputCommandType.StopUp:
                                input.StopAccelerate(Directions.North);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.StopRight:
                                input.StopAccelerate(Directions.East);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.StopDown:
                                input.StopAccelerate(Directions.South);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.StopLeft:
                                input.StopAccelerate(Directions.West);
                                break;

                            // Begin rotating.
                            case PlayerInputCommand.PlayerInputCommandType.Rotate:
                                input.TargetRotation = inputCommand.Value;
                                break;

                            // Begin/stop shooting.
                            case PlayerInputCommand.PlayerInputCommandType.Shoot:
                                input.Shooting = true;
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.CeaseFire:
                                input.Shooting = false;
                                break;
                        }
                    }
                    break;

#if DEBUG
                case SpaceCommandType.DebugCommand:
                    // Debug command.
                    {
                        var debugCommand = (DebugCommand)command;

                        // Rewind the data packet.
                        if (debugCommand.Data != null)
                        {
                            debugCommand.Data.Reset();
                        }

                        // We normally want to mess with the player's ship somehow.
                        var avatar = manager.SystemManager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

                        // What's the debug command to process?
                        switch (debugCommand.Debug)
                        {
                            // Jump to location.
                            case DebugCommand.DebugCommandType.GotoPosition:
                                avatar.GetComponent<Transform>().SetTranslation(debugCommand.Data.ReadVector2());
                                break;

                            // Adjust thruster stats.
                            case DebugCommand.DebugCommandType.SetThrusterAccelerationForce:
                                var accelerationForce = debugCommand.Data.ReadSingle();
                                foreach (var thruster in avatar.GetComponent<EntityModules<EntityAttributeType>>().GetModules<ThrusterModule>())
                                {
                                    thruster.AccelerationForce = accelerationForce;
                                }
                                break;
                            case DebugCommand.DebugCommandType.SetThrusterEnergyConsumption:
                                var energyConsumption = debugCommand.Data.ReadSingle();
                                foreach (var thruster in avatar.GetComponent<EntityModules<EntityAttributeType>>().GetModules<ThrusterModule>())
                                {
                                    thruster.EnergyConsumption = energyConsumption;
                                }
                                break;

                            default:
                                logger.Warn("Unhandled debug command type: {0}", debugCommand.Debug);
                                break;
                        }
                    }
                    break;
#endif

                default:
                    // Unknown, ignore.
                    break;
            }
        }

        #endregion
    }
}
