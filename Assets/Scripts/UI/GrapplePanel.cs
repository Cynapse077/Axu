using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GrapplePanel : MonoBehaviour
{
    public Transform anchor;
    public GameObject abilityIcon;

    int max = 0;
    int selectedNum = 0;
    List<KeyValuePair<string, int>> actions;
    Body body;
    Body targetBody;

    public void Initialize(Body bod, Body targetBod)
    {
        body = bod;
        targetBody = targetBod;
        anchor.DestroyChildren();
        actions = new List<KeyValuePair<string, int>>();
        max = 0;

        string n = LocalizationManager.GetContent("Grapple_Grab");
        int skill = body.entity.stats.proficiencies.MartialArts.level + 1;

        if (body.AllGrips().Count <= 0)
        {
            //GRAB
            GameObject grabGO = Instantiate(abilityIcon, anchor);
            grabGO.GetComponentInChildren<Text>().text = n;
            grabGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
            actions.Add(new KeyValuePair<string, int>("Grab", -1));

            max++;
        }

        for (int i = 0; i < body.bodyParts.Count; i++)
        {
            if (body.bodyParts[i].grip != null && body.bodyParts[i].grip.heldPart != null)
            {

                //PUSH
                GameObject pushGO = Instantiate(abilityIcon, anchor);
                n = LocalizationManager.GetContent("Grapple_Push");
                n = n.Replace("[NAME]", body.bodyParts[i].grip.heldPart.myBody.gameObject.name);
                pushGO.GetComponentInChildren<Text>().text = n;
                pushGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
                actions.Add(new KeyValuePair<string, int>("Push", i));

                max++;

                //TAKE DOWN
                GameObject takeDownGO = Instantiate(abilityIcon, anchor);
                n = LocalizationManager.GetContent("Grapple_TakeDown");
                n = n.Replace("[NAME]", body.bodyParts[i].grip.heldPart.myBody.gameObject.name);
                takeDownGO.GetComponentInChildren<Text>().text = n;
                takeDownGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
                actions.Add(new KeyValuePair<string, int>("Take Down", i));

                max++;

                //Pressure Point
                if (skill > 1)
                {
                    GameObject pressurePointGo = Instantiate(abilityIcon, anchor);
                    n = LocalizationManager.GetContent("Grapple_PressurePoint");
                    n = n.Replace("[NAME]", body.bodyParts[i].grip.heldPart.displayName);
                    pressurePointGo.GetComponentInChildren<Text>().text = n;
                    pressurePointGo.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
                    actions.Add(new KeyValuePair<string, int>("Pressure", i));

                    max++;
                }

                //STRANGLE
                if (skill > 2 && body.bodyParts[i].grip.heldPart.slot == ItemProperty.Slot_Head)
                {
                    GameObject strangleGO = Instantiate(abilityIcon, anchor);
                    n = LocalizationManager.GetContent("Grapple_Strangle");
                    n = n.Replace("[NAME]", body.bodyParts[i].grip.heldPart.myBody.entity.MyName);
                    strangleGO.GetComponentInChildren<Text>().text = n;
                    strangleGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
                    actions.Add(new KeyValuePair<string, int>("Strangle", i));

                    max++;
                }

                //Pull
                if (skill > 3)
                {
                    GameObject pullGO = Instantiate(abilityIcon, anchor);
                    n = LocalizationManager.GetContent("Grapple_Pull");
                    n = n.Replace("[NAME]", body.bodyParts[i].grip.heldPart.displayName);
                    pullGO.GetComponentInChildren<Text>().text = n;
                    pullGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
                    actions.Add(new KeyValuePair<string, int>("Pull", i));

                    max++;
                }

                //RELEASE GRIP
                GameObject releaseGO = Instantiate(abilityIcon, anchor);
                n = LocalizationManager.GetContent("Grapple_Release");
                n = n.Replace("[NAME]", body.bodyParts[i].grip.heldPart.myBody.gameObject.name);
                releaseGO.GetComponentInChildren<Text>().text = n;
                releaseGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
                actions.Add(new KeyValuePair<string, int>("Release", i));

                max++;
            }
        }
    }

    void SelectPressed()
    {
        if (actions[selectedNum].Key == "Grab")
        {
            World.userInterface.CloseWindows();
            World.userInterface.Grab(targetBody);
        }
        else
        {
            PerformAction();
        }
    }

    void PerformAction()
    {
        EntitySkills skills = ObjectManager.player.GetComponent<EntitySkills>();
        BodyPart targetLimb = body.bodyParts[actions[selectedNum].Value].grip.heldPart;

        if (actions[selectedNum].Key == "Push")
        {
            Entity target = targetLimb.myBody.entity;
            skills.Grapple_Shove(target);

        }
        else if (actions[selectedNum].Key == "Take Down")
        {
            Stats target = targetLimb.myBody.entity.stats;
            skills.Grapple_TakeDown(target, targetLimb.displayName);

        }
        else if (actions[selectedNum].Key == "Strangle")
        {
            Stats target = targetLimb.myBody.entity.stats;
            skills.Grapple_Strangle(target);

        }
        else if (actions[selectedNum].Key == "Pull")
        {
            skills.Grapple_Pull(body.bodyParts[actions[selectedNum].Value].grip);

        }
        else if (actions[selectedNum].Key == "Pressure")
        {
            skills.Grapple_Pressure(body.bodyParts[actions[selectedNum].Value].grip);
        }
        else if (actions[selectedNum].Key == "Release")
        {
            CombatLog.CombatMessage("Gr_ReleaseGrip", ObjectManager.player.name, targetLimb.myBody.gameObject.name, false);
            body.bodyParts[actions[selectedNum].Value].grip.Release();
        }

        World.userInterface.CloseWindows();
    }

    public void Update()
    {
        if (GameSettings.Keybindings.GetKey("North"))
            selectedNum--;
        else if (GameSettings.Keybindings.GetKey("South"))
            selectedNum++;

        if (selectedNum < 0)
            selectedNum = max - 1;
        else if (selectedNum >= max)
            selectedNum = 0;

        EventSystem.current.SetSelectedGameObject(anchor.GetChild(selectedNum).gameObject);

        if (GameSettings.Keybindings.GetKey("Enter"))
        {
            SelectPressed();
        }
    }
}
