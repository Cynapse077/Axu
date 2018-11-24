using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapFeaturePanel : MonoBehaviour
{
    public InputField iField;
    public Button addButton;
    public Sprite[] hungerSprites;
    public Transform anchor;

    public GameObject mapFeatureObject;

    void Start()
    {
        addButton.onClick.AddListener(AddButtonClicked);
    }

    public void RemoveChild(Transform c)
    {
        c.SetParent(null);
        SimplePool.Despawn(c.gameObject);
    }

    void Update()
    {
        PlayerInput.lockInput = iField.isFocused;

        if (iField.isFocused)
        {
            if (GameSettings.Keybindings.GetKey("Enter"))
            {
                AddButtonClicked();
            }
            if (GameSettings.Keybindings.GetKey("Pause"))
            {
                iField.text = "";
                iField.DeactivateInputField();
            }
        }
    }

    void AddButtonClicked()
    {
        if (string.IsNullOrEmpty(iField.text))
            return;

        World.tileMap.AddMapFeature(iField.text);
        iField.text = "";
    }

    public void UpdateFeatureList(List<string> features, List<string> custom)
    {
        while (anchor.childCount > 0)
        {
            RemoveChild(anchor.GetChild(0));
        }

        for (int i = 0; i < features.Count; i++)
        {
            CreateFeature(features[i], false);
        }

        for (int i = 0; i < custom.Count; i++)
        {
            CreateFeature(custom[i], true);
        }
    }

    void CreateFeature(string text, bool canRemove)
    {
        GameObject g = SimplePool.Spawn(mapFeatureObject, anchor);
        MapFeatureObject mfo = g.GetComponent<MapFeatureObject>();
        mfo.parentPanel = this;
        mfo.removeButton.gameObject.SetActive(canRemove);
        mfo.myText.text = text;
    }
}
