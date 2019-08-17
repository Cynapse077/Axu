using UnityEngine;
using LitJson;
using System.IO;
using System.Collections.Generic;

public static class SkillList
{
    public static List<Skill> skills
    {
        get
        {
            return GameData.instance.GetAll<Skill>();
        }
    }

    public static Skill GetSkillByID(string id)
    {
        
        return new Skill(GameData.instance.Get<Skill>(id) as Skill);
    }
}
