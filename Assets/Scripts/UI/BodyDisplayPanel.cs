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

    public BodyDisplay_Button chest;
    public BodyDisplay_Button back;

    public Text title;
    public Text desc;
    public Text traitDisplay;
    public GameObject extraDisplay;

    Body body;
    List<BodyPart> parts = new List<BodyPart>();
    Color offColor = new Color(1.0f, 1.0f, 1.0f, 0.07f);

    public void Init(Body _body)
    {
        ActivateExtraInfoWindow(true);
        body = _body;
        InitializeTraits();
        DisableAll();

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Head);

        for (int i = 0; i < parts.Count; i++)
        {
            if (i < 3)
            {
                heads.GetChild(i).GetComponent<Image>().color = Color.white;
                heads.GetChild(i).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Wing);

        for (int i = 0; i < parts.Count; i++)
        {
            if (i < 4)
            {
                if (i % 2 == 0)
                {
                    wings_R.GetChild(i / 2).GetComponent<Image>().color = Color.white;
                    wings_R.GetChild(i / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
                else
                {
                    wings_L.GetChild((i - 1) / 2).GetComponent<Image>().color = Color.white;
                    wings_L.GetChild((i - 1) / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Arm);

        for (int i = 0; i < parts.Count; i++)
        {
            if (i < 6)
            {
                if (i % 2 == 0)
                {
                    arms_R.GetChild(i / 2).GetComponent<Image>().color = Color.white;
                    arms_R.GetChild(i / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
                else
                {
                    arms_L.GetChild((i - 1) / 2).GetComponent<Image>().color = Color.white;
                    arms_L.GetChild((i - 1) / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Leg);

        for (int i = 0; i < parts.Count; i++)
        {
            if (i < 6)
            {
                if (i % 2 == 0)
                {
                    legs_R.GetChild(i / 2).GetComponent<Image>().color = Color.white;
                    legs_R.GetChild(i / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
                else
                {
                    legs_L.GetChild((i - 1) / 2).GetComponent<Image>().color = Color.white;
                    legs_L.GetChild((i - 1) / 2).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
                }
            }
        }

        parts = body.GetBodyPartsBySlot(ItemProperty.Slot_Tail);

        for (int i = 0; i < parts.Count; i++)
        {
            if (i < 3)
            {
                tails.GetChild(i).GetComponent<Image>().color = Color.white;
                tails.GetChild(i).GetComponent<BodyDisplay_Button>().SetBodyPart(parts[i]);
            }
        }

        chest.gameObject.GetComponent<Image>().color = Color.white;
        chest.SetBodyPart(body.GetBodyPartBySlot(ItemProperty.Slot_Chest));
        back.gameObject.GetComponent<Image>().color = Color.white;
        back.SetBodyPart(body.GetBodyPartBySlot(ItemProperty.Slot_Back));

        parts.Clear();
    }

    void InitializeTraits()
    {
        traitDisplay.text = "";

        foreach (Trait t in body.entity.stats.traits)
        {
            string tName = "  " + t.Name;

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
            heads.GetChild(i).GetComponent<Image>().color = offColor;
        }

        for (int i = 0; i < wings_R.childCount; i++)
        {
            wings_R.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            wings_R.GetChild(i).GetComponent<Image>().color = offColor;
        }
        for (int i = 0; i < wings_L.childCount; i++)
        {
            wings_L.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            wings_L.GetChild(i).GetComponent<Image>().color = offColor;
        }

        for (int i = 0; i < arms_R.childCount; i++)
        {
            arms_R.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            arms_R.GetChild(i).GetComponent<Image>().color = offColor;
        }
        for (int i = 0; i < arms_L.childCount; i++)
        {
            arms_L.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            arms_L.GetChild(i).GetComponent<Image>().color = offColor;
        }

        for (int i = 0; i < legs_R.childCount; i++)
        {
            legs_R.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            legs_R.GetChild(i).GetComponent<Image>().color = offColor;
        }
        for (int i = 0; i < legs_L.childCount; i++)
        {
            legs_L.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            legs_L.GetChild(i).GetComponent<Image>().color = offColor;
        }

        for (int i = 0; i < tails.childCount; i++)
        {
            tails.GetChild(i).GetComponent<BodyDisplay_Button>().SetTargets(title, desc, this);
            tails.GetChild(i).GetComponent<Image>().color = offColor;
        }

        back.SetTargets(title, desc, this);
        chest.SetTargets(title, desc, this);
        back.gameObject.GetComponent<Image>().color = Color.white;
        chest.gameObject.GetComponent<Image>().color = Color.white;
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
