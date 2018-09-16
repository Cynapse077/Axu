
public class Village_Data
{
    public string name;
    public Coord center;

    Coord mapPosition;

    public Coord MapPosition
    {
        get { return mapPosition; }
        set { mapPosition = value; }
    }

public Village_Data(Coord pos, string _name, Coord _center)
    {
        MapPosition = pos;
        name = _name;
        center = _center;
    }
}
