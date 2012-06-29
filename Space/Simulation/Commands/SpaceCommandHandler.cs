using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Systems;
using Engine.Simulation.Commands;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Used to apply commands to simulations.
    /// </summary>
    static class SpaceCommandHandler
    {
        #region Logger

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Scripting environment

#if DEBUG
        /// <summary>
        /// The global scripting engine we'll be using.
        /// </summary>
        private static readonly Microsoft.Scripting.Hosting.ScriptEngine Script = IronPython.Hosting.Python.CreateEngine();

        /// <summary>
        /// Used to keep multiple threads (TSS) from trying to execute
        /// scripts at the same time.
        /// </summary>
        private static readonly object ScriptLock = new object();

        /// <summary>
        /// Set up scripting environment.
        /// </summary>
        static SpaceCommandHandler()
        {
            var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            Script.Runtime.LoadAssembly(executingAssembly);
            foreach (var assembly in executingAssembly.GetReferencedAssemblies())
            {
                Script.Runtime.LoadAssembly(System.Reflection.Assembly.Load(assembly));
            }

            try
            {
                // Register some macros in our scripting environment.
                Script.Execute(
    @"
from Engine.ComponentSystem import *
from Engine.ComponentSystem.Components import *
from Engine.ComponentSystem.Systems import *
from Engine.ComponentSystem.RPG.Components import *
from Space.ComponentSystem.Components import *
from Space.ComponentSystem.Factories import *
from Space.ComponentSystem.Systems import *
from Space.Data import *

def goto(x, y):
    manager.GetComponent[Transform](avatar).SetTranslation(x, y)

def setBaseStat(type, value):
    character.SetBaseValue(type, value)
", Script.Runtime.Globals);
            }
            catch (Exception ex)
            {
                logger.WarnException("Failed initializing script engine.", ex);
            }

            // Redirect scripting output to the logger.
            var infoStream = new System.IO.MemoryStream();
            var errorStream = new System.IO.MemoryStream();
            Script.Runtime.IO.SetOutput(infoStream, new InfoStreamWriter(infoStream));
            Script.Runtime.IO.SetErrorOutput(errorStream, new ErrorStreamWriter(errorStream));
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

        #region Single allocation

        private static readonly HashSet<int> ReusableItemList = new HashSet<int>();

        #endregion

        #region Logic

        /// <summary>
        /// Takes a command and applies it to the simulation state
        /// represented by the given entity manager.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="manager">The manager to apply it to.</param>
        public static void HandleCommand(Command command, IManager manager)
        {
            switch ((SpaceCommandType)command.Type)
            {
                case SpaceCommandType.RestoreProfile:
                    RestoreProfile((RestoreProfileCommand)command, manager);
                    break;

                case SpaceCommandType.PlayerInput:
                    PlayerInput((PlayerInputCommand)command, manager);
                    break;

                case SpaceCommandType.Equip:
                    Equip((EquipCommand)command, manager);
                    break;

                case SpaceCommandType.MoveItem:
                    MoveItem((MoveItemCommand)command, manager);
                    break;

                case SpaceCommandType.PickUp:
                    PickUp((PickUpCommand)command, manager);
                    break;

                case SpaceCommandType.DropItem:
                    DropItem((DropCommand)command, manager);
                    break;

                case SpaceCommandType.UseItem:
                    UseItem((UseCommand)command, manager);
                    break;

#if DEBUG
                case SpaceCommandType.ScriptCommand:
                    ScriptCommand((ScriptCommand)command, manager);
                    break;
#endif
            }
        }

        #endregion

        #region Command Handlers

        private static void RestoreProfile(RestoreProfileCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Only allow loading once a session, so skip if he already has an avatar.
            if (avatar.HasValue)
            {
                logger.Warn("Player already has an avatar, not restoring received profile.");
            }
            else
            {
                lock (command.Profile)
                {
                    command.Profile.Restore(command.PlayerNumber, manager);
                }
            }
        }

        private static void PlayerInput(PlayerInputCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the ship control.
            var control = manager.GetComponent<ShipControl>(avatar.Value);

            // What type of player input should we process?
            switch (command.Input)
            {
                // Accelerating in the given direction (or stop if
                // a zero vector  is given).
                case PlayerInputCommand.PlayerInputCommandType.Accelerate:
                    control.SetAcceleration(command.Value);
                    break;

                // Begin/stop to stabilize our position.
                case PlayerInputCommand.PlayerInputCommandType.BeginStabilizing:
                    control.Stabilizing = true;
                    break;
                case PlayerInputCommand.PlayerInputCommandType.StopStabilizing:
                    control.Stabilizing = false;
                    break;

                // Begin rotating.
                case PlayerInputCommand.PlayerInputCommandType.Rotate:
                    control.SetTargetRotation(command.Value.X);
                    break;

                // Begin/stop shooting.
                case PlayerInputCommand.PlayerInputCommandType.BeginShooting:
                    control.Shooting = true;
                    break;
                case PlayerInputCommand.PlayerInputCommandType.StopShooting:
                    control.Shooting = false;
                    break;
            }
        }

        private static void Equip(EquipCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the player's inventory and equipment.
            var inventory = manager.GetComponent<Inventory>(avatar.Value);
            var equipment = manager.GetComponent<Equipment>(avatar.Value);

            // Make sure the inventory index is valid.
            if (command.InventoryIndex < 0 || command.InventoryIndex >= inventory.Capacity)
            {
                logger.Warn("Invalid equip command, inventory index out of bounds.");
                return;
            }

            // Get the item we want to equip.
            var item = inventory[command.InventoryIndex];

            // Make sure there is an item there.
            if (!item.HasValue)
            {
                logger.Warn("Invalid equip command, not item at that inventory index.");
                return;
            }

            // Make sure the equipment index is valid.
            var itemType = manager.GetComponent<Item>(item.Value).GetType();
            if (command.Slot < 0 || command.Slot >= equipment.GetSlotCount(itemType))
            {
                logger.Warn("Invalid equip command, equipment slot out of bounds.");
                return;
            }

            // See if there's an item equipped there, currently.
            var equipped = equipment.Unequip(itemType, command.Slot);
            inventory.RemoveAt(command.InventoryIndex);
            equipment.Equip(command.Slot, item.Value);
            if (equipped.HasValue)
            {
                // We unequipped something in the process, add it to the index
                // in the inventory where we got the equipped item from.
                inventory.Insert(command.InventoryIndex, equipped.Value);
            }
        }

        private static void MoveItem(MoveItemCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // The the player's inventory.
            var inventory = manager.GetComponent<Inventory>(avatar.Value);

            // Validate the indexes.
            if (command.FirstIndex < 0 || command.SecondIndex < 0 ||
                command.FirstIndex >= inventory.Capacity ||
                command.SecondIndex >= inventory.Capacity)
            {
                logger.Warn("Invalid move item command, index out of bounds.");
                return;
            }

            // Swap the items.
            inventory.Swap(command.FirstIndex, command.SecondIndex);
        }

        private static void PickUp(PickUpCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the inventory of the player and the index system.
            var inventory = manager.GetComponent<Inventory>(avatar.Value);
            var index = manager.GetSystem<IndexSystem>();

            // We may be called from a multi threaded environment (TSS), so
            // lock this shared list.
            lock (ReusableItemList)
            {
                foreach (var item in index.RangeQuery(avatar.Value, 100, Item.IndexGroup, ReusableItemList))
                {
                    // Pick the item up.
                    // TODO: check if the item belongs to the player.
                    inventory.Add(item);
                }
                ReusableItemList.Clear();
            }
        }

        private static void DropItem(DropCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Where do we want to drop from.
            switch (command.Source)
            {
                case Source.Inventory:
                    {
                        // From our inventory, so get it.
                        var inventory = manager.GetComponent<Inventory>(avatar.Value);

                        // Validate the index.
                        if (command.InventoryIndex < 0 || command.InventoryIndex >= inventory.Capacity)
                        {
                            logger.Warn("Invalid drop command, index out of bounds.");
                            return;
                        }

                        // Get the item to drop.
                        var item = inventory[command.InventoryIndex];

                        // Do we really drop anything?
                        if (item.HasValue)
                        {
                            inventory.RemoveAt(command.InventoryIndex);

                            // Position the item to be at the position of the
                            // player that dropped it.
                            var transform = manager.GetComponent<Transform>(item.Value);
                            transform.SetTranslation(manager.GetComponent<Transform>(avatar.Value).Translation);
                        }
                    }
                    break;
                case Source.Equipment:
                    {
                        //var equipment = avatar.GetComponent<Equipment>();
                        //var item = equipment[dropCommand.InventoryIndex];
                        //equipment.RemoveAt(dropCommand.InventoryIndex);

                        //var transform = item.GetComponent<Transform>();
                        //if (transform != null)
                        //{
                        //    transform.Translation = avatar.GetComponent<Transform>().Translation;
                        //    var renderer = item.GetComponent<TransformedRenderer>();
                        //    if (renderer != null)
                        //    {
                        //        renderer.Enabled = true;
                        //    }
                        //}
                    }
                    break;
            }
        }

        private static void UseItem(UseCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the inventory of the player, containing the item to use.
            var inventory = manager.GetComponent<Inventory>(avatar.Value);

            // Validate inventory index.
            if (command.InventoryIndex < 0 || command.InventoryIndex >= inventory.Capacity)
            {
                logger.Warn("Invalid use command, index out of bounds.");
                return;
            }

            var item = inventory[command.InventoryIndex];

            // Check if there really is an item there.
            if (!item.HasValue)
            {
                return;
            }

            // Is it a usable item, if so use it. Otherwise see if we can
            // equip the item.
            var usable = manager.GetComponent<Usable<UsableResponse>>(item.Value);
            if (usable != null)
            {
                // Usable item, use it.
                manager.GetSystem<SpaceUsablesSystem>().Use(usable);
            }
            else
            {
                // Not a usable item, see if we can equip it.
                var itemType = manager.GetComponent<SpaceItem>(item.Value);
                if (itemType != null)
                {
                    // If we have a free slot for that item type equip it there,
                    // otherwise swap with the first item.
                    var equipment = manager.GetComponent<Equipment>(avatar.Value);

                    // Number of slots for that type.
                    var numSlots = equipment.GetSlotCount(itemType.GetType());
                    
                    // Make sure we can even equip this.
                    if (numSlots < 1)
                    {
                        // Nope, we can't. Ignore the command.
                        return;
                    }
                    
                    // We can, so remove it from the inventory.
                    inventory.RemoveAt(command.InventoryIndex);

                    // Try to find a free slot.
                    for (int i = 0; i < numSlots; i++)
                    {
                        if (!equipment.GetItem(itemType.GetType(), i).HasValue)
                        {
                            // Free slot found, equip it there.
                            equipment.Equip(i, item.Value);
                            return;
                        }
                    }

                    // No free slot found, swap with the first slot.
                    var equipped = equipment.Unequip(itemType.GetType(), 0);
                    inventory.Insert(command.InventoryIndex, equipped.Value);
                    equipment.Equip(0, item.Value);
                }
            }
        }

#if DEBUG
        private static void ScriptCommand(ScriptCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = manager.GetSystem<AvatarSystem>().GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // We only have one engine in a potential multi threaded
            // environment, so make sure we only access it once at a
            // time.
            lock (ScriptLock)
            {
                // Avoid multiple prints of the same message (each
                // simulation in TSS calls this).
                if (command.Frame > _lastScriptFrame)
                {
                    _lastScriptFrame = command.Frame;
                    _ignoreScriptOutput = false;
                }
                else
                {
                    _ignoreScriptOutput = true;
                }

                // Set context.
                var globals = Script.Runtime.Globals;

                // Some more utility variables used frequently.
                globals.SetVariable("manager", manager);
                globals.SetVariable("avatar", avatar);
                globals.SetVariable("character", manager.GetComponent<Character<AttributeType>>(avatar.Value));
                globals.SetVariable("inventory", manager.GetComponent<Inventory>(avatar.Value));
                globals.SetVariable("equipment", manager.GetComponent<Equipment>(avatar.Value));

                // Try executing our script.
                try
                {
                    Script.Execute(command.Script, Script.Runtime.Globals);
                }
                catch (Exception ex)
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
#endif

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
