namespace Space.Model
{
    interface IGameObjectFactory
    {
        Ship CreateShip(string name, int player);
    }
}
