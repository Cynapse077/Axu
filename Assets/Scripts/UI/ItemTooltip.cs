using System.Collections.Generic;

public static class ItemTooltip
{
    public static List<string> GetDisplayItems(Item item)
    {
        List<string> displayItems = new List<string>();

        if (item.HasProp(ItemProperty.Quest_Item))
        {
            displayItems.Add(GetContent("IT_Quest", true));
        }
        else
        {
            if (item.HasProp(ItemProperty.Edible) && item.HasProp(ItemProperty.Replacement_Limb))
                displayItems.Add(GetContent("IT_Edible_Replace", true));
            if (item.HasProp(ItemProperty.Edible) && !item.HasProp(ItemProperty.Replacement_Limb))
                displayItems.Add(GetContent("IT_Edible", true));
            if (item.HasProp(ItemProperty.Replacement_Limb) && !item.HasProp(ItemProperty.Edible))
                displayItems.Add(GetContent("IT_Replace", true));
            if (item.HasProp(ItemProperty.Legible))
                displayItems.Add(GetContent("IT_Legible", true));
            if (item.HasProp(ItemProperty.Ammunition))
                displayItems.Add(GetContent("IT_Ammo", true));
            if (Usable(item))
                displayItems.Add(GetContent("IT_Usable", true));
            if (item.GetCComponent<CLiquidContainer>() != null)
                displayItems.Add(GetContent("IT_Container", true));
        }

        if (item.HasCComponent<CRequirement>())
        {
            CRequirement cr = item.GetCComponent<CRequirement>();
            string req = "";

            for (int i = 0; i < cr.req.Count; i++)
            {
                if (cr.CanUse(ObjectManager.playerEntity.stats, i))
                {
                    req += "<color=green>";
                }
                else
                {
                    req += "<color=red>";
                }

                req += LocalizationManager.GetContent(cr.req[i].String) + " " + cr.req[i].Int;

                if (i < cr.req.Count - 1)
                {
                    req += ", ";
                }

                req += "</color>";
            }

            displayItems.Add(GetContent_Input("IT_Req", req));
        }


        if (item.HasProp(ItemProperty.Ranged) || item.HasCComponent<CFirearm>())
        {
            displayItems.Add(GetContent("IT_Firearm", true));
            displayItems.Add(GetContent_Input("IT_Type", WeaponType(item)));

            string damageString = GetContent_Input("IT_Damage", item.TotalDamage().ToString());
            displayItems.Add(damageString);

            CFirearm cf = item.GetCComponent<CFirearm>();
            displayItems.Add(GetContent_Input("IT_Firearm_Ammo", cf.curr.ToString(), cf.max.ToString()));
        }

        //Weapons
        if (item.HasProp(ItemProperty.Weapon))
        {
            if (item.HasProp(ItemProperty.Two_Handed))
                displayItems.Add(GetContent("IT_Weapon2", true));
            else if (item.itemType == Proficiencies.Misc_Object)
            {
                displayItems.Add(GetContent("IT_Wieldable", true));
            }
            else
            {
                if (item.itemType == Proficiencies.Shield)
                    displayItems.Add(GetContent("Shield", true));
                else
                    displayItems.Add(GetContent("IT_Weapon", true));
            }

            if (item.HasCComponent<CItemLevel>())
                displayItems.Add(item.GetCComponent<CItemLevel>().Display());

            displayItems.Add(GetContent_Input("IT_Type", WeaponType(item)));

            string damageString = GetContent_Input("IT_Damage", item.TotalDamage().ToString());
            if (item.ContainsDamageType(DamageTypes.Cold))
                damageString += "  " + GetContent("IT_Dmg_Cold");
            else if (item.ContainsDamageType(DamageTypes.Heat))
                damageString += "  " + GetContent("IT_Dmg_Heat");
            else if (item.ContainsDamageType(DamageTypes.Energy))
                damageString += "  " + GetContent("IT_Dmg_Energy");
            else if (item.ContainsDamageType(DamageTypes.Corruption))
                damageString += "  " + GetContent("IT_Dmg_Corrupt");
            else
            {
                damageString += "  " + GetContent("IT_Dmg_Phys");
            }

            displayItems.Add(damageString);

            //Attack speed
            displayItems.Add(GetContent_Input("IT_Speed", GetSpeed(item).ToString()));

            //Effects
            if (item.ContainsDamageType(DamageTypes.Cleave))
                displayItems.Add(GetContent("IT_Cleave"));
            if (item.ContainsDamageType(DamageTypes.Bleed))
                displayItems.Add(GetContent("IT_Bleed"));
            if (item.ContainsDamageType(DamageTypes.Venom))
                displayItems.Add(GetContent("IT_Poison"));
            if (item.HasProp(ItemProperty.Shock_Nearby))
                displayItems.Add(GetContent("IT_Shock"));
            if (item.HasProp(ItemProperty.DrainHealth))
                displayItems.Add(GetContent("IT_Drain"));
            if (item.HasProp(ItemProperty.Knockback))
                displayItems.Add(GetContent("IT_Knockback"));
            if (item.HasProp(ItemProperty.Confusion))
                displayItems.Add(GetContent("IT_Confuse"));
        }

        //Armor
        if (item.HasProp(ItemProperty.Armor))
        {
            displayItems.Add(GetContent("IT_Wearable", true));
            displayItems.Add(GetContent_Input("IT_Armor", (item.armor + item.modifier.armor).ToString()));
            string slot = item.GetSlot().ToString();
            slot = slot.Replace("Slot_", "");
            displayItems.Add(GetContent_Input("IT_Slot", slot));

            if (item.itemType == Proficiencies.Armor)
                displayItems.Add(GetContent_Input("IT_Type", LocalizationManager.GetContent("Armor")));
        }

        //Custom Components
        if (item.HasCComponent<CCoordinate>())
        {
            CCoordinate cc = item.GetCComponent<CCoordinate>();
            displayItems.Add(cc.GetInfo());
        }

        if (item.HasCComponent<CLiquidContainer>())
        {
            CLiquidContainer cl = item.GetCComponent<CLiquidContainer>();
            displayItems.Add(cl.GetInfo());
        }

        if (item.HasCComponent<CBlock>())
        {
            string s = LocalizationManager.GetContent("IT_Block");

            if (s.Contains("[INPUT]"))
                s = s.Replace("[INPUT]", (item.GetCComponent<CBlock>().level * 5).ToString());

            displayItems.Add(s);
        }

        if (item.HasCComponent<CModKit>())
        {
            ItemModifier m = ItemList.GetModByID(item.GetCComponent<CModKit>().modID);

            if (m != null)
            {
                displayItems.Add(m.name + ": " + m.description);
            }
        }

        if (item.HasCComponent<CEquipped>())
        {
            string eq = item.GetCComponent<CEquipped>().baseItemID;
            if (!string.IsNullOrEmpty(eq))
            {
                displayItems.Add("Equipped: " + ItemList.GetItemByID(eq).DisplayName());
            }
        }

        //Disarm - not working.
        /*if (item.HasProp(ItemProperty.Disarm))
			displayItems.Add(GetContent("IT_Disarm"));*/

        //ACC
        if (item.Accuracy != 0)
            displayItems.Add(GetContent_Input("IT_Acc", item.Accuracy.ToString()));

        //Ability
        if (item.GetCComponent<CAbility>() != null)
        {
            CAbility cab = item.GetCComponent<CAbility>();
            Ability skill = new Ability(GameData.Get<Ability>(cab.abID) as Ability);

            if (skill != null)
            {
                string abName = skill.Name;
                displayItems.Add(GetContent_Input("IT_Ability", abName));
            }
        }

        if (item.HasCComponent<CCoat>())
        {
            CCoat cc = item.GetCComponent<CCoat>();
            displayItems.Add(GetContent_Input("IT_Coat", cc.liquid.Name, cc.strikes.ToString()));
        }

        if (item.HasProp(ItemProperty.Poison))
            displayItems.Add(GetContent("IT_OnConsume_Poison"));
        if (item.HasProp(ItemProperty.Cure_Radiation))
            displayItems.Add(GetContent("IT_OnConsume_CureRad"));
        if (item.HasProp(ItemProperty.Stop_Bleeding))
            displayItems.Add(GetContent("IT_OnUse_Bandage"));
        if (item.HasProp(ItemProperty.Surface_Tele))
            displayItems.Add(GetContent("IT_OnUse_SurfTel"));
        if (item.HasProp(ItemProperty.Addictive) && World.difficulty.Level == Difficulty.DiffLevel.Hunted)
            displayItems.Add(GetContent("IT_Addictive"));


        if (item.statMods.Count > 0)
        {
            foreach (Stat_Modifier mod in item.statMods)
            {
                if (mod.Stat == "Hunger" && mod.Amount > 0)
                    displayItems.Add(GetContent("IT_Satiates"));
                else if (mod.Stat == "Stamina")
                    displayItems.Add(GetContent("IT_Restore"));
                else if (mod.Stat == "Health")
                    continue;
                else if (mod.Stat == "Light")
                    displayItems.Add(GetContent("IT_Light"));
                else if (mod.Stat == "Attack Delay")
                {
                    string stat = LocalizationManager.GetContent(mod.Stat);

                    if (mod.Amount > 0)
                        displayItems.Add("<color=red>" + stat + " + " + mod.Amount + "</color>");
                    else
                        displayItems.Add("<color=green>" + stat + " - " + (-mod.Amount).ToString() + "</color>");
                }
                else if (mod.Stat == "Haste")
                {
                    string haste = LocalizationManager.GetContent("Haste");
                    haste = haste.Replace("[INPUT]", mod.Amount.ToString());
                    displayItems.Add(haste);
                }
                else
                {
                    string stat = LocalizationManager.GetContent(mod.Stat);
                    if (item.HasProp(ItemProperty.Severed_BodyPart) || item.HasProp(ItemProperty.Replacement_Limb))
                    {
                        displayItems.Add("<color=silver>" + stat + " (" + mod.Amount.ToString() + ")</color>");
                    }
                    else
                    {
                        if (mod.Amount > 0)
                            displayItems.Add("<color=green>" + stat + " + " + mod.Amount + "</color>");
                        else
                            displayItems.Add("<color=red>" + stat + " - " + (-mod.Amount).ToString() + "</color>");
                    }
                }
            }
        }

        //Charges
        if (item.HasCComponent<CRot>())
        {
            CRot cr = item.GetCComponent<CRot>();
            displayItems.Add(GetContent_Input("IT_Spoils", cr.current.ToString()));
        }
        else if (!item.HasProp(ItemProperty.Ranged) && item.HasCComponent<CCharges>())
        {
            CCharges cc = item.GetCComponent<CCharges>();
            displayItems.Add(GetContent_Input("IT_Uses", cc.current.ToString(), cc.max.ToString()));
        }

        //Explosive
        if (item.HasProp(ItemProperty.Explosive))
            displayItems.Add(GetContent("IT_Explode"));

        if (item.HasProp(ItemProperty.Dig))
            displayItems.Add(GetContent("IT_Dig"));

        if (item.HasProp(ItemProperty.Randart))
            displayItems.Add(GetContent("IT_Randart"));

        displayItems.Add("   ");

        return displayItems;
    }

    static string GetContent(string key, bool itemType = false)
    {
        if (itemType)
        {
            string s = LocalizationManager.GetContent(key);
            return "<color=silver>=" + s + "=</color>";
        }

        return LocalizationManager.GetContent(key);
    }

    static string GetContent_Input(string key, string input1, string input2 = "")
    {
        string content = LocalizationManager.GetContent(key);

        if (!string.IsNullOrEmpty(input1))
        {
            if (content.Contains("[INPUT]"))
                content = content.Replace("[INPUT]", input1);
            else if (content.Contains("[INPUT1]"))
                content = content.Replace("[INPUT1]", input1);
        }

        if (!string.IsNullOrEmpty(input2) && content.Contains("[INPUT2]"))
            content = content.Replace("[INPUT2]", input2);

        return content;
    }

    static bool Usable(Item item)
    {
        return (!item.HasProp(ItemProperty.Armor) && !item.HasProp(ItemProperty.Weapon) && !item.HasProp(ItemProperty.Replacement_Limb)
            && !item.HasProp(ItemProperty.Edible) && !item.HasProp(ItemProperty.Legible)
            && !item.HasProp(ItemProperty.Ranged) && !item.HasProp(ItemProperty.Ammunition) && !item.HasCComponent<CLiquidContainer>());
    }

    static string WeaponType(Item item)
    {
        string itemType = "Misc";

        switch (item.itemType)
        {
            case Proficiencies.Unarmed:
                itemType = "Unarmed";
                break;
            case Proficiencies.Blade:
                itemType = "Blade";
                break;
            case Proficiencies.Axe:
                itemType = "Axe";
                break;
            case Proficiencies.Blunt:
                itemType = "Blunt";
                break;
            case Proficiencies.Polearm:
                itemType = "Polearm";
                break;
            case Proficiencies.Firearm:
                itemType = "Firearm";
                break;
            case Proficiencies.Shield:
                itemType = "Shield";
                break;
            default:
                itemType = "Misc";
                break;
        }

        return LocalizationManager.GetContent(itemType);
    }

    static string GetSpeed(Item item)
    {
        if (item.HasProp(ItemProperty.Very_Slow))
            return LocalizationManager.GetContent("IT_Speed5");

        else if (item.HasProp(ItemProperty.Slow))
            return LocalizationManager.GetContent("IT_Speed4");

        else if (item.HasProp(ItemProperty.Quick))
            return LocalizationManager.GetContent("IT_Speed2");

        else if (item.HasProp(ItemProperty.Very_Quick))
            return LocalizationManager.GetContent("IT_Speed1");


        return LocalizationManager.GetContent("IT_Speed3");
    }
}
