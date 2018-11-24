using UnityEngine;
using System.IO;
using LitJson;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class EntityList
{
    public static List<NPC_Blueprint> npcs;
    public static string dataPath;

    static List<BodyPart> humanoidStructure;
    static JsonData bodyPartJson;

    static TileMap tileMap
    {
        get { return World.tileMap; }
    }

    public static void FillListFromData()
    {
        npcs = GrabBlueprintsFromData(Application.streamingAssetsPath + dataPath);

        TraitList.FillTraitsFromData();
        NPCGroupList.Init();
    }

    public static void RemoveNPC(string id)
    {
        if (npcs.Find(x => x.id == id) != null)
            npcs.Remove(npcs.Find(x => x.id == id));
    }

    public static List<NPC_Blueprint> GrabBlueprintsFromData(string fileName)
    {
        List<NPC_Blueprint> list = new List<NPC_Blueprint>();
        string listFromJson = File.ReadAllText(fileName);

        if (string.IsNullOrEmpty(listFromJson))
        {
            Debug.LogError("Entity Blueprints null.");
            return list;
        }

        JsonData data = JsonMapper.ToObject(listFromJson);
        humanoidStructure = GetBodyStructure("Humanoid");

        for (int i = 0; i < data["NPCs"].Count; i++)
        {
            list.Add(new NPC_Blueprint(data["NPCs"][i]));
        }

        return list;
    }

    public static NPC_Blueprint GetBlueprintByID(string search)
    {
        return (npcs.Find(x => x.id == search));
    }

    public static NPC GetNPCByID(string id, Coord worldPos, Coord localPos, int elevation = 0)
    {
        if (elevation == 0)
            elevation = tileMap.currentElevation;

        for (int i = 0; i < npcs.Count; i++)
        {
            if (npcs[i].id == id)
                return new NPC(npcs[i], worldPos, localPos, elevation);
                
        }

        Debug.LogError("No NPC with the ID of '" + id + "'.");
        return null;
    }

    public static List<BodyPart> DefaultBodyStructure()
    {
        List<BodyPart> bps = new List<BodyPart>();

        foreach (BodyPart bp in humanoidStructure)
        {
            bps.Add(new BodyPart(bp));
        }

        return bps;
    }

    public static string bodyDataPath;

    public static List<BodyPart> GetBodyStructure(string search)
    {
        List<BodyPart> parts = new List<BodyPart>();

        string file = File.ReadAllText(Application.streamingAssetsPath + bodyDataPath);
        bodyPartJson = JsonMapper.ToObject(file);

        for (int a = 0; a < bodyPartJson["Body Structures"].Count; a++)
        {
            if (bodyPartJson["Body Structures"][a]["Name"].ToString() == search)
            {
                for (int b = 0; b < bodyPartJson["Body Structures"][a]["Parts"].Count; b++)
                {
                    string bpName = bodyPartJson["Body Structures"][a]["Parts"][b].ToString();
                    BodyPart bpart = GetBodyPart(bpName);
                    parts.Add(bpart);
                }

                return parts;
            }
        }

        return parts;
    }

    public static BodyPart GetBodyPart(string bodyPartID)
    {
        for (int i = 0; i < bodyPartJson["Body Parts"].Count; i++)
        {
            JsonData bpData = bodyPartJson["Body Parts"][i];

            if (bpData["ID"].ToString() == bodyPartID)
            {
                string bpName = bpData["Name"].ToString();
                string slotName = bpData["Slot"].ToString();
                ItemProperty slot = slotName.ToEnum<ItemProperty>();

                bool severable = (bool)bpData["Severable"];
                bool wearGear = (bool)bpData["Can Wear Gear"];

                BodyPart bodyPart = new BodyPart(bpName, severable, slot)
                {
                    canWearGear = wearGear,
                    Weight = (int)bpData["Size"],
                    organic = true
                };

                if (bpData.ContainsKey("Stats"))
                {
                    bodyPart.Attributes = new List<Stat_Modifier>();
                    for (int j = 0; j < bpData["Stats"].Count; j++)
                    {
                        bodyPart.Attributes.Add(new Stat_Modifier(bpData["Stats"][j]["Stat"].ToString(), (int)bpData["Stats"][j]["Amount"]));
                    }
                }

                if (bpData.ContainsKey("Tags"))
                {
                    bodyPart.bpTags = new BodyPart.BPTags[bpData["Tags"].Count];

                    for (int j = 0; j < bpData["Tags"].Count; j++)
                    {
                        string txt = bpData["Tags"][j].ToString();

                        if (txt == "Synthetic")
                            bodyPart.organic = false;
                        else if (txt == "External")
                            bodyPart.external = true;
                        else
                        {
                            BodyPart.BPTags tag = txt.ToEnum<BodyPart.BPTags>();
                            bodyPart.bpTags[j] = tag;

                            if (tag == BodyPart.BPTags.Grip && bodyPart.slot == ItemProperty.Slot_Arm)
                            {
                                bodyPart.hand = new BodyPart.Hand(bodyPart, ItemList.GetItemByID("fists"));
                            }
                        }
                    }
                }

                if (bpData.ContainsKey("Default Equipped"))
                    bodyPart.equippedItem = ItemList.GetItemByID(bpData["Default Equipped"].ToString());

                return bodyPart;
            }
        }

        Debug.LogError("Could not find body part: " + bodyPartID);
        return null;
    }

    public static BodyPart GetRandomExtremety()
    {
        List<BodyPart> parts = new List<BodyPart>() {
            GetBodyPart("head"), GetBodyPart("arm"), GetBodyPart("leg"), GetBodyPart("leg"), GetBodyPart("tail"), GetBodyPart("wing")
        };

        BodyPart bodyPart = parts.GetRandom();

        return bodyPart;
    }
}