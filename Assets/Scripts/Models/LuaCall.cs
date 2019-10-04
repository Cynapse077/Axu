using UnityEngine;
using MoonSharp.Interpreter;

[MoonSharpUserData]
[System.Serializable]
public class LuaCall
{
    public string modID;
    public string scriptName;
    public string functionName;
    public string variable;

    public LuaCall() { }

    public LuaCall(string script)
    {
        FromString(script);
    }

    void FromString(string script)
    {
        //Store and trim variable
        int indexOfVar = script.IndexOf(':');
        if (indexOfVar >= 0 && indexOfVar < script.Length - 1)
        {
            variable = script.Substring(indexOfVar + 1);
            script = script.Remove(indexOfVar);
        }

        string[] s = script.Split('.');

        if (s.Length < 2)
        {
            Debug.Log("Script " + script + " cannot be parsed.");
            return;
        }

        modID = s[0];
        scriptName = s[1];
        functionName = s[2];
    }

    public void Call(params object[] parameters)
    {
        LuaManager.CallScriptFunction(this, parameters);
    }
}
