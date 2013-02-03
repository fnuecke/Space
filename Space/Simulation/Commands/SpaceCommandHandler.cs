using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.Simulation.Commands;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.Simulation.Commands
{
    /// <summary>Used to apply commands to simulations.</summary>
    internal static class SpaceCommandHandler
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Single allocation

        private static readonly ISet<int> ReusableItemList = new HashSet<int>();

        #endregion

        #region Logic

        /// <summary>Takes a command and applies it to the simulation state represented by the given entity manager.</summary>
        /// <param name="command">The command to process.</param>
        /// <param name="manager">The manager to apply it to.</param>
        public static void HandleCommand(Command command, IManager manager)
        {
            switch ((SpaceCommandType) command.Type)
            {
                case SpaceCommandType.RestoreProfile:
                    RestoreProfile((RestoreProfileCommand) command, manager);
                    break;

                case SpaceCommandType.PlayerInput:
                    PlayerInput((PlayerInputCommand) command, manager);
                    break;

                case SpaceCommandType.Equip:
                    Equip((EquipCommand) command, manager);
                    break;

                case SpaceCommandType.MoveItem:
                    MoveItem((MoveItemCommand) command, manager);
                    break;

                case SpaceCommandType.PickUp:
                    PickUp((PickUpCommand) command, manager);
                    break;

                case SpaceCommandType.DropItem:
                    DropItem((DropCommand) command, manager);
                    break;

                case SpaceCommandType.UseItem:
                    UseItem((UseCommand) command, manager);
                    break;

                case SpaceCommandType.ScriptCommand:
                    ScriptCommand((ScriptCommand) command, manager);
                    break;
            }
        }

        #endregion

        #region Command Handlers

        private static void RestoreProfile(RestoreProfileCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(~command.PlayerNumber);

            // Only allow loading once a session, so skip if he already has an avatar.
            if (avatar > 0)
            {
                Logger.Warn("Player already has an avatar, not restoring received profile.");
            }
            else
            {
                lock (command.Profile)
                {
                    command.Profile.Restore(manager, ~command.PlayerNumber);
                }
            }
        }

        private static void PlayerInput(PlayerInputCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (avatar <= 0)
            {
                return;
            }

            // Get the ship control.
            var control = ((ShipControl) manager.GetComponent(avatar, ShipControl.TypeId));

            // What type of player input should we process?
            switch (command.Input)
            {
                    // Accelerating in the given direction (or stop if
                    // a zero vector  is given).
                case PlayerInputCommand.PlayerInputCommandType.Accelerate:
                    control.SetAcceleration(command.Value);
                    break;

                    // Begin rotating.
                case PlayerInputCommand.PlayerInputCommandType.Rotate:
                    control.SetTargetRotation(command.Value.X);
                    break;

                    // Begin/stop to stabilize our position.
                case PlayerInputCommand.PlayerInputCommandType.BeginStabilizing:
                    control.Stabilizing = true;
                    break;
                case PlayerInputCommand.PlayerInputCommandType.StopStabilizing:
                    control.Stabilizing = false;
                    break;

                    // Begin/stop shooting.
                case PlayerInputCommand.PlayerInputCommandType.BeginShooting:
                    control.Shooting = true;
                    break;
                case PlayerInputCommand.PlayerInputCommandType.StopShooting:
                    control.Shooting = false;
                    break;

                    // Begin/stop shielding.
                case PlayerInputCommand.PlayerInputCommandType.BeginShielding:
                    if (!control.ShieldsActive)
                    {
                        control.ShieldsActive = true;
                        manager.AddComponent<ShieldEnergyStatusEffect>(avatar);
                    }
                    break;
                case PlayerInputCommand.PlayerInputCommandType.StopShielding:
                    if (control.ShieldsActive)
                    {
                        control.ShieldsActive = false;
                        manager.RemoveComponent(manager.GetComponent(avatar, ShieldEnergyStatusEffect.TypeId));
                    }
                    break;
            }
        }

        private static void Equip(EquipCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (avatar <= 0)
            {
                return;
            }

            // Get the player's inventory and equipment.
            var inventory = (Inventory) manager.GetComponent(avatar, Inventory.TypeId);
            var equipment = (ItemSlot) manager.GetComponent(avatar, ItemSlot.TypeId);

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
            var itemComponent = (Item) manager.GetComponent(item, Item.TypeId);
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
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (avatar <= 0)
            {
                return;
            }

            // The the player's inventory.
            var inventory = ((Inventory) manager.GetComponent(avatar, Inventory.TypeId));

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
        
        /// <summary>Store interface type id for performance.</summary>
        private static readonly int DrawableTypeId = Manager.GetComponentTypeId<IDrawable>();

        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Manager.GetComponentTypeId<ITransform>();

        private static void PickUp(PickUpCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (avatar <= 0)
            {
                return;
            }

            // Get the inventory of the player and the index system.
            var inventory = ((Inventory) manager.GetComponent(avatar, Inventory.TypeId));
            var index = (IndexSystem) manager.GetSystem(IndexSystem.TypeId);
            var transform = ((ITransform) manager.GetComponent(avatar, TransformTypeId));

            // We may be called from a multi threaded environment (TSS), so
            // lock this shared list.
            lock (ReusableItemList)
            {
                index[PickupSystem.IndexId].Find(transform.Position, UnitConversion.ToSimulationUnits(100), ReusableItemList);
                foreach (IIndexable item in ReusableItemList.Select(manager.GetComponentById))
                {
                    // Pick the item up.
                    // TODO: check if the item belongs to the player.
                    inventory.Add(item.Entity);

                    // Disable rendering, if available.
                    foreach (IDrawable drawable in manager.GetComponents(item.Entity, DrawableTypeId))
                    {
                        drawable.Enabled = false;
                    }
                }
                ReusableItemList.Clear();
            }
        }
        
        private static void DropItem(DropCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (avatar <= 0)
            {
                return;
            }

            // Where do we want to drop from.
            switch (command.Source)
            {
                case Source.Inventory:
                {
                    // From our inventory, so get it.
                    var inventory = ((Inventory) manager.GetComponent(avatar, Inventory.TypeId));

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
                        var transform = (ITransform) manager.GetComponent(item, TransformTypeId);
                        transform.Position =
                            ((ITransform) manager.GetComponent(avatar, TransformTypeId)).Position;

                        // Enable rendering, if available.
                        foreach (IDrawable drawable in manager.GetComponents(item, DrawableTypeId))
                        {
                            drawable.Enabled = true;
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
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (avatar <= 0)
            {
                return;
            }

            // Get the inventory of the player, containing the item to use.
            var inventory = (Inventory) manager.GetComponent(avatar, Inventory.TypeId);

            // Validate inventory index.
            if (command.InventoryIndex < 0 || command.InventoryIndex >= inventory.Capacity)
            {
                Logger.Warn("Invalid use command, index out of bounds.");
                return;
            }

            // Get the item.
            var id = inventory[command.InventoryIndex];
            var usable = (Usable<UsableResponse>) manager.GetComponent(id, Usable<UsableResponse>.TypeId);
            var item = (SpaceItem) manager.GetComponent(id, Item.TypeId);

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
                ((SpaceUsablesSystem) manager.GetSystem(SpaceUsablesSystem.TypeId)).Use(usable);
            }
            else if (item != null)
            {
                // If we have a free slot for that item type equip it there,
                // otherwise swap with the first item.
                var equipment = (ItemSlot) manager.GetComponent(avatar, ItemSlot.TypeId);

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
                        if (firstValid == null)
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
        ///     Handles scripting commands. These are ignored in release mode, as they would essentially allow uncontrolled
        ///     cheating.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="manager">The manager to apply the command to.</param>
        [Conditional("DEBUG")]
        private static void ScriptCommand(ScriptCommand command, IManager manager)
        {
            // Get the avatar of the related player.
            var avatar = ((AvatarSystem) manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(command.PlayerNumber);

            // Make sure we have the player's avatar.
            if (avatar <= 0)
            {
                return;
            }

            // We only have one engine in a potential multi threaded
            // environment, so make sure we only access it once at a
            // time.
            var scriptSystem = (ScriptSystem) manager.GetSystem(ScriptSystem.TypeId);
            scriptSystem.Call("setExecutingPlayer", command.PlayerNumber);
            try
            {
                var result = scriptSystem.Execute(command.Script);
                if (result != null)
                {
                    scriptSystem.Call("print", result);
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.ErrorException("Error executing script.\n", ex);
            }
            finally
            {
                scriptSystem.Call("setExecutingPlayer", -1);
            }
        }

        #endregion
    }
}