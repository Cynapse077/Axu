using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cybernetic
{
    public string Name { get; private set; }
    public string ID { get; private set; }

    public Dictionary<string, LuaCall> luaEvents;
}
