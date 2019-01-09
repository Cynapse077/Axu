using System.Collections.Generic;
using LitJson;

public class Cybernetic
{
    public string Name { get; private set; }
    public string ID { get; private set; }

    public Dictionary<string, LuaCall> luaEvents;
    public List<Stat_Modifier> stats;

    public Cybernetic() { }
    public Cybernetic(JsonData dat)
    {
        luaEvents = new Dictionary<string, LuaCall>();
        stats = new List<Stat_Modifier>();

        FromJson(dat);
    }

    void FromJson(JsonData dat)
    {

    }
}
