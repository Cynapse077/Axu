using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BodyDisplayPanel : MonoBehaviour
{
    public Transform heads;
    public Transform wings_R;
    public Transform wings_L;
    public Transform arms_R;
    public Transform arms_L;
    public Transform legs_R;
    public Transform legs_L;
    public Transform tails;
    public Transform external;

    public BodyDisplay_Button chest;
    public BodyDisplay_Button back;

    public Text title;
    public Text desc;
    public Text traitDisplay;
    public GameObject extraDisplay;

    Body body;
    List<BodyPart> parts = new List<BodyPart>();

    public void Init(Body _body)
    {
        ActivateExtraInfoWindow(true);
        body = _body;
        InitializeTraits();
        DisableAll();

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Head);

        for (int i = 0; i < parts.Count; i++)
        {
            if (!parts[i].external)
            {
                heads.GetChild(i).gameObject.SetActive(true);
                heads.GetChild(i).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Wing);

        for (int i = 0; i < parts.Count; i++)
        {
            if (!parts[i].external)
            {
                if (i % 2 == 0)
                {
                    wings_R.GetChild(i / 2).gameObject.SetActive(true);
                    wings_R.GetChild(i / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
                else
                {
                    wings_L.GetChild((i - 1) / 2).gameObject.SetActive(true);
                    wings_L.GetChild((i - 1) / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Arm);

        for (int i = 0; i < parts.Count; i++)
        {
            if (!parts[i].external)
            {
                if (i % 2 == 0)
                {
                    arms_R.GetChild(i / 2).gameObject.SetActive(true);
                    arms_R.GetChild(i / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
                else
                {
                    arms_L.GetChild((i - 1) / 2).gameObject.SetActive(true);
                    arms_L.GetChild((i - 1) / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Leg);

        for (int i = 0; i < parts.Count; i++)
        {
            if (!parts[i].external)
            {
                if (i % 2 == 0)
                {
                    legs_R.GetChild(i / 2).gameObject.SetActive(true);
                    legs_R.GetChild(i / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
                else
                {
                    legs_L.GetChild((i - 1) / 2).gameObject.SetActive(true);
                    legs_L.GetChild((i - 1) / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Tail);

        for (int i = 0; i < parts.Count; i++)
        {
            if (!parts[i].external)
            {
                tails.GetChild(i).gameObject.SetActive(true);
                tails.GetChild(i).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
            }
        }

        parts = body.bodyParts.FindAll(x => x.external);

        for (int i = 0; i < parts.Count; i++)
        {
            external.GetChild(i).gameObject.SetActive(true);
            external.GetChild(i).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
        }

        chest.gameObject.SetActive(true);
        chest.SetBodyPart(body.GetBodyPartBySlot(ItemProperty.Slot_Chest));
        back.gameObject.SetActive(true);
        back.SetBodyPart(body.GetBodyPartBySlot(ItemProperty.Slot_Back));

        parts.Clear();
    }

    void InitializeTraits()
    {
        traitDisplay.text = "";

        foreach (Trait t in body.entity.stats.traits)
        {
            string tName = "  " + t.name;

            if (t.ContainsEffect(TraitEffects.Mutation))
                tName = "<color=magenta>" + tName + "</color>";
            else if (t.ContainsEffect(TraitEffects.Disease))
                tName = "<color=red>" + tName + "</color>";
            else
                tName = "<color=yellow>" + tName + "</color>";

            traitDisplay.text += string.Format("{0}    \n\"<i>{1}</i>\"\n\n", tName, t.description);
        }
    }

    void DisableAll()
    {
        for (int i = 0; i < heads.childCount; i++)
        {
            heads.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            heads.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < wings_R.childCount; i++)
        {
            wings_R.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            wings_R.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < wings_L.childCount; i++)
        {
            wings_L.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            wings_L.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < arms_R.childCount; i++)
        {
            arms_R.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            arms_R.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < arms_L.childCount; i++)
        {
            arms_L.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            arms_L.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < legs_R.childCount; i++)
        {
            legs_R.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            legs_R.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < legs_L.childCount; i++)
        {
            legs_L.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            legs_L.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < tails.childCount; i++)
        {
            tails.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            tails.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < external.childCount; i++)
        {
            external.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            external.GetChild(i).gameObject.SetActive(false);
        }

        back.SetTargets(title, desc, this);
        chest.SetTargets(title, desc, this);
        back.gameObject.SetActive(false);
        chest.gameObject.SetActive(false);
    }

    public void ActivateExtraInfoWindow(bool on)
    {
        extraDisplay.SetActive(on);

        if (on)
        {
            title.text = "Traits";
        }
    }
}
