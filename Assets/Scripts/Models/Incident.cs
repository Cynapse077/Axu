using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

public class Incident : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }

    public string enterDialogue;
    public string factionID;
    public bool hostile;
    public List<string> groupIDs;
    public List<string> npcIDs;
    public List<string> objectIDs;
    public double weight = 1.0;
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
            weight = (double)dat["Weight"];
        }

        if (dat.ContainsKey("Min Difficulty"))
        {
            string val = dat["Min Difficulty"].ToString();
            minDifficulty = val.ToEnum<Difficulty.DiffLevel>();
        }

        if (dat.ContainsKey("Groups"))
        {
            groupIDs = new List<string>();

            for (int i = 0; i < dat["Groups"].Count; i++)
            {
                groupIDs.Add(dat["Groups"][i].ToString());
            }
        }

        if (dat.ContainsKey("NPCs"))
        {
            npcIDs = new List<string>();

            for (int i = 0; i < dat["NPCs"].Count; i++)
            {
                npcIDs.Add(dat["NPCs"][i].ToString());
            }
        }

        if (dat.ContainsKey("Objects"))
        {
            objectIDs = new List<string>();

            for (int i = 0; i < dat["Objects"].Count; i++)
            {
                objectIDs.Add(dat["Objects"][i].ToString());
            }
        }
    }

    public bool CanSpawn()
    {
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
        Alert.CustomAlert(enterDialogue);

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
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }
}
