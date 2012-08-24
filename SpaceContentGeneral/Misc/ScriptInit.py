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

def inv():
	def dump(item):
		print(manager.GetComponent(item, Item.TypeId).Name)
		for slot in manager.GetComponents(item, ItemSlot.TypeId):
			if slot.Item > 0:
				dump(slot.Item)
	if equipment.Item > 0:
		dump(equipment.Item)