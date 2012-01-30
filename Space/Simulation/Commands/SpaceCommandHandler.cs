using System;
using Engine.ComponentSystem;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Simulation.Commands;
using Space.ComponentSystem.Components;

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

        #region Scripting environment

#if DEBUG
        /// <summary>
        /// The global scripting engine we'll be using.
        /// </summary>
        private static Microsoft.Scripting.Hosting.ScriptEngine _script = IronPython.Hosting.Python.CreateEngine();

        /// <summary>
        /// Used to keep multiple threads (TSS) from trying to execute
        /// scripts at the same time.
        /// </summary>
        private static object _scriptLock = new object();

        /// <summary>
        /// Set up scripting environment.
        /// </summary>
        static SpaceCommandHandler()
        {
            var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            _script.Runtime.LoadAssembly(executingAssembly);
            foreach (var assembly in executingAssembly.GetReferencedAssemblies())
            {
                _script.Runtime.LoadAssembly(System.Reflection.Assembly.Load(assembly));
            }

            try
            {
                // Register some macros in our scripting environment.
                _script.Execute(
    @"
from Engine.ComponentSystem.Components import *
from Engine.ComponentSystem.Systems import *
from Engine.ComponentSystem.RPG.Components import *
from Space.ComponentSystem.Components import *
from Space.ComponentSystem.Systems import *
from Space.Data import *

def goto(x, y):
    avatar.GetComponent[Transform]().SetTranslation(x, y)

def setBaseStat(type, value):
    character.SetBaseValue(type, value)

def ge(id):
    return manager.GetEntity(id)
", _script.Runtime.Globals);
            }
            catch (Exception ex)
            {
                logger.WarnException("Failed initializing script engine.", ex);
            }

            // Redirect scripting output to the logger.
            var infoStream = new System.IO.MemoryStream();
            var errorStream = new System.IO.MemoryStream();
            _script.Runtime.IO.SetOutput(infoStream, new InfoStreamWriter(infoStream));
            _script.Runtime.IO.SetErrorOutput(errorStream, new ErrorStreamWriter(errorStream));
        }

        /// <summary>
        /// The frame number in which we executed the last command.
        /// </summary>
        private static long _lastScriptFrame;

        /// <summary>
        /// Whether to currently ignore script output.
        /// </summary>
        private static bool _ignoreScriptOutput;
#endif

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
                case SpaceCommandType.RestoreProfile:
                    // Player wants to load his profile. Only allow this once
                    // a session, so skip if he already has an avatar.
                    if (avatar == null)
                    {
                        var profileCommand = (RestoreProfileCommand)command;
                        profileCommand.Profile.Restore(profileCommand.PlayerNumber, manager);
                    }
                    else
                    {
                        logger.Warn("Player already has an avatar, not restoring received profile.");
                    }
                    break;

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
                                input.Stabilizing = true;
                                break;
                            case PlayerInputCommand.PlayerInputCommandType.StopStabilizing:
                                input.Stabilizing = false;
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

                case SpaceCommandType.Equip:
                    {
                        var equipCommand = (EquipCommand)command;
                        try
                        {
                            var inventory = avatar.GetComponent<Inventory>();
                            var item = avatar.GetComponent<Inventory>()[equipCommand.InventoryIndex];
                            inventory.RemoveAt(equipCommand.InventoryIndex);
                            avatar.GetComponent<Equipment>().Equip(item, equipCommand.Slot);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            logger.ErrorException("Invalid equip command.", ex);
                        }
                        catch (ArgumentException ex)
                        {
                            logger.ErrorException("Invalid equip command.", ex);
                        }
                    }
                    break;

#if DEBUG
                case SpaceCommandType.AddItem:
                    {
                        var addCommand = (AddItemCommand)command;
                        var item = addCommand.Item.DeepCopy();
                        manager.AddEntity(item);
                        avatar.GetComponent<Inventory>().Add(item);
                    }
                    break;

                case SpaceCommandType.ScriptCommand:
                    // Script command.
                    {
                        var scriptCommand = (ScriptCommand)command;

                        lock (_scriptLock)
                        {
                            // Avoid multiple prints of the same message (each
                            // simulation in TSS calls this).
                            if (((FrameCommand)command).Frame > _lastScriptFrame)
                            {
                                _lastScriptFrame = ((FrameCommand)command).Frame;
                                _ignoreScriptOutput = false;
                            }
                            else
                            {
                                _ignoreScriptOutput = true;
                            }

                            // Set context.
                            var globals = _script.Runtime.Globals;

                            globals.SetVariable("manager", manager);

                            // Some more utility variables used frequently.
                            if (avatar != null)
                            {
                                globals.SetVariable("avatar", avatar);

                                var character = avatar.GetComponent<Character<Space.Data.AttributeType>>();
                                var inventory = avatar.GetComponent<Inventory>();
                                var equipment = avatar.GetComponent<Equipment>();
                                globals.SetVariable("character", character);
                                globals.SetVariable("inventory", inventory);
                                globals.SetVariable("equipment", equipment);
                            }

                            // Try executing our script.
                            try
                            {
                                _script.Execute(scriptCommand.Script, _script.Runtime.Globals);
                            }
                            catch (System.Exception ex)
                            {
                                if (!_ignoreScriptOutput)
                                {
                                    logger.ErrorException("Error executing script.", ex);
                                }
                            }
                            finally
                            {
                                globals.RemoveVariable("manager");
                                globals.RemoveVariable("avatar");
                                globals.RemoveVariable("character");
                                globals.RemoveVariable("inventory");
                                globals.RemoveVariable("equipment");
                            }
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

        #region Stream classes for script IO

#if DEBUG
        private sealed class InfoStreamWriter : System.IO.StreamWriter
        {
            public InfoStreamWriter(System.IO.Stream stream)
                : base(stream)
            {
            }

            public override void Write(string value)
            {
                if (!_ignoreScriptOutput && !string.IsNullOrWhiteSpace(value))
                {
                    logger.Info(value);
                }
            }
        }

        private sealed class ErrorStreamWriter : System.IO.StreamWriter
        {
            public ErrorStreamWriter(System.IO.Stream stream)
                : base(stream)
            {
            }

            public override void Write(string value)
            {
                if (!_ignoreScriptOutput && !string.IsNullOrWhiteSpace(value))
                {
                    logger.Error(value);
                }
            }
        }
#endif

        #endregion
    }
}
