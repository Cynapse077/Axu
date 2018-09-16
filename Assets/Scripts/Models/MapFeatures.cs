using System.Collections.Generic;
using UnityEngine;

public class MapFeatures {
	public Coord pos;
	List<string> features;
    List<string> customFeatures;

	public MapFeatures(int x, int y) {
		pos = new Coord(x, y);
		features = new List<string>();
        customFeatures = new List<string>();
	}

    public void AddFeature(string s) {
        customFeatures.Add(s);
    }

    public void RemoveFeature(string s) {
        customFeatures.Remove(s);
    }

	public void SetupFeatureList(TileMap_Data tmd, List<GameObject> mos, List<NPC> ais) {
		features.Clear();
		features.Add(World.tileMap.TileName());

        if (ais != null) {
            for (int i = 0; i < ais.Count; i++) {
                if (ais[i].HasFlag(NPC_Flags.Merchant))
                    features.Add(LocalizationManager.GetContent("MF_Merchant"));

                if (ais[i].HasFlag(NPC_Flags.Book_Merchant))
                    features.Add(LocalizationManager.GetContent("MF_BookMerchant"));

                if (ais[i].HasFlag(NPC_Flags.Doctor))
                    features.Add(LocalizationManager.GetContent("MF_Doctor"));

                if (ais[i].HasFlag(NPC_Flags.Mercenary))
                    features.Add(LocalizationManager.GetContent("MF_Mercenary"));

                if (!string.IsNullOrEmpty(ais[i].questID))
                    features.Add(LocalizationManager.GetContent("MF_Quest"));
            }
        }

		World.userInterface.mapFeaturePanel.UpdateFeatureList(features, customFeatures);
	}
}
