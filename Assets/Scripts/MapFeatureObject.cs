using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapFeatureObject : MonoBehaviour {
    public Button removeButton;
    public Text myText;
    public MapFeaturePanel parentPanel;

    void OnEnable() {
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => { World.tileMap.RemoveMapFeature(myText.text); });
    }
}
