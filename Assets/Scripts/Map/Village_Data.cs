
public class Village_Data
{
    public string name;
    public Coord center;
    public Coord mapPosition;

    public Village_Data(Coord pos, string _name, Coord _center)
    {
        mapPosition = pos;
        name = _name;
        center = _center;
    }
}
