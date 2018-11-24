using System.IO;
using UnityEngine;
using LitJson;

public static class ModManager
{
    static readonly string modDirectory = Application.streamingAssetsPath + "/Data/Mods";

    public static void LoadJson()
    {
        if (!Directory.Exists(modDirectory))
            Directory.CreateDirectory(modDirectory);

        string[] files = Directory.GetFiles(modDirectory, "*.json", SearchOption.AllDirectories);

        foreach (string s in files)
        {
            //string jstring = File.ReadAllText(s);
            //string fileName = Path.GetFileNameWithoutExtension(jstring);

            //JsonData data = JsonMapper.ToObject(jstring);
        }
    }

    public static void LoadLua()
    {
        if (!Directory.Exists(modDirectory))
            Directory.CreateDirectory(modDirectory);

        string pathToLua = modDirectory;
        string[] files = Directory.GetFiles(pathToLua, "*.lua", SearchOption.AllDirectories);

        foreach (string s in files)
        {
            LuaManager.AddFile(pathToLua, s);
        }
    }
}
