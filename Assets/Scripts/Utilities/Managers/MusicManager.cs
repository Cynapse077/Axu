using UnityEngine;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public MusicZone[] musicZones;
    public AudioClip[] defaultMusic;
    public Dictionary<string, AudioClip> uniqueMusic;

    SoundManager soundManager;

    void Start()
    {
        soundManager = GetComponent<SoundManager>();
    }

    public void OverrideMusic(string musID)
    {
        if (!uniqueMusic.ContainsKey(musID))
        {
            soundManager.OverrideMusic(new AudioClip[] { uniqueMusic[musID] });
        }
        else
        {
            Debug.LogError("No music with ID " + musID);
        }
    }

    public void Init(TileMap_Data newMap)
    {
        World.tileMap.OnScreenChange += ChangeLocation;
        ChangeLocation(null, newMap);
    }

    void OnDisable()
    {
        if (World.tileMap != null)
        {
            World.tileMap.OnScreenChange -= ChangeLocation;
        }
    }

    bool ChangeLocation(TileMap_Data oldMap, TileMap_Data newMap)
    {
        if (!newMap.mapInfo.landmark.NullOrEmpty())
        {
            for (int i = 0; i < musicZones.Length; i++)
            {
                if (!newMap.mapInfo.landmark.NullOrEmpty() && musicZones[i].ZoneID == newMap.mapInfo.landmark)
                {
                    if (musicZones[i].underground && newMap.elevation != 0 || !musicZones[i].underground && newMap.elevation == 0)
                    {
                        soundManager.OverrideMusic(musicZones[i].songs);
                        return true;
                    }
                }
            }
        }

        //Biome
        if (newMap.elevation == 0 && newMap.mapInfo.biome != Biome.Default)
        {
            for (int i = 0; i < musicZones.Length; i++)
            {
                if (musicZones[i].biome == newMap.mapInfo.biome)
                {
                    soundManager.OverrideMusic(musicZones[i].songs);
                    return true;
                }
            }
        }

        //Fallback to generic music.
        soundManager.OverrideMusic(defaultMusic);

        return false;
    }
}

[System.Serializable]
public struct MusicZone
{
    public string ZoneID;
    public Biome biome;
    public bool underground;
    public AudioClip[] songs;
}