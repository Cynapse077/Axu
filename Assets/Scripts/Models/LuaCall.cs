using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class LuaCall
{
    public string scriptName { get; private set; }
    public string functionName { get; private set; }
    public string variable { get; private set; }

    public LuaCall(string sc, string fn, string v = null)
    {
        scriptName = sc;
        functionName = fn;
        variable = v;
    }
}
