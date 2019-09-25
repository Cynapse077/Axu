using UnityEngine;
using System.Collections.Generic;
using LitJson;
using System.IO;
using System.Threading;
using Augments;

public class SaveData : MonoBehaviour
{
    JsonData playerJson;

    public void SaveNPCs(List<NPC> npcs)
    {
        List<NPCCharacter> chars = new List<NPCCharacter>();

        for (int i = 0; i < npcs.Count; i++)
        {
            if (npcs[i] == null || !npcs[i].isAlive)
            {
                continue;
            }

            NPC n = npcs[i];

            List<SItem> items = new List<SItem>();
            if (n.inventory != null && n.inventory.Count > 0)
            {
                for (int it = 0; it < n.inventory.Count; it++)
                {
                    items.Add(n.inventory[it].ToSerializedItem());
                }
            }

            List<SBodyPart> bodyParts = (n.IsFollower()) ? new List<SBodyPart>() : null;

            if (n.IsFollower())
            {
                for (int j = 0; j < n.bodyParts.Count; j++)
                {
                    bodyParts.Add(n.bodyParts[j].ToSerializedBodyPart());
                }
            }

            List<SItem> handItems = new List<SItem>();
            for (int j = 0; j < n.handItems.Count; j++)
            {
                handItems.Add(n.handItems[j].ToSerializedItem());
            }

            if (n.firearm == null)
            {
                n.firearm = ItemList.GetNone();
            }

            List<object[]> atr = new List<object[]>();
            foreach (KeyValuePair<string, int> s in n.Attributes)
            {
                atr.Add(new object[2] { s.Key, s.Value });
            }

            NPCCharacter character = new NPCCharacter(n.name, n.ID, n.UID, n.worldPosition, n.localPosition, n.elevation, items, handItems, n.firearm.ToSerializedItem(),
                n.isHostile, n.spriteID, n.faction.ID, n.flags, n.questID, n.dialogueID, bodyParts, n.traits, atr);

            chars.Add(character);
        }

        ThreadStart threadStart = delegate {
            CreateSave(chars, World.turnManager.turn);
        };

        threadStart.Invoke();
    }

    void CreateSave(List<NPCCharacter> chars, int turn)
    {
        new NewWorld(chars, World.turnManager.turn);
    }

    /// <summary>
    /// Loads the player.
    /// </summary>
    public void LoadPlayer(string charName)
    {
        Manager.playerBuilder = new PlayerBuilder();

        string jsonString = File.ReadAllText(Path.Combine(Manager.SaveDirectory, charName + ".axu"));
        playerJson = JsonMapper.ToObject(jsonString)["Player"];

        //Name
        Manager.worldSeed = (int)playerJson["WorldSeed"];
        Manager.playerName = playerJson["Name"].ToString();
        Manager.profName = playerJson["ProfName"].ToString();

        //Level
        Manager.playerBuilder.level = new XPLevel(
            null,
            (int)playerJson["xpLevel"]["CurrentLevel"],
            (int)playerJson["xpLevel"]["XP"],
            (int)playerJson["xpLevel"]["XPToNext"]
        );

        //Positions
        Manager.localStartPos.x = (int)playerJson["LP"][0];
        Manager.localStartPos.y = (int)playerJson["LP"][1];
        Manager.startElevation = (int)playerJson["WP"][2];

        //Health/Stamina
        Manager.playerBuilder.hp = Manager.playerBuilder.maxHP = (int)playerJson["Stats"]["HP"];
        Manager.playerBuilder.st = Manager.playerBuilder.maxST = (int)playerJson["Stats"]["ST"];

        //Attributes
        Manager.playerBuilder.attributes["Strength"] = (int)playerJson["Stats"]["STR"];
        Manager.playerBuilder.attributes["Dexterity"] = (int)playerJson["Stats"]["DEX"];
        Manager.playerBuilder.attributes["Intelligence"] = (int)playerJson["Stats"]["INT"];
        Manager.playerBuilder.attributes["Endurance"] = (int)playerJson["Stats"]["END"];
        Manager.playerBuilder.attributes["Defense"] = (int)playerJson["Stats"]["DEF"];
        Manager.playerBuilder.attributes["Speed"] = (int)playerJson["Stats"]["SPD"];
        Manager.playerBuilder.attributes["Stealth"] = (int)playerJson["Stats"]["STLH"];
        Manager.playerBuilder.attributes["Charisma"] = (int)playerJson["Charisma"];
        Manager.playerBuilder.attributes["Accuracy"] = (int)playerJson["Stats"]["ACC"];

        //Radiation
        Manager.playerBuilder.radiation = (int)playerJson["Stats"]["Rad"];

        //Status Effects
        Manager.playerBuilder.statusEffects = new Dictionary<string, int>();
        if (playerJson["Stats"].ContainsKey("SE") && playerJson["Stats"]["SE"].Count > 0)
        {
            for (int i = 0; i < playerJson["Stats"]["SE"].Count; i++)
            {
                Manager.playerBuilder.statusEffects.Add(playerJson["Stats"]["SE"][i]["A"].ToString(), (int)playerJson["Stats"]["SE"][i]["B"]);
            }
        }

        //Proficiencies
        int p = 0;
        Manager.playerBuilder.proficiencies = new PlayerProficiencies
        {
            //Weapon
            Blade = SetProf("Blade", p++),
            Blunt = SetProf("Blunt", p++),
            Polearm = SetProf("Polearm", p++),
            Axe = SetProf("Axe", p++),
            Firearm = SetProf("Firearm", p++),
            Throwing = SetProf("Thrown", p++),
            Unarmed = SetProf("Unarmed", p++),
            Misc = SetProf("Misc", p++),
            //other
            Armor = SetProf("Armor", p++),
            Shield = SetProf("Shield", p++),
            Butchery = SetProf("Butchery", p++),
            MartialArts = SetProf("Martial Arts", p++)
        };

        //Traits
        Manager.playerBuilder.traits = new List<Trait>();
        for (int i = 0; i < playerJson["Traits"].Count; i++)
        {
            string tName = playerJson["Traits"][i]["id"].ToString();
            Trait nTrait = TraitList.GetTraitByID(tName);
            nTrait.turnAcquired = (int)playerJson["Traits"][i]["tAc"];

            Manager.playerBuilder.traits.Add(nTrait);
        }

        //Addictions
        Manager.playerBuilder.addictions = new List<Addiction>();
        if (playerJson["Stats"].ContainsKey("Adcts"))
        {
            for (int i = 0; i < playerJson["Stats"]["Adcts"].Count; i++)
            {
                JsonData j = playerJson["Stats"]["Adcts"][i];
                string itemID = j["addictedID"].ToString();
                bool addicted = (bool)j["addicted"], withdrawal = (bool)j["withdrawal"];

                Addiction a = new Addiction(itemID, addicted, withdrawal, (int)j["lastTurnTaken"], (int)j["chanceToAddict"], (int)j["currUse"]);
                Manager.playerBuilder.addictions.Add(a);
            }
        }

        SetUpSkills();
        SetUpInventory();

        Manager.playerBuilder.money = (int)playerJson["Gold"];

        //Weather
        int weatherInt = (int)playerJson["CWeather"];
        Manager.startWeather = (Weather)weatherInt;
    }

    WeaponProficiency SetProf(string name, int id)
    {
        return new WeaponProficiency(name, (int)playerJson["WepProf"][id]["level"], (double)playerJson["WepProf"][id]["xp"]);
    }

    void SetUpInventory()
    {
        //items
        JsonData invJson = playerJson["Inv"];
        for (int i = 0; i < invJson.Count; i++)
        {
            Manager.playerBuilder.items.Add(GetItemFromJsonData(invJson[i]));
        }

        //body parts
        JsonData bpJson = playerJson["BodyParts"];
        Manager.playerBuilder.bodyParts = GetBodyPartsFromJson(bpJson);

        //Hand items
        for (int i = 0; i < playerJson["HIt"].Count; i++)
        {
            Manager.playerBuilder.handItems.Add(GetItemFromJsonData(playerJson["HIt"][i]));
        }

        //Firearm
        Manager.playerBuilder.firearm = GetItemFromJsonData(playerJson["F"]);
    }

    public static List<BodyPart> GetBodyPartsFromJson(JsonData dat)
    {
        List<BodyPart> parts = new List<BodyPart>();

        for (int i = 0; i < dat.Count; i++)
        {
            if (!dat[i].ContainsKey("Name") || !dat[i].ContainsKey("Att"))
            {
                continue;
            }

            BodyPart bp = new BodyPart(dat[i]["Name"].ToString(), (bool)dat[i]["Att"])
            {
                armor = (int)dat[i]["Ar"],
                Attributes = new List<Stat_Modifier>()
            };

            int slot = (int)dat[i]["Slot"];
            bp.slot = (ItemProperty)slot;

            if (dat[i].ContainsKey("Hnd"))
            {
                string baseItem = dat[i]["Hnd"]["bItem"].ToString();
                bp.hand = new BodyPart.Hand(bp, ItemList.GetItemByID(baseItem), baseItem);
            }

            if (dat[i].ContainsKey("Stats") && dat[i]["Stats"].Count > 0)
            {
                for (int j = 0; j < dat[i]["Stats"].Count; j++)
                {
                    bp.Attributes.Add(new Stat_Modifier(dat[i]["Stats"][j]["Stat"].ToString(), (int)dat[i]["Stats"][j]["Amount"]));
                }
            }

            //Cybernetic
            if (dat[i].ContainsKey("Cyb"))
            {
                string cybName = dat[i]["Cyb"].ToString();

                if (!cybName.NullOrEmpty())
                {
                    Cybernetic cyb = Cybernetic.GetCybernetic(cybName);

                    if (cyb != null)
                    {
                        bp.cybernetic = cyb;
                        cyb.bodyPart = bp;
                    }
                }
            }

            //body part levels/xp
            if (dat[i].ContainsKey("Lvl"))
            {
                bp.level = (int)dat[i]["Lvl"];
            }

            if (dat[i].ContainsKey("XP"))
            {
                bp.SetXP((double)dat[i]["XP"][0], (double)dat[i]["XP"][1]);
            }

            bp.flags = dat[i].ContainsKey("Flgs") ? (BodyPart.BPTags)(int)dat[i]["Flgs"] : BodyPart.BPTags.None;

            //Wounds
            bp.wounds = new List<Wound>();
            if (dat[i].ContainsKey("Wounds"))
            {
                for (int j = 0; j < dat[i]["Wounds"].Count; j++)
                {
                    JsonData wound = dat[i]["Wounds"][j];
                    ItemProperty woundSlot = wound["slot"].ToString().ToEnum<ItemProperty>();
                    List<DamageTypes> dts = new List<DamageTypes>();

                    for (int k = 0; k < wound["damTypes"].Count; k++)
                    {
                        dts.Add(wound["damTypes"][k].ToString().ToEnum<DamageTypes>());
                    }

                    Wound w = new Wound(wound["Name"].ToString(), wound["ID"].ToString(), woundSlot, dts);

                    for (int k = 0; k < wound["statMods"].Count; k++)
                    {
                        w.statMods.Add(new Stat_Modifier(wound["statMods"][k]["Stat"].ToString(), (int)wound["statMods"][k]["Amount"]));
                    }

                    bp.wounds.Add(w);
                }
            }

            bp.equippedItem = GetItemFromJsonData(dat[i]["item"]);
            parts.Add(bp);
        }

        return parts;
    }

    public void SetUpJournal()
    {
        for (int i = 0; i < playerJson["Quests"].Count; i++)
        {
            Manager.playerBuilder.quests.Add(GetQuest(playerJson["Quests"][i]));
        }

        Manager.playerBuilder.progressFlags = new List<string>();
        if (playerJson.ContainsKey("Flags"))
        {
            for (int i = 0; i < playerJson["Flags"].Count; i++)
            {
                Manager.playerBuilder.progressFlags.Add(playerJson["Flags"][i].ToString());
            }
        }

        if (playerJson.ContainsKey("CQeusts"))
        {
            for (int i = 0; i < playerJson["CQuests"].Count; i++)
            {
                Manager.playerBuilder.completedQuests.Add(playerJson["CQuests"][i].ToString());
            }
        }

        if (playerJson.ContainsKey("StaticNKills"))
        {
            for (int i = 0; i < playerJson["StaticNKills"].Count; i++)
            {
                Manager.playerBuilder.killedStaticNPCs.Add(playerJson["StaticNKills"][i].ToString());
            }
        }
    }

    Quest GetQuest(JsonData qData)
    {
        JsonReader reader = new JsonReader(qData.ToJson());
        SQuest sq = JsonMapper.ToObject<SQuest>(reader);

        return new Quest(sq);
    }

    void SetUpSkills()
    {
        List<Ability> abilities = new List<Ability>();

        for (int i = 0; i < playerJson["Skills"].Count; i++)
        {
            string sID = playerJson["Skills"][i]["Name"].ToString();
            Ability s = new Ability(GameData.Get<Ability>(sID));

            playerJson["Skills"][i].TryGetInt("Lvl", out s.level);
            s.XP = (double)playerJson["Skills"][i]["XP"];
            s.origin = playerJson["Skills"][i].ContainsKey("Flg") ? (Ability.AbilityOrigin)(int)playerJson["Skills"][i]["Flg"] : Ability.AbilityOrigin.None;
            playerJson.TryGetInt("CD", out s.cooldown);

            abilities.Add(s);
        }

        Manager.playerBuilder.abilities = abilities;
    }

    public static Item GetItemFromJsonData(JsonData data)
    {
        string iID = data["ID"].ToString();
        string mName = data["MID"].ToString();

        Item it = ItemList.GetItemByID(iID);

        it.amount = (int)data["Am"];

        if (data.ContainsKey("DName"))
        {
            it.displayName = data["DName"].ToString();
        }

        if (data.ContainsKey("Props"))
        {
            for (int i = 0; i < data["Props"].Count; i++)
            {
                int iPro = (int)data["Props"][i];
                it.AddProperty((ItemProperty)iPro);
            }
        }

        if (data.ContainsKey("Dmg"))
        {
            it.damage = new Damage((int)data["Dmg"][0], (int)data["Dmg"][1], (int)data["Dmg"][2], it.damage.Type);
        }

        if (data.ContainsKey("Ar"))
        {
            it.armor = (int)data["Ar"];
        }

        it.AddModifier(ItemList.GetModByID(mName));

        if (data.ContainsKey("Com"))
        {
            it.SetComponentList(GetComponentsFromData(data["Com"]));
        }

        it.statMods = new List<Stat_Modifier>();

        if (data.ContainsKey("Sm"))
        {
            for (int i = 0; i < data["Sm"].Count; i++)
            {
                Stat_Modifier sm = new Stat_Modifier(data["Sm"][i]["Stat"].ToString(), (int)data["Sm"][i]["Amount"]);
                it.statMods.Add(sm);
            }
        }

        return it;
    }

    static List<CComponent> GetComponentsFromData(JsonData data)
    {
        List<CComponent> comps = new List<CComponent>();

        for (int i = 0; i < data.Count; i++)
        {
            comps.Add(CComponent.FromJson(data[i]));
        }

        return comps;
    }

}
