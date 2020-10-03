using UnityEngine;
using System.IO;
using System.Collections.Generic;
using LitJson;

public static class GameSettings
{
    public static Coord DefaultScreenSize;
    public static InputKeys Keybindings;
    public static double SE_Volume, Mus_Volume, Master_Volume, Animation_Speed;
    public static bool SimpleDamage, MuteAll;
    public static bool UseMouse, Fullscreen, Enable_Weather;
    public static bool Allow_Console, Particle_Effects;
    public static Coord ScreenSize;
    public static bool ShowLog;
    public static bool FourWayMovement;
    public static int RespawnTime;

    public static readonly List<Coord> SupportedResolutions = new List<Coord>()
    {
        new Coord(800, 600), new Coord(1024, 768), new Coord(1280, 720),
        new Coord(1366, 768), new Coord(1600, 900), new Coord(1920, 1080),
        new Coord(2560, 1440)
    };

    public static string version => "0.7.8";

    public static void InitializeFromFile()
    {
        Allow_Console = true;

        if (File.Exists(Manager.SettingsDirectory))
        {
            string jsonString = File.ReadAllText(Manager.SettingsDirectory);
            JsonData dat = JsonMapper.ToObject(jsonString);

            dat.TryGetCoord("ScreenSize", out ScreenSize, DefaultScreenSize);
            dat.TryGetDouble("Master_Volume", out Master_Volume, 1.0);
            dat.TryGetDouble("Music_Volume", out Mus_Volume, 1.0);
            dat.TryGetDouble("SFX_Volume", out SE_Volume, 1.0);
            dat.TryGetDouble("Animation_Speed", out Animation_Speed, 40.0);
            dat.TryGetBool("Mute", out MuteAll);
            dat.TryGetBool("Fullscreen", out Fullscreen);
            dat.TryGetBool("UseMouse", out UseMouse);
            dat.TryGetBool("Weather", out Enable_Weather, true);
            dat.TryGetBool("Particle_Effects", out Particle_Effects, true);
            dat.TryGetBool("SimpleDmg", out SimpleDamage);
            dat.TryGetBool("Show Log", out ShowLog, true);
            dat.TryGetBool("FourWayMovement", out FourWayMovement);

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
            UseMouse, Keybindings, Animation_Speed, Enable_Weather, Particle_Effects, SimpleDamage, ShowLog, FourWayMovement);

        JsonData data = JsonMapper.ToJson(settings);
        string prettyData = data.ToString();
        prettyData = JsonHelper.PrettyPrint(prettyData);
        File.WriteAllText(Manager.SettingsDirectory, prettyData);

        if (ScreenSize == null)
        {
            ScreenSize = new Coord(1280, 720);
        }

        Screen.SetResolution(ScreenSize.x, ScreenSize.y, Fullscreen);
    }

    public static void Defaults()
    {
        SE_Volume = 1.0;
        Mus_Volume = 1.0;
        Master_Volume = 1.0;

        MuteAll = false;
        Fullscreen = false;
        ScreenSize = DefaultScreenSize;
        UseMouse = false;
        Allow_Console = true;
        Animation_Speed = 40.0;
        Enable_Weather = true;
        Particle_Effects = true;
        SimpleDamage = false;
        ShowLog = true;
        FourWayMovement = false;

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
    public bool FourWayMovement { get; set; }

    public InputKeys Input;

    public SSettings() { }

    public SSettings(bool fullscreen, Coord scSize, double masvol, double musvol, double sfxvol, bool mute,
        bool mouse, InputKeys keys, double animspeed, bool wea, bool part, bool sdmg, bool log, bool fourWayMove)
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
        FourWayMovement = fourWayMove;
    }
}