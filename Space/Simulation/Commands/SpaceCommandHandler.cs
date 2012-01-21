﻿using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Simulation.Commands;
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
            // We normally want to mess with the player's ship somehow.
            var avatar = manager.SystemManager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            switch ((SpaceCommandType)command.Type)
            {
                case SpaceCommandType.PlayerInput:
                    // Player input command, apply it.
                    {
                        var inputCommand = (PlayerInputCommand)command;

                        // Make sure we have the player's avatar.
                        if (avatar == null)
                        {
                            return;
                        }
                        var input = avatar.GetComponent<ShipControl>();

                        // What type of player input should we process?
                        switch (inputCommand.Input)
                        {
                            // Accelerating in the given direction (or stop if
                            // a zero vector  is given).
                            case PlayerInputCommand.PlayerInputCommandType.Accelerate:
                                input.SetAcceleration(inputCommand.Value);
                                break;

                            // Begin/stop to stabilize our position.
                            case PlayerInputCommand.PlayerInputCommandType.BeginStabilizing:
                                input.SetStabilizing(true);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.StopStabilizing:
                                input.SetStabilizing(false);
                                break;

                            // Begin rotating.
                            case PlayerInputCommand.PlayerInputCommandType.Rotate:
                                input.SetTargetRotation(inputCommand.Value.X);
                                break;

                            // Begin/stop shooting.
                            case PlayerInputCommand.PlayerInputCommandType.BeginShooting:
                                input.SetShooting(true);
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.StopShooting:
                                input.SetShooting(false);
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

                        // What's the debug command to process?
                        switch (debugCommand.Debug)
                        {
                            // Jump to location.
                            case DebugCommand.DebugCommandType.GotoPosition:
                                if (avatar == null)
                                {
                                    return;
                                }
                                avatar.GetComponent<Transform>().SetTranslation(debugCommand.Data.ReadVector2());
                                break;

                            // Adjust thruster stats.
                            case DebugCommand.DebugCommandType.SetThrusterAccelerationForce:
                                {
                                    if (avatar == null)
                                    {
                                        return;
                                    }
                                    var accelerationForce = debugCommand.Data.ReadSingle();
                                    var modules = avatar.GetComponent<EntityModules<EntityAttributeType>>();
                                    foreach (var thruster in modules.GetModules<ThrusterModule>())
                                    {
                                        thruster.AccelerationForce = accelerationForce;
                                    }
                                    ModuleValueInvalidated<EntityAttributeType> invalidatedMessage;
                                    invalidatedMessage.ValueType = EntityAttributeType.AccelerationForce;
                                    avatar.SendMessageToComponents(ref invalidatedMessage);
                                }
                                break;
                            case DebugCommand.DebugCommandType.SetThrusterEnergyConsumption:
                                {
                                    if (avatar == null)
                                    {
                                        return;
                                    }
                                    var energyConsumption = debugCommand.Data.ReadSingle();
                                    var modules = avatar.GetComponent<EntityModules<EntityAttributeType>>();
                                    foreach (var thruster in modules.GetModules<ThrusterModule>())
                                    {
                                        thruster.EnergyConsumption = energyConsumption;
                                    }
                                    ModuleValueInvalidated<EntityAttributeType> invalidatedMessage;
                                    invalidatedMessage.ValueType = EntityAttributeType.ThrusterEnergyConsumption;
                                    avatar.SendMessageToComponents(ref invalidatedMessage);
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
