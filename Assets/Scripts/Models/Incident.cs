using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

public class Incident : IAsset, IWeighted
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public int Weight 
    { 
        get
        {
            return weight;
        }
        set
        {
            weight = value;
        }
    }

    public string enterDialogue;
    public string factionID;
    public string requiredFlag;
    public string nullifyingFlag;
    public string flagToAdd;
    public bool hostile;
    public Coord playerStartPos;
    public List<string> groupIDs;
    public List<string> npcIDs;
    public List<string> objectIDs;
    public List<string> maps;
    public int weight;
    public Difficulty.DiffLevel minDifficulty = Difficulty.DiffLevel.Adventurer;

    public Incident(JsonData dat)
    {
        FromJson(dat);
    }

    public Incident(Incident other)
    {
        enterDialogue = other.enterDialogue;
        groupIDs = other.groupIDs;
        npcIDs = other.npcIDs;
        objectIDs = other.objectIDs;
        weight = other.weight;
        minDifficulty = other.minDifficulty;
    }

    public void FromJson(JsonData dat)
    {
        ID = dat["ID"].ToString();
        enterDialogue = dat["Enter Dialogue"].ToString();

        if (dat.ContainsKey("Faction"))
        {
            factionID = dat["Faction"].ToString();
        }

        hostile = (bool)dat["Hostile"];

        if (dat.ContainsKey("Weight"))
        {
            weight = (int)dat["Weight"];
        }

        if (dat.ContainsKey("Min Difficulty"))
        {
            string val = dat["Min Difficulty"].ToString();
            minDifficulty = val.ToEnum<Difficulty.DiffLevel>();
        }

        if (dat.ContainsKey("Required Flag"))
        {
            requiredFlag = dat["Required Flag"].ToString();
        }

        if (dat.ContainsKey("Nullifying Flag"))
        {
            nullifyingFlag = dat["Nullifying Flag"].ToString();
        }

        if (dat.ContainsKey("Flag To Add"))
        {
            flagToAdd = dat["Flag To Add"].ToString();
        }

        if (dat.ContainsKey("Groups"))
        {
            groupIDs = dat["Groups"].ToStringList();
        }

        if (dat.ContainsKey("NPCs"))
        {
            npcIDs = dat["NPCs"].ToStringList();
        }

        if (dat.ContainsKey("Objects"))
        {
            objectIDs = dat["Objects"].ToStringList();
        }

        if (dat.ContainsKey("Maps"))
        {
            maps = dat["Maps"].ToStringList();
        }

        if (dat.ContainsKey("Player Start Pos"))
        {
            playerStartPos = new Coord((int)dat["Player Start Pos"][0], (int)dat["Player Start Pos"][1]);
        }
    }

    public bool CanSpawn()
    {
        if (!nullifyingFlag.NullOrEmpty() && ObjectManager.playerJournal.HasFlag(nullifyingFlag))
        {
            return false;
        }

        if (!requiredFlag.NullOrEmpty() && !ObjectManager.playerJournal.HasFlag(requiredFlag))
        {
            return false;
        }

        if (minDifficulty > World.difficulty.Level)
        {
            return false;
        }

        if (!groupIDs.NullOrEmpty())
        {
            for (int i = 0; i < groupIDs.Count; i++)
            {
                NPCGroup_Blueprint bp = GameData.Get<NPCGroup_Blueprint>(groupIDs[i]);

                if (bp != null && !bp.CanSpawn())
                {
                    return false;
                }
            }
        }

        if (!factionID.NullOrEmpty())
        {
            if (hostile && ObjectManager.playerEntity.inventory.DisguisedAs(GameData.Get<Faction>(factionID)))
            {
                return false;
            }
        }

        return true;
    }

    public void Spawn()
    {
        if (!maps.NullOrEmpty())
        {
            World.tileMap.LoadMap(maps.GetRandom());
        }

        ObjectManager.playerEntity.ForcePosition(playerStartPos ?? World.tileMap.CurrentMap.GetRandomFloorTile());

        if (!groupIDs.NullOrEmpty())
        {
            for (int i = 0; i < groupIDs.Count; i++)
            {
                SpawnController.SpawnFromGroupName(groupIDs[i]);
            }
        }

        if (!npcIDs.NullOrEmpty())
        {
            for (int i = 0; i < npcIDs.Count; i++)
            {
                NPC_Blueprint bp = GameData.Get<NPC_Blueprint>(npcIDs[i]);

                if (bp != null)
                {
                    Coord c = World.tileMap.GetRandomPosition();
                    NPC npc = new NPC(bp, World.tileMap.WorldPosition, c, World.tileMap.currentElevation);
                    World.objectManager.SpawnNPC(npc);
                }
            }
        }

        if (!objectIDs.NullOrEmpty())
        {
            for (int i = 0; i < objectIDs.Count; i++)
            {
                MapObject_Blueprint bp = GameData.Get<MapObject_Blueprint>(objectIDs[i]);

                if (bp != null)
                {
                    Coord c = World.tileMap.GetRandomPosition();
                    MapObject m = new MapObject(bp, c, World.tileMap.WorldPosition, World.tileMap.currentElevation);
                    World.objectManager.SpawnObject(m);
                }
            }
        }

        if (!flagToAdd.NullOrEmpty())
        {
            ObjectManager.playerJournal.AddFlag(flagToAdd);
        }

        Alert.CustomAlert(enterDialogue);
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }
}
