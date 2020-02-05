using System.Collections.Generic;
using LitJson;

public class EquipmentSet : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }

    public List<EquipmentSelection> choices;

    public EquipmentSet(EquipmentSet other)
    {
        ID = other.ID;
        ModID = other.ModID;
        choices = other.choices;
    }

    public EquipmentSet(JsonData dat)
    {
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        ID = dat["ID"].ToString();
        choices = new List<EquipmentSelection>();

        for (int i = 0; i < dat["Set"].Count; i++)
        {
            var d = dat["Set"][i];

            if (d.ContainsKey("Items"))
            {
                ItemProperty slot = d["Slot"].ToString().ToEnum<ItemProperty>();
                List<WeightedItem> items = new List<WeightedItem>();

                for (int j = 0; j < d["Items"].Count; j++)
                {
                    d["Items"][j].TryGetString("Item", out string id, ItemList.NoneItem.ID);
                    d["Items"][j].TryGetInt("Weight", out int weight, 1);

                    items.Add(new WeightedItem(id, weight));
                }

                choices.Add(new EquipmentSelection(slot, items));
            }
        }
    }

    public Item GetItemForSlot(ItemProperty slot)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i].slot == slot)
            {
                return GameData.Get<Item>(Utility.WeightedChoice(choices[i].items).itemID);
            }
        }

        return ItemList.NoneItem;
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }
}

public class EquipmentSelection
{
    public ItemProperty slot;
    public readonly List<WeightedItem> items;

    public EquipmentSelection(ItemProperty slot, List<WeightedItem> items)
    {
        this.slot = slot;
        this.items = items;
    }
}

public class WeightedItem : IWeighted
{
    public int Weight { get; set; }
    public readonly string itemID;

    public WeightedItem(string id, int weight)
    {
        itemID = id;
        Weight = weight;
    }
}