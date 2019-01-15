using UnityEngine;
using System.IO;
using LitJson;

public static class GameSettings
{
    public static int defaultScreenX = 1280, defaultScreenY = 720;

    public static InputKeys Keybindings;
    public static double SE_Volume, Mus_Volume, Master_Volume, Animation_Speed;
    public static bool SimpleDamage, MuteAll;
    public static bool UseMouse, Fullscreen, Enable_Weather;
    public static bool Allow_Console, Particle_Effects;
    public static Coord ScreenSize;
    public static bool ShowLog;

    public static string version
    {
        get
        {
            return "0.7.3c";
        }
    }

    public static void InitializeFromFile()
    {
        Allow_Console = true;

        if (File.Exists(Manager.SettingsDirectory))
        {
            string jsonString = File.ReadAllText(Manager.SettingsDirectory);
            JsonData dat = JsonMapper.ToObject(jsonString);

            ScreenSize = dat.ContainsKey("ScreenSize") && dat["ScreenSize"].Count > 1 ? new Coord((int)dat["ScreenSize"][0], (int)dat["ScreenSize"][1]) : new Coord(defaultScreenX, defaultScreenY);
            Master_Volume = dat.ContainsKey("Master_Volume") ? (double)dat["Master_Volume"] : 1.0;
            Mus_Volume = dat.ContainsKey("Music_Volume") ? (double)dat["Music_Volume"] : 1.0;
            SE_Volume = dat.ContainsKey("SFX_Volume") ? (double)dat["SFX_Volume"] : 1.0;
            MuteAll = dat.ContainsKey("Mute") ? (bool)dat["Mute"] : false;
            Fullscreen = dat.ContainsKey("Fullscreen") ? (bool)dat["Fullscreen"] : false;
            UseMouse = dat.ContainsKey("UseMouse") ? (bool)dat["UseMouse"] : false;
            Enable_Weather = dat.ContainsKey("Weather") ? (bool)dat["Weather"] : true;
            Animation_Speed = (dat.ContainsKey("Animation_Speed")) ? (double)dat["Animation_Speed"] : 40.0;
            Particle_Effects = dat.ContainsKey("Particle_Effects") ? (bool)dat["Particle_Effects"] : true;
            SimpleDamage = dat.ContainsKey("SimpleDmg") ? (bool)dat["SimpleDmg"] : false;
            ShowLog = dat.ContainsKey("Show_Log") ? (bool)dat["Show_Log"] : true;

            if (dat.Keys.Contains("Input"))
            {
                Keybindings = new InputKeys(dat);
            }
            else
            {
                Keybindings = new InputKeys();

                if (ObjectManager.player != null)
                {
                    ObjectManager.player.GetComponent<PlayerInput>().NewKeybindingClass();
                }
            }
        }
        else
        {
            Defaults();
        }

        Save();
    }

    public static void Save()
    {
        SSettings settings = new SSettings(Fullscreen, ScreenSize, Master_Volume, Mus_Volume, SE_Volume, MuteAll,
            UseMouse, Keybindings, Animation_Speed, Enable_Weather, Particle_Effects, SimpleDamage, ShowLog);

        JsonData data = JsonMapper.ToJson(settings);
        string prettyData = data.ToString();
        prettyData = JsonHelper.PrettyPrint(prettyData);
        File.WriteAllText(Manager.SettingsDirectory, prettyData);

        Screen.SetResolution(ScreenSize.x, ScreenSize.y, Fullscreen);
    }

    public static void Defaults()
    {
        SE_Volume = 1.0;
        Mus_Volume = 1.0;
        Master_Volume = 1.0;

        MuteAll = false;
        Fullscreen = false;
        ScreenSize = new Coord(defaultScreenX, defaultScreenY);
        UseMouse = false;
        Allow_Console = true;
        Animation_Speed = 40.0;
        Enable_Weather = true;
        Particle_Effects = true;
        SimpleDamage = false;
        ShowLog = true;

        Keybindings = new InputKeys();

        if (ObjectManager.player != null)
        {
            ObjectManager.player.GetComponent<PlayerInput>().NewKeybindingClass();
        }
    }
}

[System.Serializable]
public class SSettings
{
    public double Master_Volume { get; set; }
    public double Music_Volume { get; set; }
    public double SFX_Volume { get; set; }
    public double Animation_Speed { get; set; }
    public bool Mute { get; set; }
    public bool Fullscreen { get; set; }
    public Coord ScreenSize { get; set; }
    public bool UseMouse { get; set; }
    public bool Weather { get; set; }
    public bool Particle_Effects { get; set; }
    public bool SimpleDmg { get; set; }
    public bool ShowLog { get; set; }

    public InputKeys Input;

    public SSettings() { }

    public SSettings(bool fullscreen, Coord scSize, double masvol, double musvol, double sfxvol, bool mute,
        bool mouse, InputKeys keys, double animspeed, bool wea, bool part, bool sdmg, bool log)
    {
        Fullscreen = fullscreen;
        ScreenSize = scSize;
        Master_Volume = masvol;
        Music_Volume = musvol;
        SFX_Volume = sfxvol;
        Mute = mute;
        UseMouse = mouse;
        Input = keys;
        Animation_Speed = animspeed;
        Weather = wea;
        Particle_Effects = part;
        SimpleDmg = sdmg;
        ShowLog = log;
    }
}