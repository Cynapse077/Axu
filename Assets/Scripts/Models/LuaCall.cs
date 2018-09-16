using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class LuaCall {
	public string scriptName { get; protected set; }
	public string functionName { get; protected set; }
	public string variable { get; protected set; }

	public LuaCall(string sc, string fn, string v = null) {
		this.scriptName = sc;
		this.functionName = fn;
		variable = v;
	}
}
