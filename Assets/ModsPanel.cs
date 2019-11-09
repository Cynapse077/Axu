using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModsPanel : MonoBehaviour
{
    public GameObject modPrefab;
    public Transform modAnchor;
    public Text modDescriptionText;

    GameObject[] modObjects;

    void OnEnable()
    {
        Reload();
    }

    void Reload()
    {
        modDescriptionText.text = "";
        modAnchor.DestroyChildren();
        modObjects = new GameObject[ModManager.Mods.Count];

        if (ModManager.PreInitialized)
        {
            for (int i = 0; i < ModManager.Mods.Count; i++)
            {
                GameObject g = Instantiate(modPrefab, modAnchor);
                g.GetComponent<ModPrefab>().Setup(ModManager.Mods[i], modDescriptionText, this);
                modObjects[i] = g;
            }
        }
    }

    public void SetActive(int id, bool active)
    {
        if (ModManager.Mods[id].IsCore() && !active)
            return;

        bool change = ModManager.Mods[id].IsActive != active;

        if (change)
        {
            ModManager.Mods[id].SetActive(active);
            ModManager.ResetAllMods();
            Reload();
        }
    }
}
