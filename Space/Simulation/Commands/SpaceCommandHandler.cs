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
                            // Start accelerating in the given direction.
                            case PlayerInputCommand.PlayerInputCommandType.Accelerate:
                                input.Accelerate(inputCommand.Value);
                                break;
                            

                            // Stop accelerating in the given direction.
                            case PlayerInputCommand.PlayerInputCommandType.Stop:
                                input.StopAccelerate();
                                break;
                            

                            // Begin rotating.
                            case PlayerInputCommand.PlayerInputCommandType.Rotate:
                                input.TargetRotation = inputCommand.Value.X;
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
                                if (avatar == null)
                                {
                                    return;
                                }
                                var accelerationForce = debugCommand.Data.ReadSingle();
                                foreach (var thruster in avatar.GetComponent<EntityModules<EntityAttributeType>>().GetModules<ThrusterModule>())
                                {
                                    thruster.AccelerationForce = accelerationForce;
                                }
                                break;
                            case DebugCommand.DebugCommandType.SetThrusterEnergyConsumption:
                                if (avatar == null)
                                {
                                    return;
                                }
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
