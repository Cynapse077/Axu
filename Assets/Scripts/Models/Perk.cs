using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

public class Perk : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }



    public Perk() { }

    public Perk(JsonData dat)
    {
        FromJson(dat);
    }

    public void OnEquipItem(Item item)
    {

    }

    public void OnRemoveItem(Item item)
    {

    }

    public void FromJson(JsonData dat)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }
}
