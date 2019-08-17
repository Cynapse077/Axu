using UnityEngine;
using LitJson;
using System.IO;
using System.Collections.Generic;

public static class NPCGroupList
{
    public static List<GroupBlueprint> groupBlueprints
    {
        get
        {
            return GameData.instance.GetAll<GroupBlueprint>();
        }
    }

    public static GroupBlueprint GetGroupByName(string search)
    {
        return GameData.instance.Get<GroupBlueprint>(search) as GroupBlueprint;
    }
}
