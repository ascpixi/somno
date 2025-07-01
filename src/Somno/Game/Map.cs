namespace Somno.Game;

internal class Map
{
    /// <summary>
    /// The name of the map, without the <c>.bsp</c> extension.
    /// </summary>
    public readonly string Name;

    public Map(string name)
    {
        Name = name;
    }
}
