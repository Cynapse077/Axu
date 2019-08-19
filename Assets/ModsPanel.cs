using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModsPanel : MonoBehaviour
{
    public GameObject modPrefab;
    public Transform modAnchor;
    public Text modDescriptionText;

    void Start()
    {
        modDescriptionText.text = "";
        modAnchor.DestroyChildren();

        if (ModManager.IsInitialized)
        {
            foreach (Mod m in ModManager.mods)
            {
                GameObject g = Instantiate(modPrefab, modAnchor);
                g.GetComponent<ModPrefab>().Setup(m, modDescriptionText);
            }
        }        
    }
}
