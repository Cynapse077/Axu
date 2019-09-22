using System.Collections.Generic;

public struct ItemActions
{
    public List<ItemAction> Actions;

    bool CanAdd(Item item, string action)
    {
        return (item.HasCComponent<CConsole>() && item.GetCComponent<CConsole>().action == action ||
            item.HasCComponent<CLuaEvent>() && item.GetCComponent<CLuaEvent>().evName == action);
    }

    public ItemActions(Item item)
    {
        Actions = new List<ItemAction>();

        if (CanAdd(item, "OnAttach"))
        {
            Actions.Add(new ItemAction("Attach", "Action_Attach"));
        }

        if (item.HasProp(ItemProperty.Armor) || item.HasProp(ItemProperty.Ranged) || CanAdd(item, "OnEquip"))
        {
            Actions.Add(new ItemAction("Equip", "Action_Equip"));
        }

        if (item.HasProp(ItemProperty.Throwing_Wep) || CanAdd(item, "OnReady"))
        {
            Actions.Add(new ItemAction("Ready", "Action_Ready"));
        }

        if (item.HasProp(ItemProperty.Weapon) && !item.HasProp(ItemProperty.Throwing_Wep) || CanAdd(item, "OnWield"))
        {
            Actions.Add(new ItemAction("Wield", "Action_Wield"));
        }

        if (item.HasProp(ItemProperty.Ranged) || CanAdd(item, "OnReload"))
        {
            Actions.Add(new ItemAction("Reload", "Action_Reload"));
        }

        if (ObjectManager.playerJournal.HasFlag(ProgressFlags.Learned_Butcher) && item.HasProp(ItemProperty.Corpse) || CanAdd(item, "OnButcher"))
        {
            Actions.Add(new ItemAction("Butcher", "Action_Butcher"));
        }

        if (item.HasProp(ItemProperty.Edible) || CanAdd(item, "OnEat"))
        {
            Actions.Add(new ItemAction("Eat", "Action_Eat"));
        }

        if (item.HasCComponent<CLiquidContainer>() && item.GetCComponent<CLiquidContainer>().currentAmount() > 0)
        {
            Actions.Add(new ItemAction("Drink", "Action_Drink"));
            Actions.Add(new ItemAction("Mix", "Action_Mix"));
            Actions.Add(new ItemAction("Pour", "Action_Pour"));
        }

        if (item.HasProp(ItemProperty.Stop_Bleeding) || item.HasProp(ItemProperty.Reveal_Map) ||
            item.HasProp(ItemProperty.Blink) || item.HasProp(ItemProperty.Surface_Tele) ||
            item.HasProp(ItemProperty.ReplaceLimb) || CanAdd(item, "OnUse") 
            || item.HasCComponent<CModKit>() || item.HasCComponent<CLocationMap>())
        {
            Actions.Add(new ItemAction("Use", "Action_Use"));
        }

        if (item.HasCComponent<CCoordinate>())
        {
            CCoordinate ccord = item.GetCComponent<CCoordinate>();
            Actions.Add(((!ccord.isSet) ? (new ItemAction("Set", "Action_Set")) : (new ItemAction("Use", "Action_Use"))));
        }

        if (item.HasProp(ItemProperty.Legible) || CanAdd(item, "Read"))
        {
            Actions.Add(new ItemAction("Read", "Action_Read"));
        }

        Actions.Add(new ItemAction("Throw", "Action_Throw"));
        Actions.Add(new ItemAction("Drop", "Action_Drop"));

        if (item.amount > 1 && item.stackable)
        {
            Actions.Add(new ItemAction("Drop All", "Action_DropAll"));
        }

        if (item.HasProp(ItemProperty.Ranged))
        {
            Actions.Add(new ItemAction("Unload", "Action_Unload"));
        }

        if (!item.HasProp(ItemProperty.Weapon) || item.HasProp(ItemProperty.Throwing_Wep))
        {
            Actions.Add(new ItemAction("Wield", "Action_Wield"));
        }
    }

    public struct ItemAction
    {
        public string Key;
        public string Display;

        public ItemAction(string k, string dis)
        {
            Key = k;
            Display = dis;
        }
    }
}
