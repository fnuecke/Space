using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.Simulation.Commands;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Used to apply commands to simulations.
    /// </summary>
    internal static class SpaceCommandHandler
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Scripting environment

        /// <summary>
        /// Symbol imports for scripting convenience.
        /// </summary>
        private const string ScriptNamespaces =
@"
from Engine.ComponentSystem import *
from Engine.ComponentSystem.Components import *
from Engine.ComponentSystem.Systems import *
from Engine.ComponentSystem.Common.Components import *
from Engine.ComponentSystem.Common.Systems import *
from Engine.ComponentSystem.RPG.Components import *
from Engine.ComponentSystem.RPG.Systems import *
from Space.ComponentSystem.Components import *
from Space.ComponentSystem.Factories import *
from Space.ComponentSystem.Systems import *
from Space.Data import *
";

        /// <summary>
        /// The global scripting engine we'll be using.
        /// </summary>
        private static readonly ScriptEngine Script = Python.CreateEngine();

        /// <summary>
        /// Used to keep multiple threads (TSS) from trying to execute
        /// scripts at the same time.
        /// </summary>
        private static readonly object ScriptLock = new object();

        /// <summary>
        /// We only use this in debug mode, so don't even bother setting
        /// it up in if we're not debugging.
        /// </summary>
        [Conditional("DEBUG")]
        public static void InitializeScriptEnvironment(ContentManager content)
        {
            // Load the executing and all referenced assemblies into the script
            // environment so they can be used for debugging.
            var executingAssembly = Assembly.GetExecutingAssembly();
            Script.Runtime.LoadAssembly(executingAssembly);
            foreach (var assembly in executingAssembly.GetReferencedAssemblies())
            {
                Script.Runtime.LoadAssembly(Assembly.Load(assembly));
            }

            // Also import all symbols from the namespaces we might need.
            try
            {
                Script.Execute(ScriptNamespaces, Script.Runtime.Globals);
            }
            catch (Exception ex)
            {
                Logger.WarnException("Failed initializing script engine.", ex);
            }

            // Redirect scripting output to the logger.
            var infoStream = new System.IO.MemoryStream();
            var errorStream = new System.IO.MemoryStream();
            Script.Runtime.IO.SetOutput(infoStream, new InfoStreamWriter(infoStream));
            Script.Runtime.IO.SetErrorOutput(errorStream, new ErrorStreamWriter(errorStream));

            // Register some macros in our scripting environment.
            try
            {
                Script.Execute(content.Load<string>("Misc/ScriptInit"), Script.Runtime.Globals);
            }
            catch (Exception ex)
            {
                Logger.WarnException("Failed initializing script engine.", ex);
            }
        }

        /// <summary>
        /// Gets the global names currently registered in the scripting environment.
        /// </summary>
        /// <returns>List of global variable names.</returns>
        public static IEnumerable<string> GetGlobalNames()
        {
            return Script.Runtime.Globals.GetVariableNames();
        }

        /// <summary>
        /// The frame number in which we executed the last command.
        /// </summary>
        private static long _lastScriptFrame;

        /// <summary>
        /// Whether to currently ignore script output.
        /// </summary>
        private static bool _ignoreScriptOutput;

        #endregion

        #region Single allocation

        private static ISet<int> _reusableItemList = new HashSet<int>();

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

                case SpaceCommandType.ScriptCommand:
                    ScriptCommand((ScriptCommand)command, manager);
                    break;
            }
        }

        #endregion

        #region Command Handlers

        private static void RestoreProfile(RestoreProfileCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(~command.PlayerNumber);

            // Only allow loading once a session, so skip if he already has an avatar.
            if (avatar.HasValue)
            {
                Logger.Warn("Player already has an avatar, not restoring received profile.");
            }
            else
            {
                lock (command.Profile)
                {
                    command.Profile.Restore(~command.PlayerNumber, manager);
                }
            }
        }

        private static void PlayerInput(PlayerInputCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the ship control.
            var control = ((ShipControl)manager.GetComponent(avatar.Value, ShipControl.TypeId));

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
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the player's inventory and equipment.
            var inventory = (Inventory)manager.GetComponent(avatar.Value, Inventory.TypeId);
            var equipment = (ItemSlot)manager.GetComponent(avatar.Value, ItemSlot.TypeId);

            // Make sure the inventory index is valid.
            if (command.InventoryIndex < 0 || command.InventoryIndex >= inventory.Capacity)
            {
                Logger.Warn("Invalid equip command, inventory index out of bounds.");
                return;
            }

            // Get the item we want to equip.
            var item = inventory[command.InventoryIndex];

            // Make sure there is an item there.
            if (item <= 0)
            {
                Logger.Warn("Invalid equip command, not item at that inventory index.");
                return;
            }

            // Validate the equipment slot.
            ItemSlot slot;
            if (!manager.HasComponent(command.Slot) ||
                (slot = manager.GetComponentById(command.Slot) as ItemSlot) == null ||
                slot.Root != equipment)
            {
                Logger.Warn("Invalid equip command, equipment slot is invalid.");
                return;
            }
            
            // Make sure the item type is correct.
            var itemComponent = (Item)manager.GetComponent(item, Item.TypeId);
            if (!slot.Validate(itemComponent))
            {
                Logger.Warn("Invalid equip command, item not valid for this equipment slot.");
                return;
            }

            // Keep reference to old item, to add it back to the inventory.
            var oldItem = slot.Item;

            // Remove from inventory and equip new item.
            inventory.RemoveAt(command.InventoryIndex);
            slot.Item = item;

            // If we unequipped something in the process, add it to the index
            // in the inventory where we got the equipped item from.
            if (oldItem > 0)
            {
                inventory.Insert(command.InventoryIndex, oldItem);
            }
        }

        private static void MoveItem(MoveItemCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // The the player's inventory.
            var inventory = ((Inventory)manager.GetComponent(avatar.Value, Inventory.TypeId));

            // Validate the indexes.
            if (command.FirstIndex < 0 || command.SecondIndex < 0 ||
                command.FirstIndex >= inventory.Capacity ||
                command.SecondIndex >= inventory.Capacity)
            {
                Logger.Warn("Invalid move item command, index out of bounds.");
                return;
            }

            // Swap the items.
            inventory.Swap(command.FirstIndex, command.SecondIndex);
        }

        private static void PickUp(PickUpCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the inventory of the player and the index system.
            var inventory = ((Inventory)manager.GetComponent(avatar.Value, Inventory.TypeId));
            var index = (IndexSystem)manager.GetSystem(IndexSystem.TypeId);
            var transform = ((Transform)manager.GetComponent(avatar.Value, Transform.TypeId));

            // We may be called from a multi threaded environment (TSS), so
            // lock this shared list.
            lock (_reusableItemList)
            {
                index.Find(transform.Translation, 100, ref _reusableItemList, Item.IndexGroupMask);
                foreach (var item in _reusableItemList)
                {
                    // Pick the item up.
                    // TODO: check if the item belongs to the player.
                    inventory.Add(item);

                    // Disable rendering, if available.
                    var renderer = (TextureRenderer)manager.GetComponent(item, TextureRenderer.TypeId);
                    if (renderer != null)
                    {
                        renderer.Enabled = false;
                    }
                }
                _reusableItemList.Clear();
            }
        }

        private static void DropItem(DropCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

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
                        var inventory = ((Inventory)manager.GetComponent(avatar.Value, Inventory.TypeId));

                        // Validate the index.
                        if (command.InventoryIndex < 0 || command.InventoryIndex >= inventory.Capacity)
                        {
                            Logger.Warn("Invalid drop command, index out of bounds.");
                            return;
                        }

                        // Get the item to drop.
                        var item = inventory[command.InventoryIndex];

                        // Do we really drop anything?
                        if (item > 0)
                        {
                            inventory.RemoveAt(command.InventoryIndex);

                            // Position the item to be at the position of the
                            // player that dropped it.
                            var transform = (Transform)manager.GetComponent(item, Transform.TypeId);
                            transform.SetTranslation(((Transform)manager.GetComponent(avatar.Value, Transform.TypeId)).Translation);
                            transform.ApplyTranslation();

                            // Enable rendering, if available.
                            var renderer = (TextureRenderer)manager.GetComponent(item, TextureRenderer.TypeId);
                            if (renderer != null)
                            {
                                renderer.Enabled = true;
                            }
                        }
                    }
                    break;
                case Source.Equipment:
                    {
                        //var equipment = ((Equipment)avatar.GetComponent(, Equipment.TypeId));
                        //var item = equipment[dropCommand.InventoryIndex];
                        //equipment.RemoveAt(dropCommand.InventoryIndex);

                        //var transform = ((Transform)item.GetComponent(, Transform.TypeId));
                        //if (transform != null)
                        //{
                        //    transform.Translation = ((Transform)avatar.GetComponent(, Transform.TypeId)).Translation;
                        //    var renderer = ((TransformedRenderer)item.GetComponent(, TransformedRenderer.TypeId));
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
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (!avatar.HasValue)
            {
                return;
            }

            // Get the inventory of the player, containing the item to use.
            var inventory = (Inventory)manager.GetComponent(avatar.Value, Inventory.TypeId);

            // Validate inventory index.
            if (command.InventoryIndex < 0 || command.InventoryIndex >= inventory.Capacity)
            {
                Logger.Warn("Invalid use command, index out of bounds.");
                return;
            }

            // Get the item.
            var id = inventory[command.InventoryIndex];
            var usable = (Usable<UsableResponse>)manager.GetComponent(id, Usable<UsableResponse>.TypeId);
            var item = (SpaceItem)manager.GetComponent(id, SpaceItem.TypeId);

            // Check if there really is an item there.
            if (id <= 0)
            {
                Logger.Warn("Invalid use command, no item at specified index.");
                return;
            }

            // Is it a usable item? If so use it. Otherwise see if we can equip the item.
            if (usable != null)
            {
                // Usable item, use it.
                ((SpaceUsablesSystem)manager.GetSystem(SpaceUsablesSystem.TypeId)).Use(usable);
            }
            else if (item != null)
            {
                // If we have a free slot for that item type equip it there,
                // otherwise swap with the first item.
                var equipment = (ItemSlot)manager.GetComponent(avatar.Value, ItemSlot.TypeId);

                // Find free slot that can take the item, or failing that, the first
                // slot that can hold the item.
                ItemSlot firstValid = null;
                foreach (var slot in equipment.AllSlots)
                {
                    // Can the item be equipped into this slot?
                    if (slot.Validate(item))
                    {
                        // Is the slot empty?
                        if (slot.Item == 0)
                        {
                            // Found one, so remove it from the inventory.
                            inventory.RemoveAt(command.InventoryIndex);

                            // And equip it.
                            slot.Item = id;

                            // And we're done.
                            return;
                        }
                        else if (firstValid == null)
                        {
                            // Already occupied, but remember the first valid one
                            // to force swapping if necessary.
                            firstValid = slot;
                        }
                    }
                }

                // Got here, so we had no empty slot. See if we can swap.
                if (firstValid != null)
                {
                    var oldItem = firstValid.Item;
                    inventory.RemoveAt(command.InventoryIndex);
                    inventory.Insert(command.InventoryIndex, oldItem);
                    firstValid.Item = id;
                }
                else
                {
                    Logger.Warn("Invalid use command, there's slot in which that item can be equipped.");
                }
            }
        }

        /// <summary>
        /// Handles scripting commands. These are ignored in release mode, as
        /// they would essentially allow uncontrolled cheating.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="manager">The manager to apply the command to.</param>
        [Conditional("DEBUG")]
        private static void ScriptCommand(ScriptCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem)manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

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

                // Create context. This is again the case because of TSS,
                // so that the different simulations don't interfere with
                // each other.
                var scope = Script.Runtime.Globals;

                // Some more utility variables used frequently.
                scope.SetVariable("manager", manager);
                scope.SetVariable("avatar", avatar);
                scope.SetVariable("character", manager.GetComponent(avatar.Value, Character<AttributeType>.TypeId));
                scope.SetVariable("inventory", manager.GetComponent(avatar.Value, Inventory.TypeId));
                scope.SetVariable("equipment", manager.GetComponent(avatar.Value, ItemSlot.TypeId));

                // Try executing our script.
                try
                {
                    Script.Execute(command.Script, scope);
                }
                catch (Exception ex)
                {
                    if (!_ignoreScriptOutput)
                    {
                        Logger.ErrorException("Error executing script.", ex);
                    }
                }
                finally
                {
                    scope.RemoveVariable("manager");
                    scope.RemoveVariable("avatar");
                    scope.RemoveVariable("character");
                    scope.RemoveVariable("inventory");
                    scope.RemoveVariable("equipment");
                }
            }
        }

        #endregion

        #region Stream classes for script IO

        /// <summary>
        /// Writes informational messages from the scripting VM to the log.
        /// </summary>
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
                    Logger.Info(value);
                }
            }
        }

        /// <summary>
        /// Writes error messages from the scripting VM to the log.
        /// </summary>
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
                    Logger.Error(value);
                }
            }
        }

        #endregion
    }
}
