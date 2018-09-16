using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapFeaturePanel : MonoBehaviour {
    public InputField iField;
    public Button addButton;
	public GameObject hunger;
	public Sprite[] hungerSprites;
    public Transform anchor;

    public GameObject mapFeatureObject;

    void Start() {
        addButton.onClick.AddListener(AddButtonClicked);
    }

    public void RemoveChild(Transform c) {
        c.SetParent(null);
        SimplePool.Despawn(c.gameObject);
    }

    void Update() {
        PlayerInput.lockInput = iField.isFocused;

        if (iField.isFocused) {
            if (GameSettings.Keybindings.GetKey("Enter")) {
                AddButtonClicked();
            }
            if (GameSettings.Keybindings.GetKey("Pause")) {
                iField.text = "";
                iField.DeactivateInputField();
            }
        }
    }

    void AddButtonClicked() {
        if (string.IsNullOrEmpty(iField.text))
            return;

        World.tileMap.AddMapFeature(iField.text);
        iField.text = "";
    }
    
	public void UpdateFeatureList(List<string> features, List<string> custom) {
        while (anchor.childCount > 0) {
            RemoveChild(anchor.GetChild(0));
        }

		for (int i = 0; i < features.Count; i++) {
            CreateFeature(features[i], false);
		}

        for (int i = 0; i < custom.Count; i++) {
            CreateFeature(custom[i], true);
        }

        if (ObjectManager.playerEntity != null)
			UpdateHunger(ObjectManager.playerEntity.stats);
	}

    void CreateFeature(string text, bool canRemove) {
        GameObject g = SimplePool.Spawn(mapFeatureObject, anchor);
        MapFeatureObject mfo = g.GetComponent<MapFeatureObject>();
        mfo.parentPanel = this;
        mfo.removeButton.gameObject.SetActive(canRemove);
        mfo.myText.text = text;
    }

	void UpdateHunger(Stats stats) {
		hunger.GetComponent<Image>().sprite = hungerSprites[HungerSprites(stats)];
	}

	int HungerSprites(Stats stats) {
		bool isVamp = stats.hasTraitEffect(TraitEffects.Vampirism);
		string localTextKey = "";
		int id = 0;

		if (stats.Hunger >= Globals.Satiated) {
			localTextKey = (isVamp) ? "Thirst_1" : "Food_1";
			id = (isVamp) ? 5 : 0;
		} else if (stats.Hunger < Globals.Satiated && stats.Hunger >= Globals.Hungry) {
			localTextKey = (isVamp) ? "Thirst_2" : "Food_2";
			id = (isVamp) ? 6 : 1;
		} else if (stats.Hunger < Globals.Hungry && stats.Hunger >= Globals.VHungry) {
			localTextKey = (isVamp) ? "Thirst_3" : "Food_3";
			id = (isVamp) ? 7 : 2;
		} else if (stats.Hunger < Globals.VHungry && stats.Hunger > Globals.Starving) {
			localTextKey = (isVamp) ? "Thirst_4" : "Food_4";
			id = (isVamp) ? 8 : 3;
		} else {
			localTextKey = (isVamp) ? "Thirst_5" : "Food_5";
			id = (isVamp) ? 9 : 4;
		}

		hunger.GetComponentInChildren<LocalizedText>().GetLocalizedText(localTextKey);
		return id;
	}
}
