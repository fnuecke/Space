def goto(x, y):
	component = manager.GetComponent(avatar, Transform.TypeId)
	component.SetTranslation(x, y)
	component.ApplyTranslation()

def setBaseStat(type, value):
    character.SetBaseValue(type, value)

def desync():
	import sys
	sys.path.append("C:\\Program Files (x86)\\IronPython 2.7.1\\Lib")
	import random
	translation = manager.GetComponent(avatar, Transform.TypeId).Translation
	goto(translation.X + random.random(), translation.Y + random.random())

def listEquipment():
	def dump(slotId, itemId):
		item = manager.GetComponent(itemId, Item.TypeId)
		print("Slot: %d = %s (Entity: %d)" % (slotId, item.Name, item.Entity))
		for slot in manager.GetComponents(itemId, ItemSlot.TypeId):
			if slot.Item > 0:
				dump(slot.Id, slot.Item)
	if equipment.Item > 0:
		dump(equipment.Id, equipment.Item)

def listInventory():
	for itemId in inventory:
		item = manager.GetComponent(itemId, Item.TypeId)
		print("%s (Entity: %d)" % (item.Name, item.Entity))

def equip(itemId):
	item = manager.GetComponent(itemId, Item.TypeId)
	if not item:
		print("Invalid item.")
		return
	if inventory.Contains(itemId):
		for slot in equipment.AllSlots:
			if slot.Item == 0 and slot.Validate(item):
				slot.Item = itemId
				inventory.Remove(itemId)
				return

def unequip(slotId):
	slot = manager.GetComponentById(slotId)
	if slot.Item > 0:
		itemId = slot.Item
		slot.Item = 0
		inventory.Add(itemId)

def level():
	xp = manager.GetComponent(avatar, Experience.TypeId)
	print("Level %s [%d / %d XP (%.2f%%)" % (xp.Level, xp.Value, xp.RequiredForNextLevel, (xp.Value / float(xp.RequiredForNextLevel))))