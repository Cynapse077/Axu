using UnityEngine;
using System.IO;
using LitJson;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class EntityList
{
    public static List<NPC_Blueprint> npcs { get { return GameData.GetAll<NPC_Blueprint>(); } }
    static string bodyDataPath { get { return Application.streamingAssetsPath + "/Mods/Core/Entities/BodyStructures.json"; } }
    static List<BodyPart> humanoidStructure;
    static JsonData bodyPartJson;

    static TileMap tileMap
    {
        get { return World.tileMap; }
    }

    public static void FillListFromData()
    {
        humanoidStructure = GetBodyStructure("Humanoid");
    }

    public static NPC GetNPCByID(string id, Coord worldPos, Coord localPos, int elevation = 0)
    {
        if (elevation == 0)
            elevation = tileMap.currentElevation;

        for (int i = 0; i < npcs.Count; i++)
        {
            if (npcs[i].ID == id)
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

    public static List<BodyPart> GetBodyStructure(string search)
    {
        List<BodyPart> parts = new List<BodyPart>();

        if (bodyPartJson == null)
        {
            string file = File.ReadAllText(bodyDataPath);
            bodyPartJson = JsonMapper.ToObject(file);
        }

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
        if (bodyPartJson == null)
        {
            string file = File.ReadAllText(bodyDataPath);
            bodyPartJson = JsonMapper.ToObject(file);
        }

        for (int i = 0; i < bodyPartJson["Body Parts"].Count; i++)
        {
            JsonData bpData = bodyPartJson["Body Parts"][i];

            if (bpData["ID"].ToString() == bodyPartID)
            {
                string bpName = bpData["Name"].ToString();
                string slotName = bpData["Slot"].ToString();
                ItemProperty slot = slotName.ToEnum<ItemProperty>();

                BodyPart bodyPart = new BodyPart(bpName, slot)
                {
                    Weight = (int)bpData["Size"]
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
                    for (int j = 0; j < bpData["Tags"].Count; j++)
                    {
                        string txt = bpData["Tags"][j].ToString();

                        BodyPart.BPTags tag = txt.ToEnum<BodyPart.BPTags>();
                        bodyPart.flags.Set(tag);
                    }
                }

                if (bodyPart.slot == ItemProperty.Slot_Arm)
                {
                    string baseWep = "fists";

                    if (bpData.ContainsKey("Wielding"))
                    {
                        baseWep = bpData["Wielding"].ToString();
                    }

                    bodyPart.hand = new BodyPart.Hand(bodyPart, ItemList.GetItemByID(baseWep), baseWep);
                }

                bodyPart.equippedItem = bpData.ContainsKey("Default Equipped") ? ItemList.GetItemByID(bpData["Default Equipped"].ToString()) : ItemList.NoneItem;
                return bodyPart;
            }
        }

        return null;
    }

    public static BodyPart GetRandomExtremety()
    {
        List<BodyPart> parts = new List<BodyPart>() {
            GetBodyPart("head"),
            GetBodyPart("arm"),
            GetBodyPart("leg"),
            GetBodyPart("leg"),
            GetBodyPart("tail"),
            GetBodyPart("wing")
        };

        BodyPart bodyPart = parts.GetRandom();

        return bodyPart;
    }
}