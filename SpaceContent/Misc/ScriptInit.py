def goto(x, y):
    manager.GetComponent[Transform](avatar).SetTranslation(x, y)

def setBaseStat(type, value):
    character.SetBaseValue(type, value)

def desync():
	import sys
	sys.path.append("C:\\Program Files (x86)\\IronPython 2.7.1\\Lib")
	import random
	translation = manager.GetComponent[Transform](avatar).Translation
	goto(translation.X + random.random(), translation.Y + random.random())
