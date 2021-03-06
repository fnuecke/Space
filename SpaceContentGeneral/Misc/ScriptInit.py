﻿"""
Import anything we may need to interact with the simulation.
"""

from Engine.ComponentSystem import *
from Engine.ComponentSystem.Components import *
from Engine.ComponentSystem.Systems import *
from Engine.ComponentSystem.Common.Components import *
from Engine.ComponentSystem.Common.Systems import *
from Engine.ComponentSystem.RPG.Components import *
from Engine.ComponentSystem.RPG.Systems import *
from Engine.ComponentSystem.Spatial.Components import *
from Engine.ComponentSystem.Spatial.Systems import *
from Engine.FarMath import *
from Space.ComponentSystem import *
from Space.ComponentSystem.Components import *
from Space.ComponentSystem.Factories import *
from Space.ComponentSystem.Systems import *
from Space.Data import *

"""
One global variable is always available, 'manager', which refers to the manager this script lives in.
"""

# Some context sensitive values, adjusted if a command is executed for a specific player.
avatar = None
attributes = None
inventory = None
equipment = None

def setExecutingPlayer(player):
    """Prepares the environment for executing a command for a player."""
    global avatar, attributes, inventory, equipment
    if player >= 0:
        avatar = manager.GetSystem(AvatarSystem.TypeId).GetAvatar(player)
        attributes = manager.GetComponent(avatar, Attributes[AttributeType].TypeId)
        inventory = manager.GetComponent(avatar, Inventory.TypeId)
        equipment = manager.GetComponent(avatar, ItemSlot.TypeId)
    else:
        avatar = attributes = inventory = equipment = None

"""
Debugging utility stuff.
"""

def goto(x, y):
    """Moves the player's ship to the specified coordinates."""
    component = manager.GetComponent(avatar, manager.GetComponentTypeId[ITransform]())
    component.Position = FarPosition(x, y)

def setBaseStat(type, value):
    """Set the player's base value of the specified stat."""
    attributes.SetBaseValue(type, value)

def desync():
    """Forces desynchronization by using a system local random value."""
    import sys
    sys.path.append("C:\\Program Files (x86)\\IronPython 2.7.1\\Lib")
    import random
    translation = manager.GetComponent(avatar, Transform.TypeId).Translation
    goto(translation.X + random.random(), translation.Y + random.random())

def listEquipment():
    """Prints a list of the items equipped by the player's."""
    def dump(slotId, itemId):
        item = manager.GetComponent(itemId, Item.TypeId)
        print("Slot: %d = %s (Entity: %d)" % (slotId, item.Name, item.Entity))
        for slot in manager.GetComponents(itemId, ItemSlot.TypeId):
            if slot.Item > 0:
                dump(slot.Id, slot.Item)
    if equipment.Item > 0:
        dump(equipment.Id, equipment.Item)

def listInventory():
    """Prints a list of the items in the player's inventory."""
    for itemId in inventory:
        item = manager.GetComponent(itemId, Item.TypeId)
        print("%s (Entity: %d)" % (item.Name, item.Entity))

def equip(itemId):
    """Equips the specified item from the player's inventory (see listInventory)."""
    item = manager.GetComponent(itemId, Item.TypeId)
    if not item:
        raise ValueError("Invalid item.")
    elif inventory.Contains(itemId):
        # Find an empty slot into which we can fit the item.
        for slot in equipment.AllSlots:
            if slot.Item == 0 and slot.Validate(item):
                slot.Item = itemId
                inventory.Remove(itemId)
                return
    else:
        raise ValueError("Item not in inventory.")

def unequip(slotId):
    """Unequips the item from the slot with the specified id (see listEquipment)."""
    slot = manager.GetComponentById(slotId)
    if slot.Item > 0:
        itemId = slot.Item
        slot.Item = 0
        inventory.Add(itemId)

def level():
    """Prints some information on the player's experience level."""
    xp = manager.GetComponent(avatar, Experience.TypeId)
    print("Level %s [%d / %d XP (%.2f%%)" % (xp.Level, xp.Value, xp.RequiredForNextLevel, (xp.Value / float(xp.RequiredForNextLevel))))

def setFactions():
    """Puts the player into the first and second NPC faction."""
    f = manager.GetComponent(avatar, Faction.TypeId)
    f.Value = f.Value | Factions.NpcFactionA | Factions.NpcFactionB

def ftext(value):
    """Displays the specified floating text at the player's location."""
    manager.GetSystem(FloatingTextSystem.TypeId).Display(value, manager.GetComponent(1, Manager.GetComponentTypeId[ITransform]()).Position)

def addAi(squadMember):
    """Adds a new AI ship to the squad of the specified AI."""
    squad = manager.GetComponent(squadMember, Squad.TypeId);
    if not squad:
        squad = manager.AddComponent[Squad](squadMember)
    position = manager.GetComponent(squadMember, Manager.GetComponentTypeId[ITransform]()).Position;
    faction = manager.GetComponent(squadMember, Faction.TypeId).Value;
    ship = EntityFactory.CreateAIShip(manager, "L1_AI_Ship", faction, position, None)
    ai = manager.GetComponent(ship, ArtificialIntelligence.TypeId)
    manager.AddComponent[Squad](ship)
    squad.AddMember(ship)
    ai.Guard(squad.Leader)

def setFormation(entity, formation):
    """Sets the formation for the squad of the specified entity to the specified type."""
    manager.GetComponent(entity, Squad.TypeId).Formation = formation