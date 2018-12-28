using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public MusicZone[] musicZones;
    public AudioClip[] defaultMusic;

    SoundManager soundManager;

    void Start()
    {
        soundManager = GetComponent<SoundManager>();
    }

    public void Init(TileMap_Data newMap)
    {
        World.tileMap.OnScreenChange += ChangeLocation;

        for (int i = 0; i < musicZones.Length; i++)
        {
            if (musicZones[i].ZoneID == newMap.mapInfo.landmark)
            {
                if (musicZones[i].underground && newMap.elevation != 0 || !musicZones[i].underground && newMap.elevation == 0)
                {
                    soundManager.OverrideMusic(musicZones[i].songs);
                    return;
                }                
            }
        }

        soundManager.OverrideMusic(defaultMusic);
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
        bool changeInElevation = (oldMap.elevation != 0 && newMap.elevation == 0 || oldMap.elevation == 0 && newMap.elevation != 0);

        if (oldMap.mapInfo.landmark == newMap.mapInfo.landmark && !changeInElevation)
        {
            return false;
        }

        for (int i = 0; i < musicZones.Length; i++)
        {
            if (musicZones[i].ZoneID == newMap.mapInfo.landmark)
            {
                if (musicZones[i].underground && newMap.elevation != 0 || !musicZones[i].underground && newMap.elevation == 0)
                {
                    soundManager.OverrideMusic(musicZones[i].songs);
                    return true;
                }                
            }
        }

        for (int i = 0; i < musicZones.Length; i++)
        {
            if (musicZones[i].ZoneID == oldMap.mapInfo.landmark)
            {
                if (musicZones[i].underground && oldMap.elevation != 0 || !musicZones[i].underground && oldMap.elevation == 0)
                {
                    soundManager.OverrideMusic(defaultMusic);
                    return true;
                }
            }
        }

        return false;
    }
}

[System.Serializable]
public struct MusicZone
{
    public string ZoneID;
    public bool underground;
    public AudioClip[] songs;
}