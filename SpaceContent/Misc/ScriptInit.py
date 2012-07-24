def goto(x, y):
    manager.GetComponent[Transform](avatar).SetTranslation(x, y)

def setBaseStat(type, value):
    character.SetBaseValue(type, value)