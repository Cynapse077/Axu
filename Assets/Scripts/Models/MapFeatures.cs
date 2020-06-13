using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class MapFeatures
{
    public Coord pos;
    List<string> features;
    List<string> customFeatures;

    public MapFeatures(int x, int y)
    {
        pos = new Coord(x, y);
        features = new List<string>();
        customFeatures = new List<string>();
    }

    public bool HasCustomFeatures()
    {
        return customFeatures.Count > 0;
    }

    public void AddFeature(string s)
    {
        customFeatures.Add(s);
    }

    public void RemoveFeature(string s)
    {
        customFeatures.Remove(s);
    }

    public void SetupFeatureList(TileMap_Data tmd, List<GameObject> mos, List<NPC> ais)
    {
        features.Clear();
        features.Add(World.tileMap.TileName());

        if (ais != null)
        {
            for (int i = 0; i < ais.Count; i++)
            {
                if (ais[i].HasFlag(NPC_Flags.Merchant))
                    features.Add("MF_Merchant".Localize());

                if (ais[i].HasFlag(NPC_Flags.Book_Merchant))
                    features.Add("MF_BookMerchant".Localize());

                if (ais[i].HasFlag(NPC_Flags.Doctor))
                    features.Add("MF_Doctor".Localize());

                if (ais[i].HasFlag(NPC_Flags.Mercenary))
                    features.Add("MF_Mercenary".Localize());

                if (!string.IsNullOrEmpty(ais[i].questID))
                    features.Add("MF_Quest".Localize());
            }
        }

        World.userInterface.mapFeaturePanel.UpdateFeatureList(features, customFeatures);
    }

    public SMapFeature CustomFeatureList()
    {
        return new SMapFeature(pos, customFeatures.ToArray());
    }
}

[System.Serializable]
public struct SMapFeature
{
    public int x;
    public int y;
    public string[] feats;

    public SMapFeature(Coord p, string[] f)
    {
        x = p.x;
        y = p.y;
        feats = f;
    }

    public static SMapFeature FromJson(JsonData dat)
    {
        Coord c = new Coord((int)dat["x"], (int)dat["y"]);
        string[] f = new string[dat["feats"].Count];

        for (int i = 0; i < dat["feats"].Count; i++)
        {
            f[i] = dat["feats"][i].ToString();
        }

        return new SMapFeature(c, f);
    }
}
